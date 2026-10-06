// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSet.TextFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct DayOfWeekSet
{
    /// <summary>
    /// Describes one form of the seven-character mask.
    /// </summary>
    /// <param name="MondayFirst">Whether the mask starts on Monday rather than Sunday.</param>
    /// <param name="Placeholder">
    /// The character for a day not selected, or <see langword="null" /> when a reader takes it from the text and a
    /// writer uses <c>_</c>.
    /// </param>
    /// <param name="Binary">Whether the mask is binary, <c>1</c> for a selected day and <c>0</c> otherwise.</param>
    private readonly record struct TextFormat(bool MondayFirst, char? Placeholder, bool Binary)
    {
        /// <summary>
        /// Gets the default form: Sunday first, with <c>_</c> for a day not selected.
        /// </summary>
        public static TextFormat Default { get; } = new(MondayFirst: false, Placeholder: '_', Binary: false);

        /// <summary>
        /// Gets the binary form, Sunday first.
        /// </summary>
        public static TextFormat BinaryForm { get; } = new(MondayFirst: false, Placeholder: null, Binary: true);
    }
}
