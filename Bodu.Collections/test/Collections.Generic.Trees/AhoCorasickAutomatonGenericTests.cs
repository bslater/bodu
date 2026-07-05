// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonGenericTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Contains unit tests for the <see cref="AhoCorasickAutomaton{TValue}" /> value-carrying multi-pattern matcher.
/// </summary>
[TestClass]
public sealed partial class AhoCorasickAutomatonGenericTests
{
    /// <summary>
    /// Verifies that a keyed automaton reports the classic <c>"ushers"</c> matches with each pattern's associated
    /// value, in the documented (end index, pattern length) order.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void EnumerateMatches_WhenClassicPatternSet_ShouldReportMatchesWithValues()
    {
        var sut = AhoCorasickAutomaton<int>.Build(
        [
            new KeyValuePair<string, int>("he", 1),
            new KeyValuePair<string, int>("she", 2),
            new KeyValuePair<string, int>("his", 3),
            new KeyValuePair<string, int>("hers", 4),
        ]);

        var matches = sut.EnumerateMatches("ushers").ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new AhoCorasickMatch<int>("he", 2, 1),
                new AhoCorasickMatch<int>("she", 1, 2),
                new AhoCorasickMatch<int>("hers", 2, 4),
            },
            matches);
    }
}
