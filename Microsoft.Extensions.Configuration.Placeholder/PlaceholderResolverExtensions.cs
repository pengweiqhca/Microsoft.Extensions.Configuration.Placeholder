using Microsoft.Extensions.Configuration.Placeholder;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Configuration
{
    public static class PlaceholderResolverExtensions
    {
        /// <summary>
        /// Add a placeholder resolver configuration source to the <see cref="ConfigurationBuilder"/>. The placeholder resolver source will capture and wrap all
        /// the existing sources <see cref="IConfigurationSource"/> contained in the builder.  The newly created source will then replace the existing sources
        /// and provide placeholder resolution for the configuration. Typically you will want to add this configuration source as the last one so that you wrap all
        /// of the applications configuration sources with place holder resolution.
        /// </summary>
        /// <param name="builder">the configuration builder</param>
        /// <param name="loggerFactory">the logger factory to use</param>
        /// <returns>builder</returns>
        public static IConfigurationBuilder AddPlaceholderResolver(this IConfigurationBuilder builder, ILoggerFactory? loggerFactory = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var resolver = new PlaceholderResolverSource(builder.Sources, loggerFactory);

            builder.Sources.Clear();

            return builder.Add(resolver);
        }

        /// <summary>
        /// Creates a new <see cref="ConfigurationRoot"/> from a <see cref="PlaceholderResolverProvider"/>.  The place holder resolver will be created using the existing
        /// configuration providers contained in the incoming configuration.  This results in providing placeholder resolution for those configuration sources.
        /// </summary>
        /// <param name="configuration">incoming configuration to wrap</param>
        /// <param name="loggerFactory">the logger factory to use</param>
        /// <returns>a new configuration</returns>
        public static IConfigurationRoot AddPlaceholderResolver(this IConfiguration configuration, ILoggerFactory? loggerFactory = null)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            return new ConfigurationRoot(new IConfigurationProvider[] { new PlaceholderResolverProvider(configuration, loggerFactory) });
        }
    }
}
