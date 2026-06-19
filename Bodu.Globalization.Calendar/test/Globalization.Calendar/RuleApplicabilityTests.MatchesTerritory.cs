// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleApplicabilityTests.MatchesTerritory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class RuleApplicabilityTests
{
    /// <summary>
    /// Verifies that a global rule (no territories) matches every requested territory.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    [TestMethod]
    [DataRow("US")]
    [DataRow("AU")]
    [DataRow("AU-NSW")]
    public void MatchesTerritory_WhenGlobal_ShouldMatchEveryTerritory(string territory)
    {
        Assert.IsTrue(Territories().MatchesTerritory(territory));
    }

    /// <summary>
    /// Verifies that a territory-scoped rule matches an exact code, matches a subnational child of a national scope, and
    /// rejects a national request against a subnational-only scope or an unrelated code.
    /// </summary>
    /// <param name="scope">The single territory the rule is scoped to.</param>
    /// <param name="requested">The requested territory code.</param>
    /// <param name="expected">The expected territory-match outcome.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("AU", "AU", true)]        // exact
    [DataRow("AU", "AU-NSW", true)]    // national scope matches subnational request
    [DataRow("AU", "US", false)]       // unrelated
    [DataRow("AU-NSW", "AU-NSW", true)] // exact subnational
    [DataRow("AU-NSW", "AU", false)]   // subnational scope does not match national request
    [DataRow("AU-NSW", "AU-VIC", false)] // sibling subnational
    [DataRow("AU", "au-nsw", true)]    // case-insensitive territory comparison
    [DataRow("au", "AU-NSW", true)]    // case-insensitive scope
    public void MatchesTerritory_WhenScoped_ShouldMatchExpected(string scope, string requested, bool expected)
    {
        Assert.AreEqual(expected, Territories(scope).MatchesTerritory(requested));
    }

    /// <summary>
    /// Verifies that a rule scoped to several territories matches a request against any one of them.
    /// </summary>
    /// <param name="requested">The requested territory code.</param>
    /// <param name="expected">Whether the US/AU-scoped rule is expected to match the request.</param>
    [TestMethod]
    [DataRow("US", true)]       // first listed scope
    [DataRow("AU-NSW", true)]   // subnational of a listed scope
    [DataRow("CA", false)]      // unlisted scope
    public void MatchesTerritory_WhenMultipleScopes_ShouldMatchAnyListed(string requested, bool expected)
    {
        RuleApplicability applicability = Territories("US", "AU");

        Assert.AreEqual(expected, applicability.MatchesTerritory(requested));
    }

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.MatchesTerritory" /> throws <see cref="ArgumentNullException" /> when
    /// the territory is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void MatchesTerritory_WhenTerritoryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Territories("AU").MatchesTerritory(null!);
        });
    }
}
