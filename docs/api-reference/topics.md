<a id="quixstreams.models.topics"></a>

## quixstreams.models.topics

<a id="quixstreams.models.topics.admin"></a>

## quixstreams.models.topics.admin

<a id="quixstreams.models.topics.admin.TopicAdmin"></a>

### TopicAdmin

```python
class TopicAdmin()
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/admin.py#L31)

For performing "admin"-level operations on a Kafka cluster, mostly around topics.

Primarily used to create and inspect topic configurations.

<a id="quixstreams.models.topics.admin.TopicAdmin.__init__"></a>

<br><br>

#### TopicAdmin.\_\_init\_\_

```python
def __init__(broker_address: Union[str, ConnectionConfig],
             logger: logging.Logger = logger,
             extra_config: Optional[Mapping] = None)
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/admin.py#L38)


<br>
***Arguments:***

- `broker_address`: Connection settings for Kafka.
Accepts string with Kafka broker host and port formatted as `<host>:<port>`,
or a ConnectionConfig object if authentication is required.
- `logger`: a Logger instance to attach librdkafka logging to
- `extra_config`: optional configs (generally accepts producer configs)

<a id="quixstreams.models.topics.admin.TopicAdmin.list_topics"></a>

<br><br>

#### TopicAdmin.list\_topics

```python
def list_topics(timeout: float = -1) -> Dict[str, ConfluentTopicMetadata]
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/admin.py#L70)

Get a list of topics and their metadata from a Kafka cluster


<br>
***Arguments:***

- `timeout`: response timeout (seconds); Default infinite (-1)


<br>
***Returns:***

a dict of topic names and their metadata objects

<a id="quixstreams.models.topics.admin.TopicAdmin.inspect_topics"></a>

<br><br>

#### TopicAdmin.inspect\_topics

```python
def inspect_topics(topic_names: List[str],
                   timeout: float = 30) -> Dict[str, Optional[TopicConfig]]
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/admin.py#L81)

A simplified way of getting the topic configurations of the provided topics

from the cluster (if they exist).


<br>
***Arguments:***

- `topic_names`: a list of topic names
- `timeout`: response timeout (seconds)
>***NOTE***: `timeout` must be >0 here (expects non-neg, and 0 != inf).


<br>
***Returns:***

a dict with topic names and their respective `TopicConfig`

<a id="quixstreams.models.topics.admin.TopicAdmin.create_topics"></a>

<br><br>

#### TopicAdmin.create\_topics

```python
def create_topics(topics: List[Topic],
                  timeout: float = 30,
                  finalize_timeout: float = 60)
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/admin.py#L185)

Create the given list of topics and confirm they are ready.

Also raises an exception with detailed printout should the creation
fail (it ignores issues for a topic already existing).


<br>
***Arguments:***

- `topics`: a list of `Topic`
- `timeout`: creation acknowledge timeout (seconds)
- `finalize_timeout`: topic finalization timeout (seconds)
>***NOTE***: `timeout` must be >0 here (expects non-neg, and 0 != inf).

<a id="quixstreams.models.topics.topic"></a>

## quixstreams.models.topics.topic

<a id="quixstreams.models.topics.topic.TopicConfig"></a>

### TopicConfig

```python
@dataclasses.dataclass(eq=True, frozen=True)
class TopicConfig()
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/topic.py#L44)

Represents all kafka-level configuration for a kafka topic.

Generally used by Topic and any topic creation procedures.

<a id="quixstreams.models.topics.topic.Topic"></a>

### Topic

```python
class Topic()
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/topic.py#L85)

A definition of a Kafka topic.

Typically created with an `app = quixstreams.app.Application()` instance via
`app.topic()`, and used by `quixstreams.dataframe.StreamingDataFrame`
instance.

<a id="quixstreams.models.topics.topic.Topic.__init__"></a>

<br><br>

#### Topic.\_\_init\_\_

```python
def __init__(
        name: str,
        create_config: Optional[TopicConfig] = None,
        value_deserializer: Optional[DeserializerType] = None,
        key_deserializer: Optional[DeserializerType] = BytesDeserializer(),
        value_serializer: Optional[SerializerType] = None,
        key_serializer: Optional[SerializerType] = BytesSerializer(),
        timestamp_extractor: Optional[TimestampExtractor] = None)
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/topic.py#L94)


<br>
***Arguments:***

