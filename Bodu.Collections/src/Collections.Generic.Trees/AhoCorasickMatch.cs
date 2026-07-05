// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickMatch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Represents a single pattern occurrence reported by <see cref="AhoCorasickAutomaton.EnumerateMatches(string)" />.
/// </summary>
/// <param name="Pattern">The pattern that matched.</param>
/// <param name="Start">The zero-based index in the text at which the occurrence begins.</param>
/// <remarks>
/// Matches are value-equatable: two matches are equal when they report the same pattern at the same start index.
/// </remarks>
public readonly record struct AhoCorasickMatch(string Pattern, int Start)
{
    /// <summary>
    /// Gets the exclusive zero-based end index of the occurrence.
    /// </summary>
    /// <value>
    /// The index immediately after the last matched character, equal to <see cref="Start" /> plus the pattern length.
    /// </value>
    public int End =>
        Start + Pattern.Length;
}
