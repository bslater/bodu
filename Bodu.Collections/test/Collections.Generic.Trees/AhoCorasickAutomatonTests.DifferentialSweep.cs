// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonTests.DifferentialSweep.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Collections.Generic.Trees;

public sealed partial class AhoCorasickAutomatonTests
{
    /// <summary>
    /// Verifies that over seeded random corpora — 5,000-character texts on a four-character alphabet with 50 random
    /// patterns of lengths 1..8 — the automaton reports exactly the match set found by a brute-force per-pattern
    /// <see cref="string.IndexOf(string, int, StringComparison)" /> scan, that the enumeration honors the documented
    /// (end index, pattern length) order, and that the eager span conveniences agree with the lazy enumeration.
    /// </summary>
    /// <param name="seed">The seed for the deterministic corpus generator.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(20260705)]
    [DataRow(42)]
    [DataRow(987654321)]
    public void EnumerateMatches_WhenSweptAgainstIndexOfOracle_ShouldAgree(int seed)
    {
        const string alphabet = "abcd";
        const int textLength = 5_000;
        const int patternCount = 50;
        var random = new Random(seed);

        var textBuilder = new StringBuilder(textLength);
        for (int i = 0; i < textLength; i++)
            textBuilder.Append(alphabet[random.Next(alphabet.Length)]);
        string text = textBuilder.ToString();

        var patterns = new HashSet<string>(StringComparer.Ordinal);
        while (patterns.Count < patternCount)
        {
            int length = random.Next(1, 9);
            var patternBuilder = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                patternBuilder.Append(alphabet[random.Next(alphabet.Length)]);
            patterns.Add(patternBuilder.ToString());
        }

        var sut = AhoCorasickAutomaton.Build(patterns);
        var matches = sut.EnumerateMatches(text).ToArray();

        // Oracle: brute-force per-pattern IndexOf scan over the whole text.
        var expected = new HashSet<(string Pattern, int Start)>();
        foreach (string pattern in patterns)
        {
            int index = text.IndexOf(pattern, 0, StringComparison.Ordinal);
            while (index >= 0)
            {
                expected.Add((pattern, index));
                index = index + 1 <= text.Length - pattern.Length
                    ? text.IndexOf(pattern, index + 1, StringComparison.Ordinal)
                    : -1;
            }
        }

        var actual = matches.Select(m => (m.Pattern, m.Start)).ToHashSet();
        Assert.AreEqual(matches.Length, actual.Count, "The automaton reported a duplicate match.");
        Assert.IsTrue(actual.SetEquals(expected), $"Match sets differ for seed {seed}: expected {expected.Count}, got {actual.Count}.");

        // The documented order: ascending end index, then ascending pattern length.
        for (int i = 1; i < matches.Length; i++)
        {
            bool ordered = matches[i - 1].End < matches[i].End
                || (matches[i - 1].End == matches[i].End && matches[i - 1].Pattern.Length < matches[i].Pattern.Length);

            Assert.IsTrue(ordered, $"Match {i} breaks the (end, length) order for seed {seed}.");
        }

        // The eager span conveniences agree with the lazy enumeration.
        Assert.AreEqual(matches.Length, sut.CountMatches(text));
        Assert.AreEqual(matches.Length > 0, sut.HasMatch(text));
    }
}
