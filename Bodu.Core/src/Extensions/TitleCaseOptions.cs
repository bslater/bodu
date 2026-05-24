// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TitleCaseOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Tunes the behaviour of <see cref="StringExtensions.ToTitleCase(string, TitleCaseOptions)" /> beyond the plain
/// capitalise-each-word default.
/// </summary>
[Flags]
public enum TitleCaseOptions
{
    /// <summary>
    /// Default behaviour: capitalise the first character of every word and lower-case the rest.
    /// </summary>
    None = 0,

    /// <summary>
    /// Lower-case short connective words (<c>a</c>, <c>an</c>, <c>and</c>, <c>as</c>, <c>at</c>, <c>but</c>, <c>by</c>,
    /// <c>for</c>, <c>in</c>, <c>nor</c>, <c>of</c>, <c>on</c>, <c>or</c>, <c>per</c>, <c>so</c>, <c>the</c>, <c>to</c>
    /// , <c>up</c>, <c>via</c>, <c>vs</c>, <c>yet</c>) when they are not the first or last word. Useful for
    /// headline-style titles.
    /// </summary>
    LowerCaseSmallWords = 1,

    /// <summary>
    /// Preserve fully-uppercase words as acronyms rather than down-casing them. For example, <c>"HTML"</c> stays
    /// <c>"HTML"</c> instead of becoming <c>"Html"</c>.
    /// </summary>
    PreserveAcronyms = 2,
}
