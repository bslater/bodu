// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardPatternTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Filtering;

/// <summary>
/// Tests for the internal <see cref="WildcardPattern" /> engine: brace expansion, classification, and matching.
/// </summary>
[TestClass]
public partial class WildcardPatternTests
{
    /// <summary>
    /// Compiles a brace-free wildcard pattern and matches it against a value through the full strategy dispatch.
    /// </summary>
    /// <param name="pattern">The wildcard pattern.</param>
    /// <param name="text">The value to test.</param>
    /// <param name="ignoreCase">Whether matching folds case.</param>
    /// <returns>The match outcome.</returns>
    private static bool Match(string pattern, string text, bool ignoreCase)
    {
        var program = WildcardPattern.Compile(pattern);
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        return program.IsMatch(text, comparison, ignoreCase);
    }

    /// <summary>
    /// A naive exponential reference matcher for patterns over literals, <c>*</c>, and <c>?</c> — the oracle the
    /// optimized matcher is pinned against in the Regression sweep. Deliberately implemented by direct recursion on
    /// the pattern text so it shares no code with the implementation under test.
    /// </summary>
    /// <param name="pattern">The wildcard pattern (literals, <c>*</c>, and <c>?</c> only).</param>
    /// <param name="text">The value to test.</param>
    /// <returns>The match outcome.</returns>
    private static bool OracleMatch(string pattern, string text) =>
        OracleMatch(pattern, 0, text, 0);

    /// <summary>
    /// The recursive core of <see cref="OracleMatch(string, string)" />.
    /// </summary>
    /// <param name="pattern">The wildcard pattern.</param>
    /// <param name="p">The current pattern index.</param>
    /// <param name="text">The value to test.</param>
    /// <param name="t">The current value index.</param>
    /// <returns>The match outcome from the given positions.</returns>
    private static bool OracleMatch(string pattern, int p, string text, int t)
    {
        if (p == pattern.Length)
            return t == text.Length;

        if (pattern[p] == '*')
            return OracleMatch(pattern, p + 1, text, t) || (t < text.Length && OracleMatch(pattern, p, text, t + 1));

        if (t == text.Length)
            return false;

        return (pattern[p] == '?' || pattern[p] == text[t]) && OracleMatch(pattern, p + 1, text, t + 1);
    }

    /// <summary>
    /// A single wildcard match expectation: a pattern, a value, the case mode, and the expected outcome.
    /// </summary>
    /// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
    /// <param name="Pattern">The wildcard pattern.</param>
    /// <param name="Text">The value to test.</param>
    /// <param name="IgnoreCase">Whether matching folds case.</param>
    /// <param name="Expected">The expected match outcome.</param>
    public sealed record WildcardMatchKat(
        string Name,
        string Pattern,
        string Text,
        bool IgnoreCase,
        bool Expected) : IKat;
}
