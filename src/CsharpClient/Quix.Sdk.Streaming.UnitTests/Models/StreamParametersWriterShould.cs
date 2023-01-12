﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Quix.Sdk.Process.Models;
using Quix.Sdk.Process.Models.Utility;
using NSubstitute;
using Xunit;
using FluentAssertions.Equivalency;

namespace Quix.Sdk.Streaming.UnitTests.Models
{
    public class StreamParametersWriterShould
    {

        [Fact]
        public void AddValue_NumericAndString_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x=> sentData.Add(x)));

            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            writer.Buffer.AddTimestampNanoseconds(100)
                .AddValue("test_param", 1)
                .AddValue("test_param2", "val")
                .Write();
            writer.Buffer.AddTimestampNanoseconds(200)
                .AddValue("test_param2", "val2")
                .Write();
            writer.Buffer.AddTimestampNanoseconds(300)
                .AddValue("test_param", 3)
                .Write();

            writer.Buffer.Flush();
            
            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 100 + epoch.ToUnixNanoseconds(), 200 + epoch.ToUnixNanoseconds(), 300 + epoch.ToUnixNanoseconds() },
                NumericValues = new Dictionary<string, double?[]>
                {
                    {"test_param", new double?[] {1, null, 3}}
                },
                StringValues = new Dictionary<string, string[]>
                {
                    {
                        "test_param2", new[] {"val", "val2", null}
                    }
                },
                TagValues = new Dictionary<string, string[]>()
            });
        }
        
        [Fact]
        public void AddValue_WithVariableTags_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x=> sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            writer.Buffer.AddTimestampNanoseconds(100)
                .AddValue("test_param", 1)
                .AddTag("tag1", "tag1val1")
                .Write();

            writer.Buffer.DefaultTags = new Dictionary<string, string>()
            {
                {"tag2", "tag2val"}
            };

            writer.Buffer.AddTimestampNanoseconds(200)
                .AddValue("test_param", 2)
                .AddTag("tag3", "tag3val1")
                .Write();
            writer.Buffer.AddTimestampNanoseconds(300)
                .AddValue("test_param", 3)
                .AddTag("tag2", "tag2val2")
                .Write();

            writer.Buffer.Flush();
            
            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 100 + epoch.ToUnixNanoseconds(), 200 + epoch.ToUnixNanoseconds(), 300 + epoch.ToUnixNanoseconds() },
                NumericValues = new Dictionary<string, double?[]>
                {
                    {"test_param", new double?[] {1, 2, 3}}
                },
                TagValues = new Dictionary<string, string[]>
                {
                    {"tag1", new []{"tag1val1", null, null}},
                    {"tag2", new []{null, "tag2val", "tag2val2"}},
                    {"tag3", new []{null, "tag3val1", null}}
                },
                StringValues = new Dictionary<string, string[]>()
            });
        }
        
        [Fact]
        public void AddValue_WithVariableBinaryParams_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestampNanoseconds(100)
                .AddValue("test_param", new byte[] {1,2,3});
            parameterData.AddTimestampNanoseconds(200)
                .AddValue("test_param2", new byte[] {3,4,5});
            parameterData.AddTimestampNanoseconds(300)
                .AddValue("test_param3", new byte[] {6,7,8});
            writer.Buffer.Write(parameterData);

            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 100 + epoch.ToUnixNanoseconds(), 200 + epoch.ToUnixNanoseconds(), 300 + epoch.ToUnixNanoseconds() },
                TagValues = new Dictionary<string, string[]>(),
                NumericValues = new Dictionary<string, double?[]>(),
                StringValues = new Dictionary<string, string[]>(),
                BinaryValues = new Dictionary<string, byte[][]>
                {
                    {"test_param", new [] {new byte[] {1,2,3}, null, null}},
                    {"test_param2", new [] {null, new byte[] {3,4,5}, null}},
                    {"test_param3", new [] {null, null, new byte[] {6,7,8}}}
                }
            });
        }

        [Fact]
        public void AddValue_WithVariableNumericParams_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestampNanoseconds(100)
                .AddValue("test_param", 1);
            parameterData.AddTimestampNanoseconds(200)
                .AddValue("test_param2", 2);
            parameterData.AddTimestampNanoseconds(300)
                .AddValue("test_param3", 3);
            writer.Buffer.Write(parameterData);

            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 100 + epoch.ToUnixNanoseconds(), 200 + epoch.ToUnixNanoseconds(), 300 + epoch.ToUnixNanoseconds() },
                NumericValues = new Dictionary<string, double?[]>
                {
                    {"test_param", new double?[] {1, null, null}},
                    {"test_param2", new double?[] {null, 2, null}},
                    {"test_param3", new double?[] {null, null, 3}}
                },
                TagValues = new Dictionary<string, string[]>(),
                StringValues = new Dictionary<string, string[]>()
            });
        }

        [Fact]
        public void AddValue_WithVariableStringParams_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestampNanoseconds(100)
                .AddValue("test_param", "one");
            parameterData.AddTimestampNanoseconds(200)
                .AddValue("test_param2", "two");
            parameterData.AddTimestampNanoseconds(300)
                .AddValue("test_param3", "three");
            writer.Buffer.Write(parameterData);
            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 100 + epoch.ToUnixNanoseconds(), 200 + epoch.ToUnixNanoseconds(), 300 + epoch.ToUnixNanoseconds() },
                StringValues = new Dictionary<string, string[]>
                {
                    {"test_param", new [] {"one", null, null}},
                    {"test_param2", new [] {null, "two", null}},
                    {"test_param3", new [] {null, null, "three"}}
                },
                TagValues = new Dictionary<string, string[]>(),
                NumericValues = new Dictionary<string, double?[]>()
            });
        }

        [Fact]
        public void AddValue_WithDateTime_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestamp(new DateTime( 2020, 01, 01, 1, 2, 3))
                .AddValue("test_param", "one");
            writer.Buffer.Write(parameterData);
            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { new DateTime(2020, 01, 01, 1, 2, 3).ToUnixNanoseconds() },
                StringValues = new Dictionary<string, string[]>
                {
                    {"test_param", new [] {"one"}},
                },
                TagValues = new Dictionary<string, string[]>(),
                NumericValues = new Dictionary<string, double?[]>()
            });
        }

        [Fact]
        public void AddValue_WithTimeSpan_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestamp(new TimeSpan(2, 3, 4, 5))
                .AddValue("test_param", "one");
            writer.Buffer.Write(parameterData);
            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { new TimeSpan(2, 3, 4, 5).ToNanoseconds() + epoch.ToUnixNanoseconds() },
                StringValues = new Dictionary<string, string[]>
                {
                    {"test_param", new [] {"one"}},
                },
                TagValues = new Dictionary<string, string[]>(),
                NumericValues = new Dictionary<string, double?[]>()
            });
        }

        [Fact]
        public void AddValue_WithNanoseconds_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestampNanoseconds(1231123)
                .AddValue("test_param", "one");
            writer.Buffer.Write(parameterData);
            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 1231123 + epoch.ToUnixNanoseconds() },
                StringValues = new Dictionary<string, string[]>
                {
                    {"test_param", new [] {"one"}},
                },
                TagValues = new Dictionary<string, string[]>(),
                NumericValues = new Dictionary<string, double?[]>()
            });
        }

        [Fact]
        public void AddValue_WithMilliseconds_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestampMilliseconds(1231123)
                .AddValue("test_param", "one");
            writer.Buffer.Write(parameterData);
            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 1231123000000 + epoch.ToUnixNanoseconds() },
                StringValues = new Dictionary<string, string[]>
                {
                    {"test_param", new [] {"one"}},
                },
                TagValues = new Dictionary<string, string[]>(),
                NumericValues = new Dictionary<string, double?[]>()
            });
        }

        [Fact]
        public void AddValue_WithMultipleEpoch_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            writer.Buffer.Epoch = new DateTime(2000, 01, 01);

            // Act
            var the2000dt = new DateTime(2000, 01, 01);
            var the2001dt = new DateTime(2001, 01, 01);
            var the2002dt = new DateTime(2002, 01, 01);

            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestampNanoseconds(100)
                .AddValue("test_param1", 1);
            parameterData.AddTimestampNanoseconds(200)
                .AddValue("test_param2", 2);
            parameterData.AddTimestampNanoseconds(300)
                .AddValue("test_param3", 3);
            parameterData.AddTimestampNanoseconds(400)
                .AddValue("test_param4", 4);
            parameterData.AddTimestamp(new DateTime(1998, 01, 01, 01, 02, 03))
                .AddValue("test_param5", 5);
            writer.Buffer.Write(parameterData);

            writer.Buffer.AddTimestampNanoseconds(600)
                .AddValue("test_param6", 6)
                .Write();
            writer.Buffer.AddTimestamp(new DateTime(1998, 02, 01, 01, 02, 03))
                .AddValue("test_param7", 7)
                .Write();

            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] 
                {
                    writer.Buffer.Epoch.ToUnixNanoseconds() + 100,
                    writer.Buffer.Epoch.ToUnixNanoseconds() + 200,
                    writer.Buffer.Epoch.ToUnixNanoseconds() + 300, 
                    writer.Buffer.Epoch.ToUnixNanoseconds() + 400, 
                    new DateTime(1998, 01, 01, 01, 02, 03).ToUnixNanoseconds(),
                    writer.Buffer.Epoch.ToUnixNanoseconds() + 600,
                    new DateTime(1998, 02, 01, 01, 02, 03).ToUnixNanoseconds(),
                },
                NumericValues = new Dictionary<string, double?[]>
                {
                    {"test_param1", new double?[] {1, null, null, null, null, null, null}},
                    {"test_param2", new double?[] {null, 2, null, null, null, null, null}},
                    {"test_param3", new double?[] {null, null, 3, null, null, null, null}},
                    {"test_param4", new double?[] {null, null, null, 4, null, null, null}},
                    {"test_param5", new double?[] {null, null, null, null, 5, null, null}},
                    {"test_param6", new double?[] {null, null, null, null, null, 6, null}},
                    {"test_param7", new double?[] {null, null, null, null, null, null, 7}}
                },
                TagValues = new Dictionary<string, string[]>(),
                StringValues = new Dictionary<string, string[]>()
            });
        }

        [Fact]
        public void WriteData_ToBufferTwice_ShouldUpdateTimestampsWithEpochOnFirstWrite()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestampNanoseconds(1231123)
                .AddValue("test_param", "one");

            writer.Buffer.Write(parameterData);
            writer.Buffer.Write(parameterData);

            writer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var data = sentData[0];
            data.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 1231123 + epoch.ToUnixNanoseconds() },
                StringValues = new Dictionary<string, string[]>
                {
                    {"test_param", new [] {"one"}},
                },
                TagValues = new Dictionary<string, string[]>(),
                NumericValues = new Dictionary<string, double?[]>()
            });
        }

        [Fact]
        public void WriteData_DirectlyToWriterWithEpoch_ShouldWriteExpected()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var parameters = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            var epoch = new DateTime(2000, 01, 01);
            streamWriter.Epoch = epoch;

            // Act
            var parameterData = new Streaming.Models.ParameterData();
            parameterData.AddTimestampNanoseconds(1231123)
                .AddValue("test_param", "one");

            parameters.Write(parameterData);

            // Assert
            sentData.Count.Should().Be(1);
            sentData[0].Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 1231123 + epoch.ToUnixNanoseconds() },
                StringValues = new Dictionary<string, string[]>
                {
                    {"test_param", new [] {"one"}},
                },
                TagValues = new Dictionary<string, string[]>(),
                NumericValues = new Dictionary<string, double?[]>()
            });
        }

        [Fact]
        public void AddValue_WithTags_AndConvertTo3rdLayerAgain_ShouldResultWithoutNullTags()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentData = new List<ParameterDataRaw>();
            streamWriter.Write(Arg.Do<ParameterDataRaw>(x => sentData.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = null;
            writer.Buffer.PacketSize = 100;
            var epoch = new DateTime(2000, 01, 01);
            writer.Buffer.Epoch = epoch;

            // Act
            var incomingData = new Streaming.Models.ParameterData();
            incomingData.AddTimestampNanoseconds(100)
                .AddValue("test_param", 1)
                .AddTag("tag1", "tag1val1");

            incomingData.AddTimestampNanoseconds(200)
                .AddValue("test_param", 2)
                .AddValue("test_param2", 3);

            incomingData.AddTimestampNanoseconds(300)
                .AddValue("test_param3", "4");

            writer.Buffer.Write(incomingData);

            writer.Buffer.Flush();

            // Assert
            sentData.Count.Should().Be(1);
            var processData = sentData[0];
            processData.Should().BeEquivalentTo(new ParameterDataRaw
            {
                Epoch = 0,
                Timestamps = new long[] { 100 + epoch.ToUnixNanoseconds(), 200 + epoch.ToUnixNanoseconds(), 300 + epoch.ToUnixNanoseconds() },
                NumericValues = new Dictionary<string, double?[]>
                {
                    {"test_param", new double?[] {1, 2, null}},
                    {"test_param2", new double?[] {null, 3, null}},
                },
                TagValues = new Dictionary<string, string[]>
                {
                    {"tag1", new []{"tag1val1", null, null}},
                },
                StringValues = new Dictionary<string, string[]>()
                {
                    {"test_param3", new string[] {null, null, "4"}}
                },
            });

            // Act
            var data = new Streaming.Models.ParameterData(processData);
            data.Should().BeEquivalentTo(incomingData, options => options.Including(info => info.WhichGetterHas(FluentAssertions.Common.CSharpAccessModifier.Public)));
        }


        [Fact]
        public void AddDefinition_WithLocation_ShouldProduceExpectedDefinitions()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var sentDefinitions = new List<ParameterDefinitions>();
            streamWriter.Write(Arg.Do<ParameterDefinitions>(x => sentDefinitions.Add(x)));
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);
            writer.Buffer.BufferTimeout = 500000; // maybe implement disable?

            // Act
            writer.DefaultLocation = ""; // root
            writer.AddDefinition("Param1", "Parameter One", "The parameter one").SetUnit("%").SetRange(-10.43, 100.123).SetFormat("{0}%").SetCustomProperties("custom prop");
            writer.AddLocation("/some/nested/group").AddDefinition("param2");
            writer.AddLocation("some/nested/group").AddDefinition("param3");
            writer.AddLocation("some/nested/group/").AddDefinition("param4");
            writer.AddLocation("some/nested/group2/")
                .AddDefinition("param5")
                .AddDefinition("param6");
            writer.AddLocation("some/nested/group2/startswithtest") // there was an issue with this going under /some/nested/group/ also
                .AddDefinition("param7");
            writer.Flush();

            // Assert

            sentDefinitions.Count().Should().Be(1);
            var definitions = sentDefinitions[0];

            definitions.Should().BeEquivalentTo(new ParameterDefinitions
            {
                Parameters = new List<ParameterDefinition>()
                {
                    new ParameterDefinition
                    {
                        Id = "Param1",
                        Name = "Parameter One",
                        Description = "The parameter one",
                        Format = "{0}%",
                        Unit = "%",
                        MinimumValue = -10.43,
                        MaximumValue = 100.123,
                        CustomProperties = "custom prop"
                    }
                },
                ParameterGroups = new List<ParameterGroupDefinition>()
                {
                    new ParameterGroupDefinition
                    {
                        Name = "some",
                        Parameters = new List<ParameterDefinition>(),
                        ChildGroups = new List<ParameterGroupDefinition>()
                        {
                            new ParameterGroupDefinition
                            {
                                Name = "nested",
                                Parameters = new List<ParameterDefinition>(),
                                ChildGroups = new List<ParameterGroupDefinition>()
                                {
                                    new ParameterGroupDefinition
                                    {
                                        Name = "group",
                                        Parameters = new List<ParameterDefinition>
                                        {
                                            new ParameterDefinition
                                            {
                                                Id = "param2"
                                            },
                                            new ParameterDefinition
                                            {
                                                Id = "param3"
                                            },
                                            new ParameterDefinition
                                            {
                                                Id = "param4"
                                            }
                                        },
                                        ChildGroups = new List<ParameterGroupDefinition>()
                                    },
                                    new ParameterGroupDefinition
                                    {
                                        Name = "group2",
                                        Parameters = new List<ParameterDefinition>
                                        {
                                            new ParameterDefinition
                                            {
                                                Id = "param5"
                                            },
                                            new ParameterDefinition
                                            {
                                                Id = "param6"
                                            }
                                        },
                                        ChildGroups = new List<ParameterGroupDefinition>
                                        {
                                            new ParameterGroupDefinition
                                            {
                                                Name = "startswithtest",
                                                ChildGroups = new List<ParameterGroupDefinition>(),
                                                Parameters = new List<ParameterDefinition>
                                                {
                                                    new ParameterDefinition
                                                    {
                                                        Id = "param7"
                                                    }
                                                }
                                            }
                                        }
                                    },
                                }
                            }
                        }
                    }
                }
            });
        }

        [Fact]
        public void AddDefinition_WithIncorrectRanges_ShouldRaiseExceptions()
        {
            // Arrange
            var streamWriter = Substitute.For<IStreamWriterInternal>();
            var writer = new Streaming.Models.StreamWriter.StreamParametersWriter(streamWriter);

            // Act
            Action action1 = () => writer.AddDefinition("Param1").SetRange(10, -10);
            Action action2 = () => writer.AddDefinition("Param1").SetRange(-10, double.PositiveInfinity);
            Action action3 = () => writer.AddDefinition("Param1").SetRange(double.NegativeInfinity, 10);
            Action action4 = () => writer.AddDefinition("Param1").SetRange(-10, double.NaN);
            Action action5 = () => writer.AddDefinition("Param1").SetRange(double.NaN, 10);

            // Assert 
            action1.Should().Throw<ArgumentOutOfRangeException>();
            action2.Should().Throw<ArgumentOutOfRangeException>();
            action3.Should().Throw<ArgumentOutOfRangeException>();
            action4.Should().Throw<ArgumentOutOfRangeException>();
            action5.Should().Throw<ArgumentOutOfRangeException>();
        }


    }
}