- `name`: topic name
- `create_config`: a `TopicConfig` to create a new topic if it does not exist
- `value_deserializer`: a deserializer type for values
- `key_deserializer`: a deserializer type for keys
- `value_serializer`: a serializer type for values
- `key_serializer`: a serializer type for keys
- `timestamp_extractor`: a callable that returns a timestamp in
milliseconds from a deserialized message.

<a id="quixstreams.models.topics.topic.Topic.create_config"></a>

<br><br>

#### Topic.create\_config

```python
@property
def create_config() -> Optional[TopicConfig]
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/topic.py#L139)

A config to create the topic

<a id="quixstreams.models.topics.topic.Topic.broker_config"></a>

<br><br>

#### Topic.broker\_config

```python
@property
def broker_config() -> TopicConfig
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/topic.py#L150)

A topic config obtained from the Kafka broker

<a id="quixstreams.models.topics.topic.Topic.row_serialize"></a>

<br><br>

#### Topic.row\_serialize

```python
def row_serialize(row: Row, key: Any) -> KafkaMessage
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/topic.py#L164)

Serialize Row to a Kafka message structure


<br>
***Arguments:***

- `row`: Row to serialize
- `key`: message key to serialize


<br>
***Returns:***

KafkaMessage object with serialized values

<a id="quixstreams.models.topics.topic.Topic.row_deserialize"></a>

<br><br>

#### Topic.row\_deserialize

```python
def row_deserialize(
    message: SuccessfulConfluentKafkaMessageProto
) -> Union[Row, List[Row], None]
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/topic.py#L204)

Deserialize incoming Kafka message to a Row.


<br>
***Arguments:***

- `message`: an object with interface of `confluent_kafka.Message`


<br>
***Returns:***

Row, list of Rows or None if the message is ignored.

<a id="quixstreams.models.topics.manager"></a>

## quixstreams.models.topics.manager

<a id="quixstreams.models.topics.manager.TopicManager"></a>

### TopicManager

```python
class TopicManager()
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L21)

The source of all topic management for a Quix Streams Application.

Intended only for internal use by Application.

To create a Topic, use Application.topic() or generate them directly.

<a id="quixstreams.models.topics.manager.TopicManager.__init__"></a>

<br><br>

#### TopicManager.\_\_init\_\_

```python
def __init__(topic_admin: TopicAdmin,
             consumer_group: str,
             timeout: float = 30,
             create_timeout: float = 60,
             auto_create_topics: bool = True)
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L42)


<br>
***Arguments:***

- `topic_admin`: an `Admin` instance (required for some functionality)
- `consumer_group`: the consumer group (of the `Application`)
- `timeout`: response timeout (seconds)
- `create_timeout`: timeout for topic creation

<a id="quixstreams.models.topics.manager.TopicManager.changelog_topics"></a>

<br><br>

#### TopicManager.changelog\_topics

```python
@property
def changelog_topics() -> Dict[Optional[str], Dict[str, Topic]]
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L82)

Note: `Topic`s are the changelogs.

returns: the changelog topic dict, {topic_name: {suffix: Topic}}

<a id="quixstreams.models.topics.manager.TopicManager.changelog_topics_list"></a>

<br><br>

#### TopicManager.changelog\_topics\_list

```python
@property
def changelog_topics_list() -> List[Topic]
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L91)

Returns a list of changelog topics

returns: the changelog topic dict, {topic_name: {suffix: Topic}}

<a id="quixstreams.models.topics.manager.TopicManager.non_changelog_topics"></a>

<br><br>

#### TopicManager.non\_changelog\_topics

```python
@property
def non_changelog_topics() -> Dict[str, Topic]
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L100)

Returns a dict with normal and repartition topics

<a id="quixstreams.models.topics.manager.TopicManager.all_topics"></a>

<br><br>

#### TopicManager.all\_topics

```python
@property
def all_topics() -> Dict[str, Topic]
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L107)

Every registered topic name mapped to its respective `Topic`.

returns: full topic dict, {topic_name: Topic}

<a id="quixstreams.models.topics.manager.TopicManager.topic_config"></a>

<br><br>

#### TopicManager.topic\_config

```python
def topic_config(num_partitions: Optional[int] = None,
                 replication_factor: Optional[int] = None,
                 extra_config: Optional[dict] = None) -> TopicConfig
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L115)

Convenience method for generating a `TopicConfig` with default settings


<br>
***Arguments:***

- `num_partitions`: the number of topic partitions
- `replication_factor`: the topic replication factor
- `extra_config`: other optional configuration settings


<br>
***Returns:***

a TopicConfig object

<a id="quixstreams.models.topics.manager.TopicManager.topic"></a>

<br><br>

#### TopicManager.topic

```python
def topic(name: str,
          value_deserializer: Optional[DeserializerType] = None,
          key_deserializer: Optional[DeserializerType] = "bytes",
          value_serializer: Optional[SerializerType] = None,
          key_serializer: Optional[SerializerType] = "bytes",
          create_config: Optional[TopicConfig] = None,
          timestamp_extractor: Optional[TimestampExtractor] = None) -> Topic
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L137)

