// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickMatch{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Represents a single pattern occurrence reported by
/// <see cref="AhoCorasickAutomaton{TValue}.EnumerateMatches(string)" />, carrying the value associated with the matched
/// pattern.
/// </summary>
/// <typeparam name="TValue">The type of the value associated with each pattern.</typeparam>
/// <param name="Pattern">The pattern that matched.</param>
/// <param name="Start">The zero-based index in the text at which the occurrence begins.</param>
/// <param name="Value">The value associated with the matched pattern.</param>
/// <remarks>
/// Matches are value-equatable: two matches are equal when they report the same pattern, start index, and value.
/// </remarks>
public readonly record struct AhoCorasickMatch<TValue>(string Pattern, int Start, TValue Value)
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
