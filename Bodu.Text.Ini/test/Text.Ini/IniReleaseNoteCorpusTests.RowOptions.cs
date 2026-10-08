// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Text.Ini.Reader;
using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini;

public sealed partial class IniReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row runs with, and maps them onto the reader, document and writer options.
    /// </summary>
    /// <param name="DocumentView">Whether the row renders the <c>Document</c> view rather than the <c>Entries</c> view.</param>
    /// <param name="DisallowHashComments">Whether the reader refuses <c>#</c> as a comment prefix.</param>
    /// <param name="SkipComments">Whether the reader skips comment lines rather than reporting them.</param>
    /// <param name="DuplicateSectionBehavior">The duplicate-section policy the <c>Document</c> view applies.</param>
    /// <param name="DuplicateKeyBehavior">The duplicate-key policy the <c>Document</c> view applies.</param>
    /// <param name="CommentPrefix">The writer's comment prefix, or <c>'\0'</c> for the writer's default.</param>
    private sealed record RowOptions(
        bool DocumentView,
        bool DisallowHashComments,
        bool SkipComments,
        IniDuplicateSectionBehavior DuplicateSectionBehavior,
        IniDuplicateKeyBehavior DuplicateKeyBehavior,
        char CommentPrefix)
    {
        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">
        /// An option is malformed, unknown, or has a value it does not accept, or a duplicate policy is given without
        /// <c>View=Document</c>, the only view that applies it.
        /// </exception>
        public static RowOptions Parse(string text)
        {
            var options = new RowOptions(false, false, false, IniDuplicateSectionBehavior.Merge, IniDuplicateKeyBehavior.LastWins, '\0');
            bool hasDuplicatePolicy = false;

            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "View" when value is "Entries" or "Document" => options with { DocumentView = value == "Document" },
                    "DisallowHashComments" => options with { DisallowHashComments = ParseBoolean(name, value) },
                    "SkipComments" => options with { SkipComments = ParseBoolean(name, value) },
                    "DuplicateSectionBehavior" => options with { DuplicateSectionBehavior = ParseMember<IniDuplicateSectionBehavior>(name, value) },
                    "DuplicateKeyBehavior" => options with { DuplicateKeyBehavior = ParseMember<IniDuplicateKeyBehavior>(name, value) },
                    "CommentPrefix" when value.Length == 1 => options with { CommentPrefix = value[0] },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };

                hasDuplicatePolicy |= name is "DuplicateSectionBehavior" or "DuplicateKeyBehavior";
            }

            if (hasDuplicatePolicy && !options.DocumentView)
                throw new FormatException("A duplicate policy applies only to the Document view, so the row needs View=Document.");

            return options;
        }

        /// <summary>
        /// Creates the reader options for the row, which both views read with.
        /// </summary>
        /// <returns>The reader options.</returns>
        public IniReaderOptions ToReaderOptions() =>
            new()
            {
                DisallowHashComments = DisallowHashComments,
                SkipComments = SkipComments,
            };

        /// <summary>
        /// Creates the document options for the row, which the <c>Document</c> view applies.
        /// </summary>
        /// <returns>The document options.</returns>
        public IniDocumentOptions ToDocumentOptions() =>
            new()
            {
                DuplicateSectionBehavior = DuplicateSectionBehavior,
                DuplicateKeyBehavior = DuplicateKeyBehavior,
            };

        /// <summary>
        /// Creates the writer options for the row.
        /// </summary>
        /// <returns>The writer options.</returns>
        public IniWriterOptions ToWriterOptions() =>
            new()
            {
                CommentPrefix = CommentPrefix,
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

        /// <summary>
        /// Reads an option value that names a member of an enumeration, exactly as it is declared.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration.</typeparam>
        /// <param name="name">The option name.</param>
        /// <param name="value">The option value.</param>
        /// <returns>The member.</returns>
        /// <exception cref="FormatException">The value is not the name of a member of <typeparamref name="TEnum" />.</exception>
        private static TEnum ParseMember<TEnum>(string name, string value)
            where TEnum : struct, Enum =>
            Enum.GetNames<TEnum>().Contains(value, StringComparer.Ordinal)
                ? Enum.Parse<TEnum>(value)
                : throw new FormatException($"The option {name} takes a member name of {typeof(TEnum).Name}, not '{value}'.");
    }
}
