using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Configuration.Placeholder
{
    /// <summary>
    /// Configuration source used in creating a <see cref="PlaceholderResolverProvider"/> that resolves placeholders
    /// A placeholder takes the form of <code> ${some:config:reference??default_if_not_present}></code>
    /// </summary>
    public class PlaceholderResolverSource : IConfigurationSource
    {
#if DEBUG
        /// <summary></summary>
        // ReSharper disable once InconsistentNaming
        public readonly ILoggerFactory? _loggerFactory;

        /// <summary></summary>
        // ReSharper disable once InconsistentNaming
        public readonly IList<IConfigurationSource> _sources;
#else
        private readonly ILoggerFactory? _loggerFactory;
        private readonly IList<IConfigurationSource> _sources;
#endif
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderResolverSource"/> class.
        /// </summary>
        /// <param name="sources">the configuration sources to use</param>
        /// <param name="logFactory">the logger factory to use</param>
        public PlaceholderResolverSource(IList<IConfigurationSource> sources, ILoggerFactory? logFactory = null)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            _sources = new List<IConfigurationSource>(sources);

            _loggerFactory = logFactory;
        }

        /// <summary>
        /// Builds a <see cref="PlaceholderResolverProvider"/> from the sources.
        /// </summary>
        /// <param name="builder">the provided builder</param>
        /// <returns>the placeholder resolver provider</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new PlaceholderResolverProvider(_sources.Select(source => source.Build(builder)).ToList(), _loggerFactory);
    }
}
