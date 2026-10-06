// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonthSet.TextFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct MonthSet
{
    /// <summary>
    /// Describes one text form of a month set.
    /// </summary>
    /// <param name="Form">
    /// The numeric form, which applies when <paramref name="LetterMask" /> is <see langword="false" />.
    /// </param>
    /// <param name="LetterMask">Whether the form is the twelve-character letter mask, January first.</param>
    /// <param name="Placeholder">
    /// For the letter mask, the character for a month not selected, or <see langword="null" /> when a reader takes it
    /// from the text and a writer uses <c>_</c>.
    /// </param>
    private readonly record struct TextFormat(CalendarValueSet.NumericForm Form, bool LetterMask, char? Placeholder);
}
