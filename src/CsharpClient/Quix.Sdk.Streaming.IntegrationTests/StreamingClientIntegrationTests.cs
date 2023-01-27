using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Confluent.Kafka;
using FluentAssertions;
using Quix.Sdk.Process.Configuration;
using Quix.Sdk.Process.Models;
using Quix.Sdk.Streaming.Configuration;
using Quix.Sdk.Streaming;
using Quix.Sdk.Process.Models.Utility;
using Xunit;
using System;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Quix.Sdk.Process.Kafka;
using Quix.TestBase.Extensions;
using Xunit.Abstractions;
using AutoOffsetReset = Quix.Sdk.Process.Kafka.AutoOffsetReset;

namespace Quix.Sdk.Streaming.IntegrationTests
{
    [Collection("Kafka Container Collection")]
    public class StreamingClientIntegrationTests
    {
        private readonly ITestOutputHelper output;
        private readonly StreamingClient client;
        private int MaxTestRetry = 3;

        public StreamingClientIntegrationTests(ITestOutputHelper output, KafkaDockerTestFixture kafkaDockerTestFixture)
        {
            this.output = output;
            Quix.Sdk.Logging.Factory = output.CreateLoggerFactory();
            client = new StreamingClient(kafkaDockerTestFixture.BrokerList, kafkaDockerTestFixture.SecurityOptions);
        }
        
        private static ParameterDataRaw GenerateParameterData(int offset)
        {
            var vals = Enumerable.Range(0, 2).ToDictionary(k => "p" + k,
                s => Enumerable.Range(offset, 10).Select(s2 => new double?(s2 * 10.0)).ToArray());
            var stringVals = vals.ToDictionary(x => x.Key, x => x.Value.Select(y=> y.ToString()).ToArray());
            var binaryVals = stringVals.ToDictionary(x=> x.Key, x => x.Value.Select(y=> UTF8Encoding.UTF8.GetBytes(y)).ToArray());
            var epoch = 100000;
            return new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = Enumerable.Range(offset, 10).Select(s => (long)s + epoch).ToArray(),
                NumericValues = vals,
                StringValues = stringVals,
                BinaryValues = binaryVals
            };
        }

