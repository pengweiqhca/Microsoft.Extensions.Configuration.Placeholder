using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Extensions.Configuration.Placeholder.Tests
{
    public class PropertyPlaceholderHelperTest
    {
        [Fact]
        public void ResolvePlaceholders_ResolvesNullAndEmpty()
        {
            // Arrange
            var text = "foo=${foo??a},bar=${foo||b}";
            var builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>
            {
                {"foo", ""}
            };
            builder.AddInMemoryCollection(dic1);

            // Act and Assert
            var result = builder.Build().ResolvePlaceholders(text);
            Assert.Equal("foo=,bar=b", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesSinglePlaceholder()
        {
            // Arrange
            var text = "foo=${foo}";
            var builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>
            {
                {"foo", "bar"}
            };
            builder.AddInMemoryCollection(dic1);
            var config = builder.Build();

            // Act and Assert
            var result = config.ResolvePlaceholders(text);
            Assert.Equal("foo=bar", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultiplePlaceholders()
        {
            // Arrange
            var text = "foo=${foo},bar=${bar}";
            var builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>
            {
                {"foo", "bar"},
                {"bar", "baz"}
            };
            builder.AddInMemoryCollection(dic1);

            // Act and Assert
            var result = builder.Build().ResolvePlaceholders(text);
            Assert.Equal("foo=bar,bar=baz", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultipleRecursivePlaceholders()
        {
            // Arrange
            var text = "foo=${bar}";
            var builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>
            {
                {"bar", "${baz}"},
                {"baz", "bar"}
            };
            builder.AddInMemoryCollection(dic1);
            var config = builder.Build();

            // Act and Assert
            var result = config.ResolvePlaceholders(text);
            Assert.Equal("foo=bar", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesMultipleRecursiveInPlaceholders()
        {
            // Arrange
            var text1 = "foo=${b${inner}}";
            var builder1 = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>
            {
                {"bar", "bar"},
                {"inner", "ar"}
            };
            builder1.AddInMemoryCollection(dic1);
            var config1 = builder1.Build();

            var text2 = "${top}";
            var builder2 = new ConfigurationBuilder();
            var dic2 = new Dictionary<string, string>
            {
                {"top", "${child}+${child}"},
                {"child", "${${differentiator}.grandchild}"},
                {"differentiator", "first"},
                {"first.grandchild", "actualValue"}
            };
            builder2.AddInMemoryCollection(dic2);
            var config2 = builder2.Build();

            // Act and Assert
            var result1 = config1.ResolvePlaceholders(text1);
            Assert.Equal("foo=bar", result1);
            var result2 = config2.ResolvePlaceholders(text2);
            Assert.Equal("actualValue+actualValue", result2);
        }

        [Fact]
        public void ResolvePlaceholders_UnresolvedPlaceholderIsIgnored()
        {
            // Arrange
            var text = "foo=${foo},bar=${bar}";
            var builder = new ConfigurationBuilder();
            var dic1 = new Dictionary<string, string>
            {
                {"foo", "bar"}
            };
            builder.AddInMemoryCollection(dic1);
            var config = builder.Build();

            // Act and Assert
            var result = config.ResolvePlaceholders(text);
            Assert.Equal("foo=bar,bar=${bar}", result);
        }

        [Fact]
        public void ResolvePlaceholders_ResolvesArrayRefPlaceholder()
        {
            // Arrange
            var json1 = @"
{
    ""vcap"": {
        ""application"": {
          ""application_id"": ""fa05c1a9-0fc1-4fbd-bae1-139850dec7a3"",
          ""application_name"": ""my-app"",
          ""application_uris"": [
            ""my-app.10.244.0.34.xip.io""
          ],
          ""application_version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca"",
          ""limits"": {
            ""disk"": 1024,
            ""fds"": 16384,
            ""mem"": 256
          },
          ""name"": ""my-app"",
          ""space_id"": ""06450c72-4669-4dc6-8096-45f9777db68a"",
          ""space_name"": ""my-space"",
          ""uris"": [
            ""my-app.10.244.0.34.xip.io"",
            ""my-app2.10.244.0.34.xip.io""
          ],
          ""users"": null,
          ""version"": ""fb8fbcc6-8d58-479e-bcc7-3b4ce5a7f0ca""
        }
    }
}";

            var builder = new ConfigurationBuilder();
            builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json1)));
            var config = builder.Build();

            var text = "foo=${vcap:application:uris[1]}";

            // Act and Assert
            var result = config.ResolvePlaceholders(text);
            Assert.Equal("foo=my-app2.10.244.0.34.xip.io", result);
        }

        [Fact]
        public void GetResolvedConfigurationPlaceholders_ReturnsValues_WhenResolved()
        {
            // arrange
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    {"foo", "${bar}"},
                    {"bar", "baz"}
                });

            // act
            var resolved = builder.Build().GetResolvedConfigurationPlaceholders().ToArray();

            // assert
            Assert.Contains(resolved, f => f.Key == "foo");
            Assert.DoesNotContain(resolved, f => f.Key == "bar");
            Assert.Equal("baz", resolved.First(k => k.Key == "foo").Value);
        }

        [Fact]
        public void GetResolvedConfigurationPlaceholders_ReturnsEmpty_WhenUnResolved()
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(
                new Dictionary<string, string>
                {
                    {"foo", "${bar}"}
                });

            // act
            var resolved = builder.Build().GetResolvedConfigurationPlaceholders().ToArray();

            // assert
            Assert.Contains(resolved, f => f.Key == "foo");
            Assert.Equal(string.Empty, resolved.First(k => k.Key == "foo").Value);
        }

        [Fact]
        public void Circular_Placeholder()
        {
            var builder2 = new ConfigurationBuilder();
            var dic2 = new Dictionary<string, string>
            {
                {"top", "${child}+${child}"},
                {"child", "${differentiator}"},
                {"differentiator", "${top}abc"}
            };
            builder2.AddInMemoryCollection(dic2);
            var config2 = builder2.Build();

            // Act and Assert
            Assert.Throws<ArgumentException>(() => config2.ResolvePlaceholders("a/${top}"));
        }
    }
}
