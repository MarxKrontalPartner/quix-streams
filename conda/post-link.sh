# Use pip to install packages missing from conda

$PREFIX/bin/pip install \
'rocksdict>=0.3,<0.4' \
'protobuf>=5.27.2,<6.0' \
'influxdb3-python>=0.7,<1.0' \
'pyiceberg[pyarrow,glue]>=0.7,<0.8' \
'redis[hiredis]>=5.2.0,<6' \
'confluent-kafka[avro,json,protobuf,schemaregistry]>=2.8.2,<2.9'