        [Fact]
        public void StreamReadAndWrite()
        {
            RunTest(() =>
            {
                var inputTopic = client.OpenInputTopic("integration-tests", "quix.tests-sdk-tests");
                var outputTopic = client.OpenOutputTopic("integration-tests");

                IList<ParameterDataRaw> data = new List<ParameterDataRaw>();
                IList<EventDataRaw> events = new List<EventDataRaw>();
                var streamStarted = false;
                var streamEnded = false;
                StreamProperties streamProperties = null;
                var parameterDefinitionsChanged = false;
                var eventDefinitionsChanged = false;
                string streamId = null;

                inputTopic.OnStreamReceived += (s, e) =>
                {
                    if (e.StreamId != streamId)
                    {
                        return;
                    }

                    streamStarted = true;
                    (e as IStreamReaderInternal).OnParameterData += (s2, e2) => data.Add(e2);
                    (e as IStreamReaderInternal).OnParameterDefinitionsChanged += (s2, e2) => parameterDefinitionsChanged = true;
                    (e as IStreamReaderInternal).OnEventDefinitionsChanged += (s2, e2) => eventDefinitionsChanged = true;
                    (e as IStreamReaderInternal).OnStreamPropertiesChanged += (s2, e2) => streamProperties = e2;
                    (e as IStreamReaderInternal).OnEventData += (s2, e2) => events.Add(e2);

                    e.OnStreamClosed += (s2, e2) => streamEnded = true;
                };

                inputTopic.StartReading();

                using (var stream = outputTopic.CreateStream())
                {
                    streamId = stream.StreamId;

                    stream.Properties.Name = "Volvo car telemetry";
                    stream.Properties.Location = "Car telemetry/Vehicles/Volvo";
                    stream.Properties.AddParent("1234");
                    stream.Properties.Metadata["test_key"] = "test_value";
                    stream.Properties.TimeOfRecording = new DateTime(2018, 01, 01);
                    stream.Properties.Flush();

                    SpinWait.SpinUntil(() => streamStarted, 20000);
                    SpinWait.SpinUntil(() => streamProperties != null, 2000);

                    Assert.True(streamStarted, "Stream is not started on reader.");
                    Assert.Equal("Car telemetry/Vehicles/Volvo", streamProperties.Location);
                    Assert.Equal("test_value", streamProperties.Metadata["test_key"]);
                    Assert.Equal("1234", streamProperties.Parents.First());
                    Assert.Equal(new DateTime(2018, 01, 01), streamProperties.TimeOfRecording);

                    var expectedParameterDefinitions = new ParameterDefinitions
                    {
                        Parameters = new List<ParameterDefinition>
                        {
                            {
                                new ParameterDefinition
                                {
                                    Id = "p1",
                                    Name = "P0 parameter",
                                    MinimumValue = 0,
                                    MaximumValue = 10,
                                    Unit = "kmh"
                                }
                            },
                            {
                                new ParameterDefinition
                                {
                                    Id = "p2",
                                    Name = "P1 parameter",
                                    MinimumValue = 0,
                                    MaximumValue = 10,
                                    Unit = "kmh"
                                }
                            }
                        }
                    };

                    var expectedEventDefinitions = new EventDefinitions
                    {
                        Events = new List<EventDefinition>()
                        {
                            new EventDefinition
                            {
                                Id = "evid3"
                            },
                            new EventDefinition
                            {
                                Id = "evid4"
                            }
                        }
                    };


                    (stream as IStreamWriterInternal).Write(expectedParameterDefinitions);

                    SpinWait.SpinUntil(() => parameterDefinitionsChanged, 2000);

                    Assert.True(parameterDefinitionsChanged, "Parameter definitions event not reached reader.");

                    (stream as IStreamWriterInternal).Write(expectedEventDefinitions);

                    SpinWait.SpinUntil(() => eventDefinitionsChanged, 2000);

                    Assert.True(eventDefinitionsChanged, "Event definitions event not reached reader.");

                    var expectedData = new List<ParameterDataRaw>();
                    expectedData.Add(GenerateParameterData(0));
                    expectedData.Add(GenerateParameterData(10));

                    (stream as IStreamWriterInternal).Write(expectedData);

                    SpinWait.SpinUntil(() => data.Count == 2, 5000);

                    Assert.Equal(2, data.Count);
                    Assert.Equal(data.FirstOrDefault()?.Timestamps.FirstOrDefault(), expectedData.FirstOrDefault().Timestamps.FirstOrDefault());
                    Assert.Equal(data.LastOrDefault()?.Timestamps.LastOrDefault(), expectedData.LastOrDefault().Timestamps.LastOrDefault());

                    // test events
                    var inputEvents = new EventDataRaw[]
                    {
                        new EventDataRaw
                        {
                            Id = "abc",
                            Tags = new Dictionary<string, string>()
                            {
                                {"one", "two"}
                            },
                            Value = "Iamvalue",
                            Timestamp = 123456789
                        },
                        new EventDataRaw
                        {
                            Id = "efg",
                            Tags = new Dictionary<string, string>()
                            {
                                {"three", "fwo"}
                            },
                            Value = "Iamvalue2",
                            Timestamp = 123456790
                        }
                    };
                    // test single event
                    (stream as IStreamWriterInternal).Write(inputEvents[0]);
                    SpinWait.SpinUntil(() => events.Count == 1, 2000);
                    events[0].Should().BeEquivalentTo(inputEvents[0]);
                    events.Clear();

                    //test multiple event
                    (stream as IStreamWriterInternal).Write(inputEvents);
                    SpinWait.SpinUntil(() => events.Count == 2, 2000);
                    events.Should().BeEquivalentTo(inputEvents);

                    stream.Close();

                    SpinWait.SpinUntil(() => streamEnded, 2000);

                    Assert.True(streamEnded, "Stream end event not reached reader.");
                }

                inputTopic.Dispose();
            });
        }

