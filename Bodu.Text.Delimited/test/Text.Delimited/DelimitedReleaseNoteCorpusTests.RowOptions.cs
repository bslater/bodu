// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Text.Delimited.Reader;
using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row runs with, and maps them onto the reader and writer options.
    /// </summary>
    /// <remarks>
    /// Each option starts at the reader's and writer's own default: <c>'\0'</c> for the delimiter, quote and comment
    /// characters, which the options document as selecting the default character, header mode, no trimming and no
    /// comments, and the <see cref="DelimitedFieldCountBehavior.Strict" />,
    /// <see cref="DelimitedMalformedRecordBehavior.Throw" /> and <see cref="DelimitedDuplicateHeaderBehavior.Throw" />
    /// policies. A row's <c>\0</c> passes through unchanged, so it too selects the default.
    /// </remarks>
    private sealed record RowOptions
    {
        /// <summary>
        /// Gets the field delimiter the reader and the writer use.
        /// </summary>
        /// <value>The <c>Delimiter</c> option, or <c>'\0'</c> for the default comma.</value>
        public char Delimiter { get; init; }

        /// <summary>
        /// Gets the quote character the reader and the writer use.
        /// </summary>
        /// <value>The <c>Quote</c> option, or <c>'\0'</c> for the default double quote.</value>
        public char Quote { get; init; }

        /// <summary>
        /// Gets the comment character the reader uses.
        /// </summary>
        /// <value>The <c>CommentChar</c> option, or <c>'\0'</c> for the default <c>#</c>.</value>
        public char CommentChar { get; init; }

        /// <summary>
        /// Gets a value indicating whether the reader and the writer work without a header row.
        /// </summary>
        /// <value>The <c>NoHeader</c> option.</value>
        public bool NoHeader { get; init; }

        /// <summary>
        /// Gets a value indicating whether the reader trims unquoted fields.
        /// </summary>
        /// <value>The <c>TrimFields</c> option.</value>
        public bool TrimFields { get; init; }

        /// <summary>
        /// Gets a value indicating whether the reader skips comment lines.
        /// </summary>
        /// <value>The <c>AllowComments</c> option.</value>
        public bool AllowComments { get; init; }

        /// <summary>
        /// Gets the reader's field-count policy.
        /// </summary>
        /// <value>The <c>FieldCountBehavior</c> option.</value>
        public DelimitedFieldCountBehavior FieldCountBehavior { get; init; }

        /// <summary>
        /// Gets the reader's malformed-record policy.
        /// </summary>
        /// <value>The <c>MalformedRecordBehavior</c> option.</value>
        public DelimitedMalformedRecordBehavior MalformedRecordBehavior { get; init; }

        /// <summary>
        /// Gets the reader's duplicate-header policy.
        /// </summary>
        /// <value>The <c>DuplicateHeaderBehavior</c> option.</value>
        public DelimitedDuplicateHeaderBehavior DuplicateHeaderBehavior { get; init; }

        /// <summary>
        /// Gets what a <c>parse</c> row renders.
        /// </summary>
        /// <value>The <c>View</c> option.</value>
        public RowView View { get; init; }

        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">An option is malformed, unknown, or has a value it does not accept.</exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions();
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "Delimiter" => options with { Delimiter = ParseCharacter(name, value) },
                    "Quote" => options with { Quote = ParseCharacter(name, value) },
                    "CommentChar" => options with { CommentChar = ParseCharacter(name, value) },
                    "NoHeader" => options with { NoHeader = ParseBoolean(name, value) },
                    "TrimFields" => options with { TrimFields = ParseBoolean(name, value) },
                    "AllowComments" => options with { AllowComments = ParseBoolean(name, value) },
                    "FieldCountBehavior" => options with { FieldCountBehavior = ParseMember<DelimitedFieldCountBehavior>(name, value) },
                    "MalformedRecordBehavior" => options with { MalformedRecordBehavior = ParseMember<DelimitedMalformedRecordBehavior>(name, value) },
                    "DuplicateHeaderBehavior" => options with { DuplicateHeaderBehavior = ParseMember<DelimitedDuplicateHeaderBehavior>(name, value) },
                    "View" => options with { View = ParseMember<RowView>(name, value) },
                    _ => throw new FormatException($"The option {name} is not one this catalogue maps."),
                };
            }

            return options;
        }

        /// <summary>
        /// Creates the reader options for the row.
        /// </summary>
        /// <returns>The reader options.</returns>
        public DelimitedReaderOptions ToReaderOptions() =>
            new()
            {
                Delimiter = Delimiter,
                Quote = Quote,
                CommentChar = CommentChar,
                NoHeader = NoHeader,
                TrimFields = TrimFields,
                AllowComments = AllowComments,
                FieldCountBehavior = FieldCountBehavior,
                MalformedRecordBehavior = MalformedRecordBehavior,
                DuplicateHeaderBehavior = DuplicateHeaderBehavior,
            };

        /// <summary>
        /// Creates the writer options for the row, which carry the dialect options the writer has.
        /// </summary>
        /// <returns>The writer options.</returns>
        public DelimitedWriterOptions ToWriterOptions() =>
            new()
            {
                Delimiter = Delimiter,
                Quote = Quote,
                NoHeader = NoHeader,
            };

        /// <summary>
        /// Reads a one-character option value.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The decoded option value.</param>
        /// <returns>The character.</returns>
        /// <exception cref="FormatException">The value is not exactly one UTF-16 code unit.</exception>
        private static char ParseCharacter(string name, string value) =>
            value.Length == 1 ? value[0] : throw new FormatException($"The option {name} takes one character, not '{value}'.");

        /// <summary>
        /// Reads a Boolean option value.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="value">The decoded option value.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is neither <c>true</c> nor <c>false</c>.</exception>
        private static bool ParseBoolean(string name, string value) =>
            value switch
            {
                "true" => true,
                "false" => false,
                _ => throw new FormatException($"The option {name} takes true or false, not '{value}'."),
            };

        /// <summary>
        /// Reads an option value that names an enumeration member, matching the name exactly; a number is not accepted.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type.</typeparam>
        /// <param name="name">The option name.</param>
        /// <param name="value">The decoded option value.</param>
        /// <returns>The member.</returns>
        /// <exception cref="FormatException">The value is not the name of a member of <typeparamref name="TEnum" />.</exception>
        private static TEnum ParseMember<TEnum>(string name, string value)
            where TEnum : struct, Enum =>
            Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal)
                ? Enum.Parse<TEnum>(value)
                : throw new FormatException($"The option {name} takes one of {string.Join(", ", Enum.GetNames<TEnum>())}, not '{value}'.");
    }
}
