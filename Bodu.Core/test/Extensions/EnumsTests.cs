// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Contains unit tests for the <see cref="Enums" /> static helper.
/// </summary>
[TestClass]
public sealed class EnumsTests
{
    /// <summary>
    /// Verifies that <see cref="Enums.GetValues{TEnum}" /> returns the declared values and serves a cached array.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetValues_WhenQueried_ShouldReturnCachedValues()
    {
        var first = Enums.GetValues<DescribedStatus>();

        CollectionAssert.AreEqual(
            new[] { DescribedStatus.Active, DescribedStatus.Pending, DescribedStatus.Plain, DescribedStatus.Cancelled },
            first);
        Assert.AreSame(first, Enums.GetValues<DescribedStatus>());
    }

    /// <summary>
    /// Verifies that <see cref="Enums.GetNames{TEnum}" /> returns the declared names and serves a cached array.
    /// </summary>
    [TestMethod]
    public void GetNames_WhenQueried_ShouldReturnCachedNames()
    {
        var names = Enums.GetNames<DescribedStatus>();

        CollectionAssert.AreEqual(new[] { "Active", "Pending", "Plain", "Cancelled" }, names);
        Assert.AreSame(names, Enums.GetNames<DescribedStatus>());
    }

    /// <summary>
    /// Verifies that <see cref="Enums.TryParse{TEnum}(string, out TEnum)" /> parses valid names and rejects invalid text.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextValidOrInvalid_ShouldReportOutcome()
    {
        Assert.IsTrue(Enums.TryParse<DescribedStatus>("Pending", out var parsed));
        Assert.AreEqual(DescribedStatus.Pending, parsed);

        Assert.IsFalse(Enums.TryParse<DescribedStatus>("Nope", out _));
    }

    /// <summary>
    /// Verifies that the case-insensitive overload of <see cref="Enums.TryParse{TEnum}(string, bool, out TEnum)" /> ignores case.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenIgnoreCase_ShouldMatchRegardlessOfCase()
    {
        Assert.IsTrue(Enums.TryParse<DescribedStatus>("active", ignoreCase: true, out var parsed));
        Assert.AreEqual(DescribedStatus.Active, parsed);

        Assert.IsFalse(Enums.TryParse<DescribedStatus>("active", ignoreCase: false, out _));
    }

    /// <summary>
    /// Verifies that <see cref="Enums.TryParseDescription{TEnum}(string, out TEnum)" /> resolves a value from its
    /// description or display text.
    /// </summary>
    [TestMethod]
    public void TryParseDescription_WhenTextMatches_ShouldResolveValue()
    {
        Assert.IsTrue(Enums.TryParseDescription<DescribedStatus>("Is Active", out var byDescription));
        Assert.AreEqual(DescribedStatus.Active, byDescription);

        Assert.IsTrue(Enums.TryParseDescription<DescribedStatus>("Pend", out var byDisplay));
        Assert.AreEqual(DescribedStatus.Pending, byDisplay);

        Assert.IsFalse(Enums.TryParseDescription<DescribedStatus>("unknown", out _));
    }
}