        [Fact]
        public void StreamReadAndWriteBuilders()
        {
            RunTest(() =>
            {
                var inputTopic = client.OpenInputTopic("integration-tests", "quix.tests-sdk-tests");
                var outputTopic = client.OpenOutputTopic("integration-tests");

                IList<ParameterDataRaw> data = new List<ParameterDataRaw>();
                IList<EventDataRaw> events = new List<EventDataRaw>();
                var streamStarted = false;
                var streamEnded = false;
                StreamProperties streamProperties = null;
                List<ParameterDefinition> parameterDefinitions = null;
                List<EventDefinition> eventDefinitions = null;
                string streamId = null;

                inputTopic.OnStreamReceived += (s, e) =>
                {
                    if (e.StreamId != streamId)
                    {
                        return;
                    }

                    streamStarted = true;
                    (e as IStreamReaderInternal).OnParameterData += (s2, e2) => data.Add(e2);
                    (e as IStreamReaderInternal).OnParameterDefinitionsChanged += (s2, e2) => parameterDefinitions = e2.Parameters ?? parameterDefinitions;
                    (e as IStreamReaderInternal).OnEventDefinitionsChanged += (s2, e2) => eventDefinitions = e2.Events ?? eventDefinitions;
                    (e as IStreamReaderInternal).OnStreamPropertiesChanged += (s2, e2) => streamProperties = e2;
                    (e as IStreamReaderInternal).OnEventData += (s2, e2) => events.Add(e2);

                    e.OnStreamClosed += (s2, e2) => streamEnded = true;
                };

                inputTopic.StartReading();

                using (var stream = outputTopic.CreateStream())
                {
                    streamId = stream.StreamId;
                    output.WriteLine($"New stream created: {streamId}");

                    stream.Properties.Name = "Volvo car telemetry";
                    stream.Properties.Location = "Car telemetry/Vehicles/Volvo";
                    stream.Properties.AddParent("1234");
                    stream.Properties.Metadata["test_key"] = "test_value";

                    SpinWait.SpinUntil(() => streamStarted, 20000);
                    Assert.True(streamStarted, "Stream is not started on reader.");
                    SpinWait.SpinUntil(() => streamProperties != null, 2000);

                    Assert.Equal("Car telemetry/Vehicles/Volvo", streamProperties.Location);
                    Assert.Equal("test_value", streamProperties.Metadata["test_key"]);
                    Assert.Equal("1234", streamProperties.Parents.First());

                    var expectedParameterDefinitions = new ParameterDefinitions
                    {
                        Parameters = new List<ParameterDefinition>
                        {
                            {
                                new ParameterDefinition
                                {
                                    Id = "p1",
                                    Name = "P0 parameter",
                                    Description = "Desc 1",
                                    MinimumValue = 0,
                                    MaximumValue = 10,
                                    Unit = "kmh"
                                }
                            },
                            {
                                new ParameterDefinition
                                {
                                    Id = "p2",
                                    Name = "P1 parameter",
                                    Description = "Desc 2",
                                    MinimumValue = 0,
                                    MaximumValue = 10,
                                    Unit = "kmh"
                                }
                            }
                        },
                    };

                    var expectedEventDefinitions = new EventDefinitions
                    {
                        Events = new List<EventDefinition>()
                        {
                            new EventDefinition
                            {
                                Id = "evid3",
                                Name = "evName3"
                            },
                            new EventDefinition
                            {
                                Id = "evid4",
                                Name = "evName4"
                            }
                        }
                    };

                    stream.Parameters.AddDefinition("p1", "P0 parameter", "Desc 1").SetRange(0, 10).SetUnit("kmh");
                    stream.Parameters.AddDefinition("p2", "P1 parameter", "Desc 2").SetRange(0, 10).SetUnit("kmh");

                    stream.Events.AddDefinition("evid3", "evName3");
                    stream.Events.AddDefinition("evid4", "evName4");


                    SpinWait.SpinUntil(() => parameterDefinitions != null && eventDefinitions != null, 2000);

                    Assert.True(parameterDefinitions != null && eventDefinitions != null, "Parameter / Event definitions event not reached reader.");

                    expectedParameterDefinitions.Parameters.Should().BeEquivalentTo(parameterDefinitions);
                    expectedEventDefinitions.Events.Should().BeEquivalentTo(eventDefinitions);

                    var expectedData = new List<ParameterDataRaw>();
                    expectedData.Add(GenerateParameterData(0));
                    expectedData.Add(GenerateParameterData(10));

                    stream.Parameters.Buffer.Epoch = ((long) 100000).FromUnixNanoseconds();
                    stream.Parameters.Buffer.PacketSize = 10;
                    for (var i = 0; i < 20; i++)
                    {
                        stream.Parameters.Buffer.AddTimestampNanoseconds(i)
                            .AddValue("p0", i)
                            .AddValue("p1", i)
                            .AddValue("p2", i)
                            .Write();
                    }

                    SpinWait.SpinUntil(() => data.Count == 2, 2000);

                    Assert.Equal(2, data.Count);
                    Assert.Equal(data.FirstOrDefault()?.Timestamps.FirstOrDefault(), expectedData.FirstOrDefault().Timestamps.FirstOrDefault());
                    Assert.Equal(data.LastOrDefault()?.Timestamps.LastOrDefault(), expectedData.LastOrDefault().Timestamps.LastOrDefault());

                    var now = DateTime.UtcNow;
                    // test events
                    var inputEvents = new EventDataRaw[]
                    {
                        new EventDataRaw
                        {
                            Id = "abc",
                            Tags = new Dictionary<string, string>()
                            {
                                {"one", "two"}
                            },
                            Value = "Iamvalue",
                            Timestamp = 123456789
                        },
                        new EventDataRaw
                        {
                            Id = "efg",
                            Tags = new Dictionary<string, string>()
                            {
                                {"three", "fwo"}
                            },
                            Value = "Iamvalue2",
                            Timestamp = 123456790
                        },
                        new EventDataRaw
                        {
                            Id = "datetimetest",
                            Tags = new Dictionary<string, string>() { },
                            Value = "Iamvalue3",
                            Timestamp = now.ToUnixNanoseconds()
                        },
                        new EventDataRaw
                        {
                            Id = "timespan",
                            Tags = new Dictionary<string, string>() { },
                            Value = "Iamvalue4",
                            Timestamp = now.Add(TimeSpan.FromSeconds(10)).ToUnixNanoseconds()
                        }
                    };



                    // test single event
                    stream.Events.AddTimestampNanoseconds(123456789)
                        .AddValue("abc", "Iamvalue")
                        .AddTag("one", "two")
                        .Write();

                    SpinWait.SpinUntil(() => events.Count == 1, 2000);
                    events[0].Should().BeEquivalentTo(inputEvents[0]);
                    events.Clear();

                    //test multiple event
                    stream.Events.AddTimestampNanoseconds(123456789)
                        .AddValue("abc", "Iamvalue")
                        .AddTag("one", "two")
                        .Write();
                    stream.Events.AddTimestampNanoseconds(123456790)
                        .AddValue("efg", "Iamvalue2")
                        .AddTag("three", "fwo")
                        .Write();
                    stream.Events.Epoch = now;
                    stream.Events.AddTimestamp(now)
                        .AddValue("datetimetest", "Iamvalue3")
                        .Write();
                    stream.Events.AddTimestamp(TimeSpan.FromSeconds(10))
                        .AddValue("timespan", "Iamvalue4")
                        .Write();

                    SpinWait.SpinUntil(() => events.Count == 4, 2000);
                    events.Should().BeEquivalentTo(inputEvents);

                    stream.Close();

                    SpinWait.SpinUntil(() => streamEnded, 2000);

                    Assert.True(streamEnded, "Stream end event not reached reader.");

                    inputTopic.Dispose();
                }
            });
        }