A convenience method for generating a `Topic`. Will use default config options

as dictated by the TopicManager.


<br>
***Arguments:***

- `name`: topic name
- `value_deserializer`: a deserializer type for values
- `key_deserializer`: a deserializer type for keys
- `value_serializer`: a serializer type for values
- `key_serializer`: a serializer type for keys
- `create_config`: optional topic configurations (for creation/validation)
- `timestamp_extractor`: a callable that returns a timestamp in
milliseconds from a deserialized message.


<br>
***Returns:***

Topic object with creation configs

<a id="quixstreams.models.topics.manager.TopicManager.register"></a>

<br><br>

#### TopicManager.register

```python
def register(topic: Topic) -> Topic
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L183)

Register an already generated :class:`quixstreams.models.topics.Topic` to the topic manager.

The topic name and config can be updated by the topic manager.


<br>
***Arguments:***

- `topic`: The topic to register

<a id="quixstreams.models.topics.manager.TopicManager.repartition_topic"></a>

<br><br>

#### TopicManager.repartition\_topic

```python
def repartition_topic(operation: str,
                      topic_name: str,
                      value_deserializer: Optional[DeserializerType] = "json",
                      key_deserializer: Optional[DeserializerType] = "json",
                      value_serializer: Optional[SerializerType] = "json",
                      key_serializer: Optional[SerializerType] = "json",
                      timeout: Optional[float] = None) -> Topic
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L202)

Create an internal repartition topic.


<br>
***Arguments:***

- `operation`: name of the GroupBy operation (column name or user-defined).
- `topic_name`: name of the topic the GroupBy is sourced from.
- `value_deserializer`: a deserializer type for values; default - JSON
- `key_deserializer`: a deserializer type for keys; default - JSON
- `value_serializer`: a serializer type for values; default - JSON
- `key_serializer`: a serializer type for keys; default - JSON
- `timeout`: config lookup timeout (seconds); Default 30


<br>
***Returns:***

`Topic` object (which is also stored on the TopicManager)

<a id="quixstreams.models.topics.manager.TopicManager.changelog_topic"></a>

<br><br>

#### TopicManager.changelog\_topic

```python
def changelog_topic(topic_name: Optional[str],
                    store_name: str,
                    config: Optional[TopicConfig] = None,
                    timeout: Optional[float] = None) -> Topic
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L242)

Performs all the logic necessary to generate a changelog topic based on an

optional "source topic" (aka input/consumed topic).

Its main goal is to ensure partition counts of the to-be generated changelog
match the source topic, and ensure the changelog topic is compacted. Also
enforces the serialization type. All `Topic` objects generated with this are
stored on the TopicManager.

If source topic already exists, defers to the existing topic settings, else
uses the settings as defined by the `Topic` (and its defaults) as generated
by the `TopicManager`.

In general, users should NOT need this; an Application knows when/how to
generate changelog topics. To turn off changelogs, init an Application with
"use_changelog_topics"=`False`.


<br>
***Arguments:***

- `topic_name`: name of consumed topic (app input topic)
> NOTE: normally contain any prefixes added by TopicManager.topic()
- `store_name`: name of the store this changelog belongs to
(default, rolling10s, etc.)
- `config`: the changelog topic configuration. Default to `topic_name` configuration or TopicManager default
- `timeout`: config lookup timeout (seconds); Default 30


<br>
***Returns:***

`Topic` object (which is also stored on the TopicManager)

<a id="quixstreams.models.topics.manager.TopicManager.validate_all_topics"></a>

<br><br>

#### TopicManager.validate\_all\_topics

```python
def validate_all_topics()
```

[[VIEW SOURCE]](https://github.com/quixio/quix-streams/blob/main/quixstreams/models/topics/manager.py#L311)

Validates that all topics have ".broker_config" set
and changelog topics have correct numbers of partitions and replication factors.

Issues are pooled and raised as an Exception once inspections are complete.

<a id="quixstreams.models.topics.exceptions"></a>

## quixstreams.models.topics.exceptions

