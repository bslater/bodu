// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Test.Corpus;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

public sealed partial class TomlReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row runs with, and maps them onto the reader, document and writer options.
    /// </summary>
    /// <param name="SpecVersion">The TOML version the reader and the document parse under.</param>
    /// <param name="MaxDepth">The maximum nesting depth, or zero for each surface's default.</param>
    /// <remarks>
    /// A row with no <c>SpecVersion</c> option runs at Bodu's default version, the one a default
    /// <see cref="TomlReaderOptions" /> selects. The writer takes no version: its output is valid under both
    /// (<see cref="TomlSpecVersion" />), so the version only decides how the written text is read back.
    /// </remarks>
    private sealed record RowOptions(TomlSpecVersion SpecVersion, int MaxDepth)
    {
        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">An option is malformed, unknown, or has a value it does not accept.</exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions(default(TomlReaderOptions).SpecVersion, 0);
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "SpecVersion" when value == "V1_0" => options with { SpecVersion = TomlSpecVersion.V1_0 },
                    "SpecVersion" when value == "V1_1" => options with { SpecVersion = TomlSpecVersion.V1_1 },
                    "MaxDepth" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int depth) && depth > 0 =>
                        options with { MaxDepth = depth },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };
            }

            return options;
        }

        /// <summary>
        /// Creates the reader options for the row.
        /// </summary>
        /// <returns>The reader options.</returns>
        public TomlReaderOptions ToReaderOptions() =>
            new()
            {
                SpecVersion = SpecVersion,
                MaxDepth = MaxDepth,
            };

        /// <summary>
        /// Creates the read-only document options for the row.
        /// </summary>
        /// <returns>The document options.</returns>
        public TomlDocumentOptions ToDocumentOptions() =>
            new()
            {
                SpecVersion = SpecVersion,
                MaxDepth = MaxDepth,
            };

        /// <summary>
        /// Creates the writer options for the row.
        /// </summary>
        /// <returns>The writer options.</returns>
        public TomlWriterOptions ToWriterOptions() =>
            new()
            {
                MaxDepth = MaxDepth,
            };
    }
}
