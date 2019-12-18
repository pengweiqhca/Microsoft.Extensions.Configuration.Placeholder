using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.Configuration.Placeholder
{
    /// <summary>
    /// Utility class for working with configuration values that have placeholders in them.
    /// A placeholder takes the form of <code> ${some:config:reference?default_if_not_present}></code>
    /// Note: This was "inspired" by the Spring class: PropertyPlaceholderHelper
    /// </summary>
    public static class PropertyPlaceholderHelper
    {
        private const string Prefix = "${";
        private const string Suffix = "}";
        private const string Separator = "??";

        /// <summary>
        /// Replaces all placeholders of the form <code> ${some:config:reference?default_if_not_present}</code>
        /// with the corresponding value from the supplied <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="config">the configuration used for finding replace values.</param>
        /// <param name="property">the string containing one or more placeholders</param>
        /// <param name="logger">optional logger</param>
        /// <returns>the supplied value with the placeholders replaced inline</returns>
        public static string ResolvePlaceholders(this IConfiguration config, string property, ILogger? logger = null) =>
            ParseStringValue(property, config, new HashSet<string>(), logger);
#if DEBUG
        public static IEnumerable<KeyValuePair<string, string>> GetResolvedConfigurationPlaceholders(this IConfiguration config, ILogger? logger = null, bool useEmptyStringIfNotFound = true)
        {
            // setup a holding tank for resolved values
            var resolvedValues = new Dictionary<string, string>();
            var visitedPlaceholders = new HashSet<string>();

            // iterate all config entries where the value isn't null and contains both the prefix and suffix that identify placeholders
            foreach (var entry in config.AsEnumerable().Where(e => e.Value != null && e.Value.Contains(Prefix) && e.Value.Contains(Suffix)))
            {
                logger?.LogTrace("Found a property placeholder '{0}' to resolve for key '{1}", entry.Value, entry.Key);
                resolvedValues.Add(entry.Key, ParseStringValue(entry.Value, config, visitedPlaceholders, logger, useEmptyStringIfNotFound));
            }

            return resolvedValues;
        }
#endif
        private static string ParseStringValue(string property, IConfiguration config, ISet<string> visitedPlaceHolders, ILogger? logger = null, bool useEmptyStringIfNotFound = false)
        {
            if (config == null || string.IsNullOrEmpty(property)) return property;

            var result = new StringBuilder(property);

            var startIndex = property.IndexOf(Prefix, StringComparison.Ordinal);
            while (startIndex != -1)
            {
                var endIndex = FindEndIndex(result, startIndex);
                if (endIndex != -1)
                {
                    var placeholder = result.Substring(startIndex + Prefix.Length, endIndex);
                    var originalPlaceholder = placeholder;

                    if (!visitedPlaceHolders.Add(originalPlaceholder))
                    {
                        throw new ArgumentException($"Circular placeholder reference '{originalPlaceholder}' in property definitions");
                    }

                    // Recursive invocation, parsing placeholders contained in the placeholder key.
                    placeholder = ParseStringValue(placeholder, config, visitedPlaceHolders);

                    // Handle array references foo:bar[1]:baz format -> foo:bar:1:baz
                    var lookup = placeholder.Replace('[', ':').Replace("]", string.Empty);

                    // Now obtain the value for the fully resolved key...
                    var propVal = config[lookup];
                    if (propVal == null)
                    {
                        var separatorIndex = placeholder.IndexOf(Separator, StringComparison.Ordinal);
                        if (separatorIndex != -1)
                        {
                            var actualPlaceholder = placeholder.Substring(0, separatorIndex);
                            var defaultValue = placeholder.Substring(separatorIndex + Separator.Length);
                            propVal = config[actualPlaceholder] ?? defaultValue;
                        }
                        else if (useEmptyStringIfNotFound)
                        {
                            propVal = string.Empty;
                        }
                    }

                    if (propVal != null)
                    {
                        // Recursive invocation, parsing placeholders contained in these
                        // previously resolved placeholder value.
                        propVal = ParseStringValue(propVal, config, visitedPlaceHolders);
                        result.Replace(startIndex, endIndex + Suffix.Length, propVal);
                        logger?.LogDebug("Resolved placeholder '{0}'", placeholder);
                        startIndex = result.IndexOf(Prefix, startIndex + propVal.Length);
                    }
                    else
                    {
                        // Proceed with unprocessed value.
                        startIndex = result.IndexOf(Prefix, endIndex + Prefix.Length);
                    }

                    visitedPlaceHolders.Remove(originalPlaceholder);
                }
                else
                {
                    startIndex = -1;
                }
            }

            return result.ToString();
        }

        private static int FindEndIndex(StringBuilder property, int startIndex)
        {
            var index = startIndex + Prefix.Length;
            var withinNestedPlaceholder = 0;
            while (index < property.Length)
            {
                if (SubstringMatch(property, index, Suffix))
                {
                    if (withinNestedPlaceholder > 0)
                    {
                        withinNestedPlaceholder--;
                        index += Suffix.Length;
                    }
                    else
                    {
                        return index;
                    }
                }
                else if (SubstringMatch(property, index, Prefix))
                {
                    withinNestedPlaceholder++;
                    index += Prefix.Length;
                }
                else
                {
                    index++;
                }
            }

            return -1;
        }

        private static bool SubstringMatch(StringBuilder str, int index, string substring)
        {
            for (var j = 0; j < substring.Length; j++)
            {
                var i = index + j;
                if (i >= str.Length || str[i] != substring[j])
                {
                    return false;
                }
            }

            return true;
        }

        private static void Replace(this StringBuilder builder, int start, int end, string str)
        {
            builder.Remove(start, end - start);
            builder.Insert(start, str);
        }

        private static int IndexOf(this StringBuilder builder, string str, int start)
        {
            if (start + str.Length > builder.Length) return -1;

            for (var i = start; i < builder.Length; i++)
            {
                var j = 0;
                for (; j < str.Length; j++)
                {
                    if (builder[i + j] != str[j]) break;
                }

                if (j == str.Length) return i;
            }

            return -1;
        }

        private static string Substring(this StringBuilder builder, int start, int end)
        {
            var array = new char[end - start];

            builder.CopyTo(start, array, 0, array.Length);

            return new string(array);
        }
    }
}
