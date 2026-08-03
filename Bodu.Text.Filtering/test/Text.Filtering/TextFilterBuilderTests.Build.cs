// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterBuilderTests.Build.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for <see cref="TextFilterBuilder.Build" />.
/// </content>
public partial class TextFilterBuilderTests
{
    /// <summary>
    /// Verifies that the built filter compiles with the options supplied to the builder.
    /// </summary>
    [TestMethod]
    public void Build_WhenOptionsSupplied_ShouldApplyThemToTheFilter()
    {
        var options = new TextFilterOptions { Mode = TextFilterEvaluationMode.LastMatchWins, IgnoreCase = false };

        var filter = new TextFilterBuilder(options).AddExclude("*.log").AddInclude("important.log").Build();

        Assert.AreEqual(TextFilterEvaluationMode.LastMatchWins, filter.Mode);
        Assert.IsFalse(filter.IgnoreCase);
        Assert.IsTrue(filter.IsMatch("important.log"));
        Assert.IsFalse(filter.IsMatch("other.log"));
    }

    /// <summary>
    /// Verifies that an empty builder produces an include-all filter.
    /// </summary>
    [TestMethod]
    public void Build_WhenNoPatternsAdded_ShouldProduceIncludeAllFilter()
    {
        var filter = new TextFilterBuilder().Build();

        Assert.AreEqual(0, filter.Patterns.Count);
        Assert.IsTrue(filter.IsMatch("anything"));
    }

    /// <summary>
    /// Verifies that the builder keeps accumulating after a build, and later builds see the additional patterns.
    /// </summary>
    [TestMethod]
    public void Build_WhenCalledTwice_ShouldIncludePatternsAddedBetweenBuilds()
    {
        var builder = new TextFilterBuilder().AddInclude("a*");
        var first = builder.Build();

        var second = builder.AddInclude("b*").Build();

        Assert.AreEqual(1, first.Patterns.Count);
        Assert.AreEqual(2, second.Patterns.Count);
    }
}
