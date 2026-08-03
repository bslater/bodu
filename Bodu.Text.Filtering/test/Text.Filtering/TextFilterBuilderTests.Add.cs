// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterBuilderTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <content>
/// Tests for the pattern-adding members: <see cref="TextFilterBuilder.AddInclude" />,
/// <see cref="TextFilterBuilder.AddExclude" />, the regex variants, and <see cref="TextFilterBuilder.Add" />.
/// </content>
public partial class TextFilterBuilderTests
{
    /// <summary>
    /// Verifies that the wildcard adders record patterns with the expected action and kind, in order.
    /// </summary>
    [TestMethod]
    public void AddInclude_WhenChainedWithAddExclude_ShouldAccumulateInOrder()
    {
        var filter = new TextFilterBuilder()
            .AddInclude("error*")
            .AddExclude("*debug*")
            .Build();

        Assert.AreEqual(2, filter.Patterns.Count);
        Assert.AreEqual(TextFilterAction.Include, filter.Patterns[0].Action);
        Assert.AreEqual(TextFilterPatternKind.Wildcard, filter.Patterns[0].Kind);
        Assert.AreEqual(TextFilterAction.Exclude, filter.Patterns[1].Action);
    }

    /// <summary>
    /// Verifies that the regex adders record regular-expression patterns.
    /// </summary>
    [TestMethod]
    public void AddIncludeRegex_WhenChainedWithAddExcludeRegex_ShouldRecordRegexKind()
    {
        var filter = new TextFilterBuilder()
            .AddIncludeRegex("^err")
            .AddExcludeRegex("x$")
            .Build();

        Assert.AreEqual(TextFilterPatternKind.Regex, filter.Patterns[0].Kind);
        Assert.AreEqual(TextFilterPatternKind.Regex, filter.Patterns[1].Kind);
        Assert.IsTrue(filter.IsMatch("error"));
        Assert.IsFalse(filter.IsMatch("errx"));
    }

    /// <summary>
    /// Verifies that an already constructed pattern is added as-is.
    /// </summary>
    [TestMethod]
    public void Add_WhenGivenConstructedPattern_ShouldUseItDirectly()
    {
        var pattern = TextFilterPattern.Exclude("*.tmp", ignoreCase: false);

        var filter = new TextFilterBuilder().Add(pattern).Build();

        Assert.AreSame(pattern, filter.Patterns[0]);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> pattern throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenPatternIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TextFilterBuilder().Add(null!);
        });

        Assert.AreEqual("pattern", ex.ParamName);
    }
}