        [Fact]
        public void StreamCloseAndReopenSameStream_ShouldRaiseEventAsExpected()
        {
            RunTest(() =>
            {
                var inputTopic = client.OpenInputTopic("integration-tests", "quix.tests-sdk-tests");
                var outputTopic = client.OpenOutputTopic("integration-tests");

                IList<ParameterDataRaw> data = new List<ParameterDataRaw>();
                IList<EventDataRaw> events = new List<EventDataRaw>();
                var streamStarted = false;
                var streamEnded = false;
                string streamId = null;

                inputTopic.OnStreamReceived += (s, e) =>
                {
                    if (e.StreamId != streamId)
                    {
                        return;
                    }

                    streamStarted = true;
                    (e as IStreamReaderInternal).OnParameterData += (s2, e2) => data.Add(e2);
                    e.OnStreamClosed += (s2, e2) => streamEnded = true;
                };

                inputTopic.StartReading();

                using (var stream = outputTopic.CreateStream())
                {
                    streamId = stream.StreamId;

                    stream.Parameters.Buffer.AddTimestampNanoseconds(100)
                        .AddValue("p0", 1)
                        .AddValue("p1", 2)
                        .AddValue("p2", 3)
                        .Write();

                    SpinWait.SpinUntil(() => streamStarted, 20000);
                    Assert.True(streamStarted, "Stream is not started on reader.");

                    SpinWait.SpinUntil(() => data.Count == 1, 2000);

                    Assert.Equal(1, data.Count);


                    // Close stream
                    stream.Close();

                    SpinWait.SpinUntil(() => streamEnded, 2000);

                    Assert.True(streamEnded, "Stream end event not reached reader.");

                    streamStarted = false;

                    // Second stream
                    using (var stream2 = outputTopic.CreateStream(streamId))
                    {
                        stream2.Parameters.Buffer.AddTimestampNanoseconds(100)
                            .AddValue("p0", 1)
                            .AddValue("p1", 2)
                            .AddValue("p2", 3)
                            .Write();
                    }

                    SpinWait.SpinUntil(() => streamStarted, 2000);

                    Assert.True(streamStarted, "Stream is not started on reader.");
                }

                inputTopic.Dispose();
            });
        }
        
