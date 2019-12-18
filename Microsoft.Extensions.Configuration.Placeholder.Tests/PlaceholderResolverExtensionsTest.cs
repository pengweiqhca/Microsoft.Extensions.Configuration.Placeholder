using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Extensions.Configuration.Placeholder.Tests
{
    public class PlaceholderResolverExtensionsTest
    {
        [Fact]
        public void AddPlaceholderResolver_ThrowsIfConfigBuilderNull()
        {
            // Arrange
            IConfigurationBuilder configurationBuilder = null!;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => configurationBuilder.AddPlaceholderResolver());
        }

        [Fact]
        public void AddPlaceholderResolver_ThrowsIfConfigNull()
        {
            // Arrange
            IConfigurationRoot configuration = null!;

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => configuration.AddPlaceholderResolver());
        }

        [Fact]
        public void AddPlaceholderResolver_AddsPlaceholderResolverSourceToList()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();

            Assert.Single(configurationBuilder.Sources.OfType<PlaceholderResolverSource>());
        }

        [Fact]
        public void AddPlaceholderResolver_WithLoggerFactorySucceeds()
        {
            // Arrange
            var configurationBuilder = new ConfigurationBuilder();
            var loggerFactory = new LoggerFactory();

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver(loggerFactory);
            var configuration = configurationBuilder.Build();

            var provider =
                configuration.Providers.OfType<PlaceholderResolverProvider>().SingleOrDefault();

            Assert.NotNull(provider);
            Assert.NotNull(provider._logger);
        }

        [Fact]
        public void AddPlaceholderResolver_JsonAppSettingsResolvesPlaceholders()
        {
            // Arrange
            var appsettings = @"
                {
                    ""spring"": {
                        ""bar"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:bar:name??noname}"",
                        }
                      }
                    }
                }";

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(appsettings)));

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_XmlAppSettingsResolvesPlaceholders()
        {
            // Arrange
            var appsettings = @"
<settings>
    <spring>
        <bar>
            <name>myName</name>
        </bar>
      <cloud>
        <config>
            <name>${spring:bar:name??noname}</name>
        </config>
      </cloud>
    </spring>
</settings>";

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddXmlStream(new MemoryStream(Encoding.UTF8.GetBytes(appsettings)));

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_IniAppSettingsResolvesPlaceholders()
        {
            // Arrange
            var appsettings = @"
[spring:bar]
    name=myName
[spring:cloud:config]
    name=${spring:bar:name??noName}
";

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddIniStream(new MemoryStream(Encoding.UTF8.GetBytes(appsettings)));

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_CommandLineAppSettingsResolvesPlaceholders()
        {
            // Arrange
            var appsettings = new[]
            {
                "spring:bar:name=myName",
                "--spring:cloud:config:name=${spring:bar:name??noName}"
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddCommandLine(appsettings);

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_HandlesRecursivePlaceHolders()
        {
            var appsettingsJson = @"
                {
                    ""spring"": {
                        ""json"": {
                            ""name"": ""myName""
                    },
                      ""cloud"": {
                        ""config"": {
                            ""name"" : ""${spring:xml:name??noname}"",
                        }
                      }
                    }
                }";

            var appsettingsXml = @"
<settings>
    <spring>
        <xml>
            <name>${spring:ini:name??noName}</name>
        </xml>
    </spring>
</settings>";

            var appsettingsIni = @"
[spring:ini]
    name=${spring:line:name??noName}
";
            var appsettingsLine = new[]
    {
                            "--spring:line:name=${spring:json:name??noName}"
    };

            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(appsettingsJson)));
            configurationBuilder.AddXmlStream(new MemoryStream(Encoding.UTF8.GetBytes(appsettingsXml)));
            configurationBuilder.AddIniStream(new MemoryStream(Encoding.UTF8.GetBytes(appsettingsIni)));
            configurationBuilder.AddCommandLine(appsettingsLine);

            // Act and Assert
            configurationBuilder.AddPlaceholderResolver();
            var config = configurationBuilder.Build();

            Assert.Equal("myName", config["spring:cloud:config:name"]);
        }

        [Fact]
        public void AddPlaceholderResolver_ClearsSources()
        {
            var settings = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "${key1?notfound}" },
                { "key3", "${nokey?notfound}" },
                { "key4", "${nokey}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            builder.AddPlaceholderResolver();

            Assert.Single(builder.Sources);
            var config = builder.Build();

            Assert.Single(config.Providers);
            var provider = config.Providers.ToList()[0];
            Assert.IsType<PlaceholderResolverProvider>(provider);
        }

        [Fact]
        public void AddPlaceholderResolver_WithConfiguration_ReturnsNewConfiguration()
        {
            var settings = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "${key1??notfound}" },
                { "key3", "${nokey??notfound}" },
                { "key4", "${nokey}" },
            };

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(settings);
            var config1 = builder.Build();

            var config2 = config1.AddPlaceholderResolver();
            Assert.NotSame(config1, config2);

            Assert.Single(config2.Providers);
            var provider = config2.Providers.ToList()[0];
            Assert.IsType<PlaceholderResolverProvider>(provider);

            Assert.Null(config2["nokey"]);
            Assert.Equal("value1", config2["key1"]);
            Assert.Equal("value1", config2["key2"]);
            Assert.Equal("notfound", config2["key3"]);
            Assert.Equal("${nokey}", config2["key4"]);
        }
    }
}
