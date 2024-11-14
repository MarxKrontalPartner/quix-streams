import logging
from pathlib import Path
from time import sleep
from typing import Generator, Optional, Union

from quixstreams.models import Topic, TopicConfig
from quixstreams.sources import Source

from .compressions import CompressionName
from .formats import FORMATS, Format, FormatName

__all__ = ("FileSource",)

logger = logging.getLogger(__name__)


class FileSource(Source):
    """
    Ingest a set of local files into kafka by iterating through the provided folder and
    processing all nested files within it.

    Expects folder and file structures as generated by the related FileSink connector:

    my_topics/
    ├── topic_a/
    │   ├── 0/
    │   │   ├── 0000.ext
    │   │   └── 0011.ext
    │   └── 1/
    │       ├── 0003.ext
    │       └── 0016.ext
    └── topic_b/
        └── etc...

    Intended to be used with a single topic (ex: topic_a), but will recursively read
    from whatever entrypoint is passed to it.

    File format structure depends on the file format.

    See the `.formats` and `.compressions` modules to see what is supported.

    Example:

    from quixstreams import Application
    from quixstreams.sources.community.file import FileSource

    app = Application(broker_address="localhost:9092", auto_offset_reset="earliest")
    source = FileSource(
        filepath="/path/to/my/topic_folder",
        file_format="json",
        file_compression="gzip",
    )
    sdf = app.dataframe(source=source).print(metadata=True)

    if __name__ == "__main__":
        app.run()
    """

    def __init__(
        self,
        filepath: Union[str, Path],
        file_format: Union[Format, FormatName],
        file_compression: Optional[CompressionName] = None,
        as_replay: bool = True,
        name: Optional[str] = None,
        shutdown_timeout: float = 10,
    ):
        """
        :param filepath: a filepath to recursively read through; it is recommended to
            provide the path to a given topic folder (ex: `/path/to/topic_a`).
        :param file_format: what format the message files are in (ex: json, parquet).
            Optionally, can provide a `Format` instance if more than file_compression
            is necessary to define (file_compression will then be ignored).
        :param file_compression: what compression is used on the given files, if any.
        :param as_replay: Produce the messages with the original time delay between them.
            Otherwise, produce the messages as fast as possible.
            NOTE: Time delay will only be accurate per partition, NOT overall.
        :param name: The name of the Source application (Default: last folder name).
        :param shutdown_timeout: Time in seconds the application waits for the source
            to gracefully shutdown
        """
        self._filepath = Path(filepath)
        self._formatter = _get_formatter(file_format, file_compression)
        self._as_replay = as_replay
        self._previous_timestamp = None
        self._previous_partition = None
        super().__init__(
            name=name or self._filepath.name, shutdown_timeout=shutdown_timeout
        )

    def _replay_delay(self, current_timestamp: int):
        """
        Apply the replay speed by calculating the delay between messages
        based on their timestamps.
        """
        if self._previous_timestamp is not None:
            time_diff = (current_timestamp - self._previous_timestamp) / 1000
            if time_diff > 0:
                logger.debug(f"Sleeping for {time_diff} seconds...")
                sleep(time_diff)
        self._previous_timestamp = current_timestamp

    def _get_partition_count(self) -> int:
        return len([f for f in self._filepath.iterdir()])

    def default_topic(self) -> Topic:
        """
        Uses the file structure to generate the desired partition count for the
        internal topic.
        :return: the original default topic, with updated partition count
        """
        topic = super().default_topic()
        topic.config = TopicConfig(
            num_partitions=self._get_partition_count(), replication_factor=1
        )
        return topic

    def _check_file_partition_number(self, file: Path):
        """
        Checks whether the next file is the start of a new partition so the timestamp
        tracker can be reset.
        """
        partition = int(file.parent.name)
        if self._previous_partition != partition:
            self._previous_timestamp = None
            self._previous_partition = partition
            logger.debug(f"Beginning reading partition {partition}")

    def _produce(self, record: dict):
        kafka_msg = self._producer_topic.serialize(
            key=record["_key"],
            value=record["_value"],
            timestamp_ms=record["_timestamp"],
        )
        self.produce(
            key=kafka_msg.key, value=kafka_msg.value, timestamp=kafka_msg.timestamp
        )

    def run(self):
        while self._running:
            for file in _file_finder(self._filepath):
                logger.info(f"Reading files from topic {self._filepath.name}")
                self._check_file_partition_number(file)
                for record in self._formatter.file_read(file):
                    if self._as_replay:
                        self._replay_delay(record["_timestamp"])
                    self._produce(record)
                self.flush()
            return


def _get_formatter(
    formatter: Union[Format, FormatName], compression: Optional[CompressionName]
) -> Format:
    if isinstance(formatter, Format):
        return formatter
    elif format_obj := FORMATS.get(formatter):
        return format_obj(compression=compression)

    allowed_formats = ", ".join(FormatName.__args__)
    raise ValueError(
        f'Invalid format name "{formatter}". '
        f"Allowed values: {allowed_formats}, "
        f"or an instance of a subclass of `Format`."
    )


def _file_finder(filepath: Path) -> Generator[Path, None, None]:
    if filepath.is_dir():
        for i in sorted(filepath.iterdir(), key=lambda x: x.name):
            yield from _file_finder(i)
    else:
        yield filepath