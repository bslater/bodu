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
    /// A root model with a public global key and a private property, which is not part of its serialized form.
    /// </summary>
    private sealed class PrivateMemberConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets a value the serializer must not write, because the property is private.</summary>
        private string Ignored { get; set; } = "ignored";
    }

    /// <summary>
    /// A root model whose only member is an integer.
    /// </summary>
    private sealed class PortConfig
    {
        /// <summary>Gets or sets the port number (a global key).</summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// A root model whose only member is a <see cref="float" />.
    /// </summary>
    private sealed class RatioConfig
    {
        /// <summary>Gets or sets the ratio (a global key).</summary>
        public float Ratio { get; set; }
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
