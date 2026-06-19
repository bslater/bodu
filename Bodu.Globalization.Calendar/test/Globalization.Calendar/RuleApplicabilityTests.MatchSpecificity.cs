// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleApplicabilityTests.MatchSpecificity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class RuleApplicabilityTests
{
    /// <summary>
    /// Verifies that <see cref="RuleApplicability.MatchSpecificity" /> returns <c>0</c> for a global rule, the matching
    /// scope length for an exact or parent match, and <c>-1</c> when the rule does not match.
    /// </summary>
    /// <param name="scope">The single territory the rule is scoped to, or an empty string for a global rule.</param>
    /// <param name="requested">The requested territory code.</param>
    /// <param name="expected">The expected specificity.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("", "AU", 0)]          // global
    [DataRow("", "AU-NSW", 0)]      // global
    [DataRow("AU", "AU", 2)]        // exact national
    [DataRow("AU", "AU-NSW", 2)]    // parent of subnational
    [DataRow("AU-NSW", "AU-NSW", 6)] // exact subnational
    [DataRow("AU", "US", -1)]       // no match
    [DataRow("AU-NSW", "AU", -1)]   // subnational scope, national request
    public void MatchSpecificity_WhenScoped_ShouldReturnExpected(string scope, string requested, int expected)
    {
        RuleApplicability applicability = scope.Length == 0 ? Territories() : Territories(scope);

        Assert.AreEqual(expected, applicability.MatchSpecificity(requested));
    }

    /// <summary>
    /// Verifies that a narrower scope yields a higher specificity than a broader scope for the same subnational request,
    /// which is the ordering that lets a subdivision rule shadow a national rule.
    /// </summary>
    [TestMethod]
    public void MatchSpecificity_WhenNarrowerScope_ShouldExceedBroaderScope()
    {
        int national = Territories("AU").MatchSpecificity("AU-NSW");
        int subnational = Territories("AU-NSW").MatchSpecificity("AU-NSW");

        Assert.IsGreaterThan(national, subnational);
    }
}
