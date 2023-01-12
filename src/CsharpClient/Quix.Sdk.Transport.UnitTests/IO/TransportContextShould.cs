﻿using FluentAssertions;
using Quix.Sdk.Transport.IO;
using System.Collections.Generic;
using Xunit;

namespace Quix.Sdk.Transport.UnitTests.IO
{
    public class TransportContextShould
    {
        [Fact]
        public void Constructor_WithNullDictionary_ShouldNotThrowException()
        {
            // Arrange
            Dictionary<string, object> dictionary = null;

            // Act
            new TransportContext(dictionary);

            // Assert by not getting an exception
        }

        [Fact]
        public void Constructor_WithDictionary_ShouldSetExpected()
        {
            // Arrange
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary.Add("Key", "value");

            // Act
            var transportContext = new TransportContext(dictionary);

            // Assert
            transportContext["Key"].Should().Be("value");
        }
    }
}
