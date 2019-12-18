using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Configuration.Placeholder.Tests
{
    public class PlaceholderResolverSourceTest
    {
        [Fact]
        public void Constructor_ThrowsIfNulls()
        {
            // Arrange
            IList<IConfigurationSource> sources = null;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => new PlaceholderResolverSource(sources));
        }

        [Fact]
        public void Constructors__InitializesProperties()
        {
            var memSource = new MemoryConfigurationSource();
            IList<IConfigurationSource> sources = new List<IConfigurationSource> { memSource };
            ILoggerFactory factory = new LoggerFactory();

            var source = new PlaceholderResolverSource(sources, factory);
            Assert.Equal(factory, source._loggerFactory);
            Assert.NotNull(source._sources);
            Assert.Single(source._sources);
            Assert.NotSame(sources, source._sources);
            Assert.Contains(memSource, source._sources);
        }

        [Fact]
        public void Build__ReturnsProvider()
        {
            // Arrange
            var memSource = new MemoryConfigurationSource();
            IList<IConfigurationSource> sources = new List<IConfigurationSource> { memSource };
            ILoggerFactory factory = new LoggerFactory();

            // Act and Assert
            var source = new PlaceholderResolverSource(sources, factory);
            var provider = source.Build(new ConfigurationBuilder());
            Assert.IsType<PlaceholderResolverProvider>(provider);
        }
    }
}
