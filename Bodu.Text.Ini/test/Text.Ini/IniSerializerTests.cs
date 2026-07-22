// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains the shared model types for the <see cref="IniSerializer" /> test suite; the member backbone lives in the
/// <c>.Serialize</c>, <c>.Deserialize</c>, <c>.SerializeAsync</c>, <c>.DeserializeAsync</c>, and <c>.RoundTrip</c>
/// partials.
/// </summary>
[TestClass]
public partial class IniSerializerTests
{
    /// <summary>
    /// A root model with global scalar members, a section POCO, and a section dictionary.
    /// </summary>
    private sealed class ServerConfig
    {
        /// <summary>Gets or sets the server name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the retry count (a global key).</summary>
        public int Retries { get; set; }

        /// <summary>Gets or sets the database section.</summary>
        public DatabaseSection? Database { get; set; }

        /// <summary>Gets or sets the logging section as a dictionary.</summary>
        public Dictionary<string, string>? Logging { get; set; }
    }

    /// <summary>
    /// A section model of scalar members.
    /// </summary>
    private sealed class DatabaseSection
    {
        /// <summary>Gets or sets the host name.</summary>
        public string? Host { get; set; }

        /// <summary>Gets or sets the port number.</summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// A root model whose only member is required.
    /// </summary>
    private sealed class RequiredConfig
    {
        /// <summary>Gets or sets the required name.</summary>
        [Required]
        public string? Name { get; set; }
    }

    /// <summary>
    /// A root model with a section nested one level too deep.
    /// </summary>
    private sealed class DeepConfig
    {
        /// <summary>Gets or sets the outer section.</summary>
        public OuterSection? Outer { get; set; }
    }

    /// <summary>
    /// A section model that illegally contains a further section.
    /// </summary>
    private sealed class OuterSection
    {
        /// <summary>Gets or sets the nested section, which INI cannot represent.</summary>
        public DatabaseSection? Inner { get; set; }
    }

    /// <summary>
    /// A hand-written <see cref="IIniSectionFactory{TSection}" /> for <see cref="DatabaseSection" />, standing in for
    /// the generated factory in the reflection-free section-overload tests.
    /// </summary>
    private sealed class DatabaseSectionFactory : IIniSectionFactory<DatabaseSection>
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Keys { get; } = new[] { "Host", "Port" };

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<string, string>> GetEntries(DatabaseSection section)
        {
            yield return new KeyValuePair<string, string>("Host", section.Host ?? string.Empty);
            yield return new KeyValuePair<string, string>("Port", section.Port.ToString(CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public DatabaseSection Create(IEnumerable<KeyValuePair<string, string>> entries)
        {
            var section = new DatabaseSection();
            foreach (KeyValuePair<string, string> entry in entries)
            {
                if (string.Equals(entry.Key, "Host", StringComparison.Ordinal))
                    section.Host = entry.Value;
                else if (string.Equals(entry.Key, "Port", StringComparison.Ordinal))
                    section.Port = int.Parse(entry.Value, CultureInfo.InvariantCulture);
            }

            return section;
        }
    }
}
