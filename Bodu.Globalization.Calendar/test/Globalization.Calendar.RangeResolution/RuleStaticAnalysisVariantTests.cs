// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleStaticAnalysisVariantTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Verifies that <see cref="RuleStaticAnalysis" /> keys profiles and offset anchors by full
/// <see cref="NotableDateRuleIdentity" /> so that distinct variants sharing a canonical name (for example western and
/// orthodox Easter) neither collapse under a name-only key nor bind an offset rule to an arbitrary same-name candidate.
/// </summary>
[TestClass]
public sealed class RuleStaticAnalysisVariantTests
{
    private static NotableDateRule Fixed(string name, string? variant, int month, int day) => new()
    {
        Name = name,
        RuleName = variant,
        Strategy = DateResolutionStrategy.Fixed,
        Category = NotableDateCategory.Holiday,
        Month = month,
        Day = day,
    };

    private static NotableDateRule Algorithmic(string name, string variant, string algorithmKey) => new()
    {
        Name = name,
        RuleName = variant,
        Strategy = DateResolutionStrategy.Algorithm,
        Category = NotableDateCategory.Holiday,
        AlgorithmKey = algorithmKey,
    };

    /// <summary>
    /// Verifies that two algorithmic rules sharing a canonical name but differing by variant are exposed as distinct
    /// profiles, each retrievable by its full identity rather than collapsing last-writer-wins under the shared name.
    /// </summary>
    [TestMethod]
    public void Build_WhenTwoAlgorithmicVariantsShareName_ShouldExposeDistinctProfilesByIdentity()
    {
        NotableDateRule western = Algorithmic("Easter", "western", "easter-western");
        NotableDateRule orthodox = Algorithmic("Easter", "orthodox", "easter-orthodox");

        var analysis = RuleStaticAnalysis.Build([western, orthodox]);

        Assert.IsTrue(analysis.TryGetProfile(NotableDateRuleIdentity.From(western), out RuleStaticProfile w));
        Assert.AreEqual("easter-western", w.Rule.AlgorithmKey);

        Assert.IsTrue(analysis.TryGetProfile(NotableDateRuleIdentity.From(orthodox), out RuleStaticProfile o));
        Assert.AreEqual("easter-orthodox", o.Rule.AlgorithmKey);
    }

    /// <summary>
    /// Verifies that an offset rule that authors an <see cref="NotableDateRule.AnchorRuleVariant" /> binds its root anchor
    /// to that specific same-name variant rather than the first candidate.
    /// </summary>
    [TestMethod]
    public void Build_WhenOffsetAnchorVariantAuthored_ShouldClassifyRootToThatVariant()
    {
        NotableDateRule early = Fixed("Spring Festival", "early", 3, 10);
        NotableDateRule late = Fixed("Spring Festival", "late", 3, 20);
        NotableDateRule eve = new()
        {
            Name = "Festival Eve",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Holiday,
            AnchorRuleName = "Spring Festival",
            AnchorRuleVariant = "late",
            OffsetDays = -1,
        };

        var analysis = RuleStaticAnalysis.Build([early, late, eve]);

        RuleStaticProfile profile = analysis.Profiles.Single(p => p.Rule.Name == "Festival Eve");
        Assert.AreEqual(RuleTier.OffsetFromFixed, profile.Tier);
        Assert.AreEqual("late", profile.RootAnchorIdentity?.RuleName);
        Assert.AreEqual(-1, profile.OffsetFromRoot);
    }

    /// <summary>
    /// Verifies that an offset rule whose anchor name matches several variants — with no authored variant or context to
    /// disambiguate — is not silently bound to an arbitrary candidate; the chain degrades to no root anchor instead.
    /// </summary>
    [TestMethod]
    public void Build_WhenOffsetAnchorIsAmbiguous_ShouldNotBindToArbitraryVariant()
    {
        NotableDateRule early = Fixed("Spring Festival", "early", 3, 10);
        NotableDateRule late = Fixed("Spring Festival", "late", 3, 20);
        NotableDateRule eve = new()
        {
            Name = "Festival Eve",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Holiday,
            AnchorRuleName = "Spring Festival",
            OffsetDays = -1,
        };

        var analysis = RuleStaticAnalysis.Build([early, late, eve]);

        RuleStaticProfile profile = analysis.Profiles.Single(p => p.Rule.Name == "Festival Eve");
        Assert.IsNull(profile.RootAnchorIdentity);
        Assert.AreEqual(RuleTier.Fixed, profile.Tier);
    }
}
