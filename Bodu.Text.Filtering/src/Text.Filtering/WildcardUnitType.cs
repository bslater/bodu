// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardUnitType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Identifies what a single <see cref="WildcardUnit" /> of a compiled wildcard pattern matches.
/// </summary>
internal enum WildcardUnitType : byte
{
    /// <summary>
    /// The unit matches exactly one specific character.
    /// </summary>
    Char = 0,

    /// <summary>
    /// The unit matches any single character (the <c>?</c> metacharacter).
    /// </summary>
    AnyOne = 1,

    /// <summary>
    /// The unit matches one character constrained by a character class (<c>[...]</c>).
    /// </summary>
    Class = 2,

    /// <summary>
    /// The unit matches zero or more characters (the <c>*</c> metacharacter).
    /// </summary>
    Star = 3,
}