        [Fact]
        public void StreamGetOrCreateStream_ShouldNotThrowException()
        {
            RunTest(() =>
            {
                var outputTopic = client.OpenOutputTopic("integration-tests");
                var result = Parallel.For(0, 1000, parallelOptions: new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 20
                }, (loop) => { outputTopic.GetOrCreateStream("testme"); });

                result.IsCompleted.Should().BeTrue();
                var stream = outputTopic.GetOrCreateStream("testme");
                var stream2 = outputTopic.GetOrCreateStream("testme");
                stream.Should().BeSameAs(stream2);
            });
        }
        
        [Fact]
        public void ReadingAStreamWithSameConsumerGroup_ShouldGetRevokedOnOne()
        {
            RunTest(() =>
            {
                var outputTopic = client.OpenOutputTopic("integration-tests");

                var testGroup = "quix.tests-" + Guid.NewGuid().GetHashCode().ToString("X");
                var inputTopic1 = client.OpenInputTopic("integration-tests", testGroup, autoOffset: AutoOffsetReset.Latest);

                var expectedStreamCount = 1;
                long streamsRevoked = 0;
                long streamsReceived = 0;
                var cts = new CancellationTokenSource();

                var streams = new List<IStreamWriter>();
                for (var i = 0; i <= expectedStreamCount; i++)
                {
                    streams.Add(outputTopic.CreateStream());
                }

                var writerTask = Task.Run(async () =>
                {

                    while (!cts.IsCancellationRequested)
                    {
                        this.output.WriteLine("Generating data for streams");
                        foreach (var stream in streams)
                        {
                            stream.Parameters.Buffer.AddTimestamp(DateTime.UtcNow)
                                .AddValue("p0", 1)
                                .Write();
                            stream.Parameters.Buffer.Flush();
                        }

                        await Task.Delay(1000, cts.Token);
                    }
                });

                inputTopic1.OnStreamReceived += (sender, sr) =>
                {
                    Interlocked.Increment(ref streamsReceived);
                };
                inputTopic1.OnStreamsRevoked += (sender, streams) =>
                {
                    foreach (var streamReader in streams)
                    {
                        Interlocked.Increment(ref streamsRevoked);
                    }
                };
                inputTopic1.StartReading();
                SpinWait.SpinUntil(() => streamsReceived > 0, TimeSpan.FromSeconds(20));
                streamsReceived.Should().BeGreaterThan(0);



                long streamsReceived2 = 0;
                for (var index = 0; index <= 5; index++)
                {
                    if (streamsReceived2 > 0) break;
                    var inputTopic2 = client.OpenInputTopic("integration-tests", testGroup, autoOffset: AutoOffsetReset.Latest);
                    inputTopic2.OnStreamReceived += (sender, sr) => { Interlocked.Increment(ref streamsReceived2); };
                    inputTopic2.StartReading();
                }

                SpinWait.SpinUntil(() => streamsReceived2 > 0, TimeSpan.FromSeconds(20));
                streamsReceived2.Should().BeGreaterThan(0);
                SpinWait.SpinUntil(() => streamsRevoked > 0, TimeSpan.FromSeconds(10000));
                streamsRevoked.Should().BeGreaterThan(0);
            });
        }

        private async Task RunTest(Func<Task> test)
        {
            var count = 0;
            while (count < MaxTestRetry)
            {
                try
                {
                    count++;
                    await test();
                    return; // success
                }
                catch
                {
                    this.output.WriteLine($"Attempt {count} failed");
                }
            }
        }
        
        private void RunTest(Action test)
        {
            var count = 0;
            while (count < MaxTestRetry)
            {
                try
                {
                    count++;
                    test();
                    return; // success
                }
                catch
                {
                    this.output.WriteLine($"Attempt {count} failed");
                }
            }
        }

    }
}