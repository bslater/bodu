// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Test.Corpus;
using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml;

public sealed partial class YamlReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row reads with, and maps them onto <see cref="YamlDocumentOptions" />.
    /// </summary>
    /// <param name="SpecVersion">The YAML version whose scalar resolution applies.</param>
    /// <param name="DuplicateKeyBehavior">The policy for a key a mapping repeats.</param>
    /// <param name="MaxDepth">The maximum nesting depth; zero or less selects the default.</param>
    private sealed record RowOptions(
        YamlSpecVersion SpecVersion,
        YamlDuplicateKeyBehavior DuplicateKeyBehavior,
        int MaxDepth)
    {
        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">An option is malformed, unknown, or has a value it does not accept.</exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions(YamlSpecVersion.V1_2, YamlDuplicateKeyBehavior.Throw, 0);
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "SpecVersion" => options with { SpecVersion = ParseMember<YamlSpecVersion>(name, value) },
                    "DuplicateKeyBehavior" => options with { DuplicateKeyBehavior = ParseMember<YamlDuplicateKeyBehavior>(name, value) },
                    "MaxDepth" when int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int depth) =>
                        options with { MaxDepth = depth },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };
            }

            return options;
        }

        /// <summary>
        /// Creates the document options for the row.
        /// </summary>
        /// <returns>The document options; the merge-key behavior keeps its default.</returns>
        public YamlDocumentOptions ToDocumentOptions() =>
            new()
            {
                SpecVersion = SpecVersion,
                DuplicateKeyBehavior = DuplicateKeyBehavior,
                MaxDepth = MaxDepth,
            };

        /// <summary>
        /// Reads an option value that names an enumeration member.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type.</typeparam>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <returns>The member.</returns>
        /// <exception cref="FormatException">The value is not the exact name of a member.</exception>
        private static TEnum ParseMember<TEnum>(string name, string value)
            where TEnum : struct, Enum =>
            Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal)
                ? Enum.Parse<TEnum>(value)
                : throw new FormatException($"The option {name} takes one of {string.Join(", ", Enum.GetNames<TEnum>())}, not '{value}'.");
    }
}
