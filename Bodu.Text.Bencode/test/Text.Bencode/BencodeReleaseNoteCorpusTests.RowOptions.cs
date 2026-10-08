// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Test.Corpus;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

public sealed partial class BencodeReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row runs with, and maps them onto the reader, document and writer options.
    /// </summary>
    /// <param name="Surface">What reads the input: <c>Reader</c>, <c>Document</c> or <c>Node</c>.</param>
    /// <param name="MaxDepth">The maximum depth, or zero for each surface's default.</param>
    /// <param name="AllowUnsortedKeys">Whether the read accepts dictionary keys out of order.</param>
    /// <param name="AllowDuplicateKeys">Whether the read accepts a repeated dictionary key.</param>
    /// <param name="AllowMultipleRootValues">Whether the writer accepts more than one root value.</param>
    private sealed record RowOptions(
        string Surface,
        int MaxDepth,
        bool AllowUnsortedKeys,
        bool AllowDuplicateKeys,
        bool AllowMultipleRootValues)
    {
        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">An option is malformed, unknown, or has a value it does not accept.</exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions("Reader", 0, false, false, false);
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "Surface" when value is "Reader" or "Document" or "Node" => options with { Surface = value },
                    "MaxDepth" when int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int depth) && depth > 0 =>
                        options with { MaxDepth = depth },
                    "AllowUnsortedKeys" => options with { AllowUnsortedKeys = ParseBoolean(name, value) },
                    "AllowDuplicateKeys" => options with { AllowDuplicateKeys = ParseBoolean(name, value) },
                    "AllowMultipleRootValues" => options with { AllowMultipleRootValues = ParseBoolean(name, value) },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };
            }

            return options;
        }

        /// <summary>
        /// Creates the reader options for the row.
        /// </summary>
        /// <returns>The reader options.</returns>
        public BencodeReaderOptions ToReaderOptions() =>
            new()
            {
                MaxDepth = MaxDepth,
                AllowUnsortedKeys = AllowUnsortedKeys,
                AllowDuplicateKeys = AllowDuplicateKeys,
            };

        /// <summary>
        /// Creates the document options for the row, which the node surface reads with too.
        /// </summary>
        /// <returns>The document options.</returns>
        public BencodeDocumentOptions ToDocumentOptions() =>
            new()
            {
                MaxDepth = MaxDepth,
                AllowUnsortedKeys = AllowUnsortedKeys,
                AllowDuplicateKeys = AllowDuplicateKeys,
            };

        /// <summary>
        /// Creates the writer options for the row.
        /// </summary>
        /// <returns>The writer options.</returns>
        public BencodeWriterOptions ToWriterOptions() =>
            new()
            {
                MaxDepth = MaxDepth,
                AllowMultipleRootValues = AllowMultipleRootValues,
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
