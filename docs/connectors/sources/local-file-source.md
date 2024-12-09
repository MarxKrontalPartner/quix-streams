# Local File Source

!!! info

    This is a **Community** connector. Test it before using in production.

    To learn more about differences between Core and Community connectors, see the [Community and Core Connectors](../community-and-core.md) page.

This source reads records from files at a local directory and produces 
them as messages to a kafka topic using any desired `StreamingDataFrame`-based transformations. 

The resulting messages can be produced in "replay" mode, where the time between record 
producing is matched as close as possible to the original. (per topic partition only).


## How To Install

Simply install Quix Streams, no options required:

```bash
pip install quixstreams
```

## How It Works

`FileSource` steps through each folder within the provided path and dumps each record 
contained in each file as a message to a Kafka topic. Folders are navigated in 
lexicographical order.

Records are read in a streaming fashion and committed after every file, offering 
[at-least-once guarantees](#processingdelivery-guarantees).

It can handle one given file type (ex. JSONlines or Parquet) at a time, and also 
supports file decompression.

You can learn more details about the [expected kafka message format](#message-data-formatschema) below.

## How To Use

Local File Source is the default configuration of the `FileSource` connector.

Simply hand the configured `FileSource` (without a `origin`) to your `SDF` 
(`app.dataframe(source=<SOURCE>)`).

For more details around various settings, see [configuration](#configuration).

```python
from quixstreams import Application
from quixstreams.sources.community.file import FileSource

app = Application(broker_address="localhost:9092")
source = FileSource(
    directory="/path/to/my/topic_folder",
    format="json",
    compression="gzip",
    replay_speed=1.0,
)
sdf = app.dataframe(source=source).print(metadata=True)
# YOUR LOGIC HERE!

if __name__ == "__main__":
    app.run()
```

## Configuration

Here are some important configurations to be aware of (see [File Source API](../../api-reference/sources.md#filesource) for all parameters).

### Required:

- `directory`: a directory to recursively read through (exclude bucket name).    
    **Note**: If using alongside `FileSink`, provide the path to the topic name folder (ex: `"path/to/topic_a/"`).

### Optional:

- `format`: what format the message files are in (ex: `"json"`, `"parquet"`).    
    **Advanced**: can optionally provide a `Format` instance (`compression` will then be ignored).    
    **Default**: `"json"`
- `compression`: what compression is used on the given files, if any (ex: `"gzip"`)    
    **Default**: `None`
- `replay_speed`: Produce the messages with this speed multiplier, which roughly 
    reflects the time "delay" between the original message producing.    
    Use any `float` `>= 0.0`, where `0.0` is no delay, and `1.0` is the original speed.    
    **Note**: Time delay will only be accurate _per partition_, NOT overall.    
    **Default**: 1.0


## File hierarchy/structure

The File Source expects a folder structure like so:

```
    my_sinked_topics/
    ├── topic_a/          # topic name (use this path to File Source!)
    │   ├── 0/            # topic partition number
    │   │   ├── 0000.ext  # formatted offset files (ex: JSON)
    │   │   └── 0011.ext
    │   └── 1/
    │       ├── 0003.ext
    │       └── 0016.ext
    └── topic_b/
        └── etc...
```

This is the default structure generated by the File Sink.

## Message Data Format/Schema

The expected file schema largely depends on the chosen 
file format.

For easiest use (especially alongside [`FileSink`](../sinks/file-sink.md)), 
you can follow these patterns: 

### Row-based Formats (ex: JSON)

Files should have records with the following fields, with `_value` being a 
JSON-deserializable item:

  - `_key`
  - `_value`
  - `_timestamp`


This will result in the following Kafka message format for `Application`:

- Message `key` will be the record `_key` as `bytes`.
- Message `value` will be the record `_value` as a `json`/`dict`
- Message `timestamp` will be the record `_timestamp` (ms).

### Columnar Formats (ex: Parquet)
These do not expect an explicit `value` field; instead all columns should be included 
individually while including `_key` and `_timestamp`:

  - `_key`
  - `_timestamp`
  - `field_a`
  - `field_b`    
  etc...


This will result in the following Kafka message format for `Application`:

- Message `key` will be the record `_key` as `bytes`.
- Message `value` will be every record field except `_key` and `_timestamp` packed as a `json`/`dict`
- Message `timestamp` will be the record `_timestamp` (ms).


### Custom Schemas (Advanced)

If the original files are not formatted as expected, custom loaders can be configured 
on some `Format` classes (ex: `JsonFormat`) which can be handed to `FileSource(format=<Format>)`.

Formats can be imported from `quixstreams.sources.community.file.formats`.

## Processing/Delivery Guarantees

This Source offers "at-least-once" guarantees with message delivery: messages are
guaranteed to be committed when a file is finished processing.

However, it does not save any state/position: an unhandled exception will cause the 
`Application` to fail, and rerunning the `Application` will begin processing from the
beginning (reproducing all previously processed messages).

## Topic

The default topic will have a partition count that reflects the partition count found 
within the provided topic's folder structure.

The default topic name the Application dumps to is based on the last folder name of 
the `FileSource` `directory` as: `source__<last folder name>`.