using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Configuration.Placeholder
{
    /// <summary>
    /// Configuration provider that resolves placeholders
    /// A placeholder takes the form of <code> ${some:config:reference?default_if_not_present}></code>
    /// </summary>
    public class PlaceholderResolverProvider : IConfigurationProvider
    {
#if DEBUG
        /// <summary></summary>
        public IList<IConfigurationProvider>? Providers => _providers;

        /// <summary></summary>
        public ILogger<PlaceholderResolverProvider>? Logger => _logger;

        /// <summary></summary>
        public IConfiguration? Configuration => _configuration;
#endif
        private readonly IList<IConfigurationProvider>? _providers;
        private readonly ILogger<PlaceholderResolverProvider>? _logger;
        private IConfiguration? _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderResolverProvider"/> class.
        /// The new placeholder resolver wraps the provided configuration
        /// </summary>
        /// <param name="configuration">the configuration the provider uses when resolving placeholders</param>
        /// <param name="logFactory">the logger factory to use</param>
        public PlaceholderResolverProvider(IConfiguration configuration, ILoggerFactory? logFactory = null)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _logger = logFactory?.CreateLogger<PlaceholderResolverProvider>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderResolverProvider"/> class.
        /// The new placeholder resolver wraps the provided configuration providers.  The <see cref="IConfigurationRoot"/> will be created from these providers.
        /// </summary>
        /// <param name="providers">the configuration providers the resolver uses when resolving placeholders</param>
        /// <param name="logFactory">the logger factory to use</param>
        public PlaceholderResolverProvider(IList<IConfigurationProvider> providers, ILoggerFactory? logFactory = null)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));

            _logger = logFactory?.CreateLogger<PlaceholderResolverProvider>();
        }

        /// <summary>
        /// Tries to get a configuration value for the specified key. If the value is a placeholder
        /// it will try to resolve the placeholder before returning it.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>True</c> if a value for the specified key was found, otherwise <c>false</c>.</returns>
        public bool TryGet(string key, [NotNullWhen(true)] out string? value)
        {
            EnsureInitialized();

            value = _configuration!.ResolvePlaceholders(_configuration![key], _logger);

            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Sets a configuration value for the specified key. No placeholder resolution is performed.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(string key, string value)
        {
            EnsureInitialized();

            _configuration![key] = value;
        }

        /// <summary>
        /// Returns a change token if this provider supports change tracking, null otherwise.
        /// </summary>
        /// <returns>changed token</returns>
        public IChangeToken GetReloadToken()
        {
            EnsureInitialized();

            return _configuration!.GetReloadToken();
        }

        /// <summary>
        /// Creates the <see cref="_configuration"/> from the providers if it has not done so already.
        /// If Configuration already exists, it will call Reload() on the underlying configuration
        /// </summary>
        public void Load()
        {
            if (_configuration == null) _configuration = new ConfigurationRoot(_providers);
            else if (_configuration is IConfigurationRoot root)
                root.Reload();
        }

        /// <summary>
        /// Returns the immediate descendant configuration keys for a given parent path based on this
        /// <see cref="_configuration"/>'s data and the set of keys returned by all the preceding providers.
        /// </summary>
        /// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
        /// <param name="parentPath">The parent path.</param>
        /// <returns>The child keys.</returns>
        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
        {
            EnsureInitialized();

            var section = parentPath == null ? _configuration! : _configuration!.GetSection(parentPath);

            var children = section.GetChildren();

            return children.Select(c => c.Key)
                .Concat(earlierKeys).OrderBy(k => k, ConfigurationKeyComparer.Instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureInitialized()
        {
            if (_configuration == null) _configuration = new ConfigurationRoot(_providers);
        }
    }
}
