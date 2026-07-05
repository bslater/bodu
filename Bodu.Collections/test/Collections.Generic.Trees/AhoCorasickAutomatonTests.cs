// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Contains unit tests for the <see cref="AhoCorasickAutomaton" /> multi-pattern matcher.
/// </summary>
[TestClass]
public sealed partial class AhoCorasickAutomatonTests
{
    /// <summary>The classic Aho-Corasick example pattern set.</summary>
    private static readonly string[] ClassicPatterns = ["he", "she", "his", "hers"];

    /// <summary>
    /// Builds an automaton over the classic <c>he / she / his / hers</c> pattern set.
    /// </summary>
    /// <returns>The built automaton.</returns>
    private static AhoCorasickAutomaton CreateClassic() =>
        AhoCorasickAutomaton.Build(ClassicPatterns);

    /// <summary>
    /// Verifies that the classic <c>he / she / his / hers</c> pattern set over <c>"ushers"</c> reports exactly the
    /// published match set — <c>she@1</c>, <c>he@2</c>, <c>hers@2</c> — in the documented order (ascending end index,
    /// then ascending pattern length).
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void EnumerateMatches_WhenClassicPatternSet_ShouldReportKnownMatches()
    {
        var sut = CreateClassic();

        var matches = sut.EnumerateMatches("ushers").ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new AhoCorasickMatch("he", 2),
                new AhoCorasickMatch("she", 1),
                new AhoCorasickMatch("hers", 2),
            },
            matches);
    }
}
