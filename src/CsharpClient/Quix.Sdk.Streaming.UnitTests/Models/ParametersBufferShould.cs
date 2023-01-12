﻿using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using Quix.Sdk.Streaming.Models;
using System.Linq;
using System.Threading;
using Quix.TestBase.Extensions;
using Xunit.Abstractions;

namespace Quix.Sdk.Streaming.UnitTests.Models
{
    public class ParametersBufferShould
    {
        public ParametersBufferShould(ITestOutputHelper helper)
        {
            Quix.Sdk.Logging.Factory = helper.CreateLoggerFactory();
        }

        [Fact]
        public void WriteData_WithDisabledConfiguration_ShouldRaiseOnReadEventsStraightForward()
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = null,
                TimeSpanInMilliseconds = null,
                TimeSpanInNanoseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            var buffer = new ParametersBuffer(bufferConfiguration);
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));

            // Assert
            receivedData.Count.Should().Be(5);
            foreach (var rData in receivedData)
            {
                receivedData[0].Should().BeEquivalentTo(new ParameterData(new List<ParameterDataTimestamp>() {data.Timestamps[0]}));   
            }
        }

        [Fact]
        public void WriteData_WithData_FlushOnDispose()
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = 3,
                TimeSpanInMilliseconds = null,
                TimeSpanInNanoseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            var buffer = new ParametersBuffer(bufferConfiguration);
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));

            // Assert
            receivedData.Count.Should().Be(1);
            receivedData[0].Timestamps.Count.Should().Be(3);

            buffer.Dispose();

            receivedData.Count.Should().Be(2);
            receivedData[1].Timestamps.Count.Should().Be(2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteData_WithPacketSizeConfiguration_ShouldRaiseProperOnReadEvents(bool initialConfig)
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = null,
                TimeSpanInMilliseconds = null,
                TimeSpanInNanoseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            if (initialConfig) bufferConfiguration.PacketSize = 2;
            var buffer = new ParametersBuffer(bufferConfiguration);
            if (!initialConfig) buffer.PacketSize = 2;
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));

            // Assert
            receivedData.Count.Should().Be(2);
            receivedData[0].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(0).Take(2).ToList()));
            receivedData[1].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(2).Take(2).ToList()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteData_WithTimeSpanMsConfiguration_ShouldRaiseProperOnReadEvents(bool initialConfig)
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = null,
                TimeSpanInMilliseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            if (initialConfig) bufferConfiguration.TimeSpanInMilliseconds = 200;
            var buffer = new ParametersBuffer(bufferConfiguration);
            if (!initialConfig) buffer.TimeSpanInMilliseconds = 200;
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));

            // Assert
            receivedData.Count.Should().Be(2);
            receivedData[0].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(0).Take(2).ToList()));
            receivedData[1].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(2).Take(2).ToList()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteData_WithTimeSpanNsConfiguration_ShouldRaiseProperOnReadEvents(bool initialConfig)
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = null,
                TimeSpanInMilliseconds = null,
                TimeSpanInNanoseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            if (initialConfig) bufferConfiguration.TimeSpanInNanoseconds = 200 * (long) 1e6;
            var buffer = new ParametersBuffer(bufferConfiguration);
            if (!initialConfig) buffer.TimeSpanInNanoseconds = 200 * (long) 1e6;
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));

            // Assert
            receivedData.Count.Should().Be(2);
            receivedData[0].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(0).Take(2).ToList()));
            receivedData[1].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(2).Take(2).ToList()));
        }

        [Fact]
        public void WriteData_WithTimeSpanAndBufferTimeoutConfiguration_ShouldRaiseProperOnReadEvents()
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() 
            {
                PacketSize = null,
                TimeSpanInMilliseconds = 200,
                BufferTimeout = 100,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            var buffer = new ParametersBuffer(bufferConfiguration);
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));
            Thread.Sleep(1000);

            // Assert
            receivedData.Count.Should().Be(3);
            receivedData[0].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(0).Take(2).ToList()));
            receivedData[1].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(2).Take(2).ToList()));
            receivedData[2].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(4).Take(1).ToList()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteData_WithFilterConfiguration_ShouldRaiseProperOnReadEvents(bool initialConfig)
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = null,
                TimeSpanInMilliseconds = null,
                TimeSpanInNanoseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            if (initialConfig) bufferConfiguration.Filter = (timestamp) => timestamp.Parameters["param2"].NumericValue == 2;
            var buffer = new ParametersBuffer(bufferConfiguration);
            if (!initialConfig) buffer.Filter = (timestamp) => timestamp.Parameters["param2"].NumericValue == 2;
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));
            Thread.Sleep(1000);

            // Assert
            receivedData.Count.Should().Be(3);
            receivedData[0].Should().BeEquivalentTo(new ParameterData(new List<ParameterDataTimestamp>() {data.Timestamps[0]}));
            receivedData[1].Should().BeEquivalentTo(new ParameterData(new List<ParameterDataTimestamp>() {data.Timestamps[2]}));
            receivedData[2].Should().BeEquivalentTo(new ParameterData(new List<ParameterDataTimestamp>() {data.Timestamps[4]}));
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteData_WithBufferTimeout_ShouldRaiseProperOnReadEvents(bool initialConfig)
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = null,
                TimeSpanInMilliseconds = null,
                TimeSpanInNanoseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            if (initialConfig) bufferConfiguration.BufferTimeout = 100;
            var buffer = new ParametersBuffer(bufferConfiguration);
            if (!initialConfig) buffer.BufferTimeout = 100;
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));
            Thread.Sleep(1000);

            // Assert
            receivedData.Count.Should().Be(1);
            receivedData[0].Should().BeEquivalentTo(data);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteData_WithCustomTriggerConfiguration_ShouldRaiseProperOnReadEvents(bool initialConfig)
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = null,
                TimeSpanInMilliseconds = null,
                TimeSpanInNanoseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            if (initialConfig) bufferConfiguration.CustomTrigger = (data) => data.Timestamps.Count == 2;
            var buffer = new ParametersBuffer(bufferConfiguration);
            if (!initialConfig) buffer.CustomTrigger = (data) => data.Timestamps.Count == 2;
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));
            Thread.Sleep(1000);

            // Assert
            receivedData.Count.Should().Be(2);
            receivedData[0].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(0).Take(2).ToList()));
            receivedData[1].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(2).Take(2).ToList()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteData_WithCustomTriggerBeforeEnqueueConfiguration_ShouldRaiseProperOnReadEvents(bool initialConfig)
        {
            // Arrange
            var bufferConfiguration = new ParametersBufferConfiguration() // Set the buffer explicitly to null
            {
                PacketSize = null,
                TimeSpanInMilliseconds = null,
                TimeSpanInNanoseconds = null,
                BufferTimeout = null,
                Filter = null,
                CustomTrigger = null,
                CustomTriggerBeforeEnqueue = null
            };
            if (initialConfig) bufferConfiguration.CustomTriggerBeforeEnqueue = timestamp => timestamp.Tags["tag2"] == "value2";
            var buffer = new ParametersBuffer(bufferConfiguration);
            if (!initialConfig) buffer.CustomTriggerBeforeEnqueue = timestamp => timestamp.Tags["tag2"] == "value2";
            var receivedData = new List<Streaming.Models.ParameterData>();

            buffer.OnRead += (data) =>
            {
                receivedData.Add(data);
            };

            //Act
            var data = this.GenerateParameterData();
            buffer.WriteChunk(data.ConvertToProcessData(false, false));
            Thread.Sleep(1000);

            // Assert
            receivedData.Count.Should().Be(2);
            receivedData[0].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(0).Take(2).ToList()));
            receivedData[1].Should().BeEquivalentTo(new ParameterData(data.Timestamps.Skip(2).Take(2).ToList()));
        }

        private ParameterData GenerateParameterData()
        {
            var data = new ParameterData();
            data.AddTimestampMilliseconds(100)
                .AddValue("param1", 1)
                .AddValue("param2", 2)
                .AddValue("param3", "3")
                .AddValue("param4", "4")
                .AddTag("tag1", "value1");

            data.AddTimestampMilliseconds(200)
                .AddValue("param3", "3")
                .AddValue("param4", "4")
                .AddTag("tag1", "value1");

            data.AddTimestampMilliseconds(300)
                .AddValue("param1", 1)
                .AddValue("param2", 2)
                .AddTag("tag2", "value2");

            data.AddTimestampMilliseconds(400)
                .AddValue("param1", 1);

            data.AddTimestampMilliseconds(500)
                .AddValue("param2", 2)
                .AddTag("tag2", "value2");

            return data;
        }

    }
}
