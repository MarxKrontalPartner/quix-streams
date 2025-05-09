import logging
import shutil
from pathlib import Path
from typing import Dict, Optional, Type, Union

from quixstreams.models.topics import TopicConfig
from quixstreams.rowproducer import RowProducer

from .base import Store, StorePartition
from .exceptions import (
    PartitionStoreIsUsed,
    StoreNotRegisteredError,
    WindowedStoreAlreadyRegisteredError,
)
from .memory import MemoryStore
from .recovery import ChangelogProducerFactory, RecoveryManager
from .rocksdb import RocksDBOptionsType, RocksDBStore
from .rocksdb.windowed.store import WindowedRocksDBStore

__all__ = ("StateStoreManager", "DEFAULT_STATE_STORE_NAME", "StoreTypes")

logger = logging.getLogger(__name__)

DEFAULT_STATE_STORE_NAME = "default"

StoreTypes = Union[Type[RocksDBStore], Type[MemoryStore]]
SUPPORTED_STORES = [RocksDBStore, MemoryStore]


class StateStoreManager:
    """
    Class for managing state stores and partitions.

    StateStoreManager is responsible for:
     - reacting to rebalance callbacks
     - managing the individual state stores
     - providing access to store transactions
    """

    def __init__(
        self,
        group_id: Optional[str] = None,
        state_dir: Optional[Union[str, Path]] = None,
        rocksdb_options: Optional[RocksDBOptionsType] = None,
        producer: Optional[RowProducer] = None,
        recovery_manager: Optional[RecoveryManager] = None,
        default_store_type: StoreTypes = RocksDBStore,
    ):
        if state_dir is not None:
            state_dir = Path(state_dir).absolute()

            if group_id is not None:
                state_dir = state_dir / group_id

        self._state_dir = state_dir
        self._rocksdb_options = rocksdb_options
        self._stores: Dict[Optional[str], Dict[str, Store]] = {}
        self._producer = producer
        self._recovery_manager = recovery_manager
        self._default_store_type = default_store_type

    def _init_state_dir(self) -> None:
        if self._state_dir is None:
            return

        logger.info(f'Initializing state directory at "{self._state_dir}"')
        if self._state_dir.exists():
            if not self._state_dir.is_dir():
                raise FileExistsError(
                    f'Path "{self._state_dir}" already exists, '
                    f"but it is not a directory"
                )
            logger.debug(f'State directory already exists at "{self._state_dir}"')
        else:
            self._state_dir.mkdir(parents=True)
            logger.debug(f'Created state directory at "{self._state_dir}"')

    @property
    def stores(self) -> Dict[Optional[str], Dict[str, Store]]:
        """
        Map of registered state stores
        :return: dict in format {topic: {store_name: store}}
        """
        return self._stores

    @property
    def recovery_required(self) -> bool:
        """
        Whether recovery needs to be done.
        """
        if self._recovery_manager:
            return self._recovery_manager.has_assignments
        return False

    @property
    def using_changelogs(self) -> bool:
        """
        Whether the StateStoreManager is using changelog topics

        :return: using changelogs, as bool
        """
        return bool(self._recovery_manager)

    @property
    def default_store_type(self) -> StoreTypes:
        return self._default_store_type

    def do_recovery(self) -> None:
        """
        Perform a state recovery, if necessary.
        """
        if self._recovery_manager is None:
            raise RuntimeError("a recovery manager is needed to do a recovery")

        return self._recovery_manager.do_recovery()

    def stop_recovery(self) -> None:
        """
        Stop recovery (called during app shutdown).
        """
        if self._recovery_manager is None:
            raise RuntimeError("a recovery manager is needed to do a recovery")

        return self._recovery_manager.stop_recovery()

    def get_store(
        self, topic: str, store_name: str = DEFAULT_STATE_STORE_NAME
    ) -> Store:
        """
        Get a store for given name and topic
        :param topic: topic name
        :param store_name: store name
        :return: instance of `Store`
        """
        store = self._stores.get(topic, {}).get(store_name)
        if store is None:
            raise StoreNotRegisteredError(
                f'Store "{store_name}" (topic "{topic}") is not registered'
            )
        return store

    def _setup_changelogs(
        self,
        topic_name: Optional[str],
        store_name: str,
        topic_config: Optional[TopicConfig] = None,
    ) -> Optional[ChangelogProducerFactory]:
        if self._recovery_manager is None or self._producer is None:
            return None

        logger.debug(
            f'State Manager: registering changelog for store "{store_name}" '
            f'(topic "{topic_name}")'
        )
        changelog_topic = self._recovery_manager.register_changelog(
            topic_name=topic_name, store_name=store_name, topic_config=topic_config
        )
        return ChangelogProducerFactory(
            changelog_name=changelog_topic.name,
            producer=self._producer,
        )

    def register_store(
        self,
        topic_name: Optional[str],
        store_name: str = DEFAULT_STATE_STORE_NAME,
        store_type: Optional[StoreTypes] = None,
        topic_config: Optional[TopicConfig] = None,
    ) -> None:
        """
        Register a state store to be managed by StateStoreManager.

        During processing, the StateStoreManager will react to rebalancing callbacks
        and assign/revoke the partitions for registered stores.

        Each store can be registered only once for each topic.

        :param topic_name: topic name
        :param store_name: store name
        :param store_type: the storage type used for this store.
            Default to StateStoreManager `default_store_type`
        """
        if self._stores.get(topic_name, {}).get(store_name) is None:
            changelog_producer_factory = self._setup_changelogs(
                topic_name, store_name, topic_config=topic_config
            )

            store_type = store_type or self.default_store_type
            if store_type == RocksDBStore:
                factory: Store = RocksDBStore(
                    name=store_name,
                    topic=topic_name,
                    base_dir=str(self._state_dir),
                    changelog_producer_factory=changelog_producer_factory,
                    options=self._rocksdb_options,
                )
            elif store_type == MemoryStore:
                factory = MemoryStore(
                    name=store_name,
                    topic=topic_name,
                    changelog_producer_factory=changelog_producer_factory,
                )
            else:
                raise ValueError(f"invalid store type: {store_type}")

            self._stores.setdefault(topic_name, {})[store_name] = factory

    def register_windowed_store(self, topic_name: str, store_name: str) -> None:
        """
        Register a windowed state store to be managed by StateStoreManager.

        During processing, the StateStoreManager will react to rebalancing callbacks
        and assign/revoke the partitions for registered stores.

        Each window store can be registered only once for each topic.

        :param topic_name: topic name
        :param store_name: store name
        """
        store = self._stores.get(topic_name, {}).get(store_name)
        if store:
            raise WindowedStoreAlreadyRegisteredError(
                "This window range and type combination already exists; "
                "to use this window, provide a unique name via the `name` parameter."
            )
        self._stores.setdefault(topic_name, {})[store_name] = WindowedRocksDBStore(
            name=store_name,
            topic=topic_name,
            base_dir=str(self._state_dir),
            changelog_producer_factory=self._setup_changelogs(topic_name, store_name),
            options=self._rocksdb_options,
        )

    def clear_stores(self) -> None:
        """
        Delete all state stores managed by StateStoreManager.
        """
        if any(
            store.partitions
            for topic_stores in self._stores.values()
            for store in topic_stores.values()
        ):
            raise PartitionStoreIsUsed(
                "Cannot clear stores with active partitions assigned"
            )

        if self._state_dir is not None:
            shutil.rmtree(self._state_dir, ignore_errors=True)
            logger.info(f"Removing state folder at {self._state_dir}")

    def on_partition_assign(
        self,
        topic: Optional[str],
        partition: int,
        committed_offsets: dict[str, int],
    ) -> Dict[str, StorePartition]:
        """
        Assign store partitions for each registered store for the given `TopicPartition`
        and return a list of assigned `StorePartition` objects.

        :param topic: Kafka topic name
        :param partition: Kafka topic partition
        :param committed_offsets: a dict with latest committed offsets
            of all assigned topics for this partition number.
        :return: list of assigned `StorePartition`
        """

        store_partitions = {}
        for name, store in self._stores.get(topic, {}).items():
            store_partition = store.assign_partition(partition)
            store_partitions[name] = store_partition
        if self._recovery_manager and store_partitions:
            self._recovery_manager.assign_partition(
                topic=topic,
                partition=partition,
                committed_offsets=committed_offsets,
                store_partitions=store_partitions,
            )
        return store_partitions

    def on_partition_revoke(self, topic: str, partition: int) -> None:
        """
        Revoke store partitions for each registered store for the given `TopicPartition`

        :param topic: Kafka topic name
        :param partition: Kafka topic partition
        """
        if stores := self._stores.get(topic, {}).values():
            if self._recovery_manager:
                self._recovery_manager.revoke_partition(partition_num=partition)
            for store in stores:
                store.revoke_partition(partition=partition)

    def init(self) -> None:
        """
        Initialize `StateStoreManager` and create a store directory
        :return:
        """
        self._init_state_dir()

    def close(self) -> None:
        """
        Close all registered stores
        """
        for topic_stores in self._stores.values():
            for store in topic_stores.values():
                store.close()

    def __enter__(self):
        self.init()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()
