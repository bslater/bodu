// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReleaseNoteCorpusTests.RowOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;

namespace Bodu.IO.Compound;

public sealed partial class CompoundReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the options a catalogue row reads with, and maps them onto <see cref="CompoundFileOptions" />.
    /// </summary>
    /// <param name="ValidationLevel">The validation level every read of the row uses.</param>
    /// <param name="ReadStrategy">The read strategy every read of the row uses.</param>
    private sealed record RowOptions(CompoundValidationLevel ValidationLevel, CompoundReadStrategy ReadStrategy)
    {
        /// <summary>
        /// Reads a row's <c>options</c> field.
        /// </summary>
        /// <param name="text">The field; empty for the defaults of a new <see cref="CompoundFileOptions" />.</param>
        /// <returns>The row options.</returns>
        /// <exception cref="FormatException">
        /// An option is malformed, unknown, or has a value it does not accept.
        /// </exception>
        public static RowOptions Parse(string text)
        {
            var defaults = new CompoundFileOptions();
            var options = new RowOptions(defaults.ValidationLevel, defaults.ReadStrategy);
            foreach ((string name, string value) in ReleaseNoteOptions.Parse(text))
            {
                options = name switch
                {
                    "ValidationLevel" => options with { ValidationLevel = ParseMember<CompoundValidationLevel>(name, value) },
                    "ReadStrategy" => options with { ReadStrategy = ParseMember<CompoundReadStrategy>(name, value) },
                    _ => throw new FormatException($"The option {name}={value} is not one this catalogue maps."),
                };
            }

            return options;
        }

        /// <summary>
        /// Creates the read options for the row.
        /// </summary>
        /// <returns>
        /// A new <see cref="CompoundFileOptions" /> with the row's validation level and read strategy.
        /// </returns>
        public CompoundFileOptions ToFileOptions() =>
            new()
            {
                ValidationLevel = ValidationLevel,
                ReadStrategy = ReadStrategy,
            };

        /// <summary>
        /// Reads an option value that names an enumeration member.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration.</typeparam>
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
