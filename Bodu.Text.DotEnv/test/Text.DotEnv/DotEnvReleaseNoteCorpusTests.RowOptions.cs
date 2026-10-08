// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Text.DotEnv.Reader;
using Bodu.Text.DotEnv.Writer;

namespace Bodu.Text.DotEnv;

public sealed partial class DotEnvReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row runs with, and maps them onto the reader and writer options.
    /// </summary>
    /// <param name="DisallowExportPrefix">Whether the read leaves a leading <c>export</c> keyword unrecognized.</param>
    /// <param name="DisallowInlineComments">
    /// Whether a <c>#</c> after whitespace stays part of an unquoted value instead of starting an inline comment.
    /// </param>
    /// <param name="SkipComments">Whether the read skips comment lines instead of reporting them.</param>
    /// <param name="WriteExportPrefix">
    /// Whether <see cref="Utf8DotEnvWriter.WritePropertyName(string)" /> prefixes the key with <c>export</c>.
    /// </param>
    private sealed record RowOptions(
        bool DisallowExportPrefix,
        bool DisallowInlineComments,
        bool SkipComments,
        bool WriteExportPrefix)
    {
        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">An option is malformed, unknown, or has a value it does not accept.</exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions(false, false, false, false);
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "DisallowExportPrefix" => options with { DisallowExportPrefix = ParseBoolean(name, value) },
                    "DisallowInlineComments" => options with { DisallowInlineComments = ParseBoolean(name, value) },
                    "SkipComments" => options with { SkipComments = ParseBoolean(name, value) },
                    "WriteExportPrefix" => options with { WriteExportPrefix = ParseBoolean(name, value) },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };
            }

            return options;
        }

        /// <summary>
        /// Creates the reader options for the row.
        /// </summary>
        /// <returns>The reader options.</returns>
        public DotEnvReaderOptions ToReaderOptions() =>
            new()
            {
                DisallowExportPrefix = DisallowExportPrefix,
                DisallowInlineComments = DisallowInlineComments,
                SkipComments = SkipComments,
            };

        /// <summary>
        /// Creates the writer options for the row.
        /// </summary>
        /// <returns>The writer options.</returns>
        public DotEnvWriterOptions ToWriterOptions() =>
            new()
            {
                WriteExportPrefix = WriteExportPrefix,
            };

        /// <summary>
        /// Reads a Boolean option value.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is neither <c>true</c> nor <c>false</c>.</exception>
        private static bool ParseBoolean(string name, string value) =>
            value switch
            {
                "true" => true,
                "false" => false,
                _ => throw new FormatException($"The option {name} takes true or false, not '{value}'."),
            };
    }
}
