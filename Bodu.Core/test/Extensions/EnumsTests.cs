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
    /// Verifies that <see cref="Enums.GetValues{TEnum}" /> returns the declared values as a fresh array on each call.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void GetValues_WhenQueried_ShouldReturnDeclaredValues()
    {
        DescribedStatus[] first = Enums.GetValues<DescribedStatus>();

        CollectionAssert.AreEqual(
            new[] { DescribedStatus.Active, DescribedStatus.Pending, DescribedStatus.Plain, DescribedStatus.Cancelled },
            first);
        Assert.AreNotSame(first, Enums.GetValues<DescribedStatus>());
    }

    /// <summary>
    /// Verifies that mutating the array returned by <see cref="Enums.GetValues{TEnum}" /> does not affect subsequent calls.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenReturnedArrayMutated_ShouldNotAffectSubsequentCalls()
    {
        DescribedStatus[] values = Enums.GetValues<DescribedStatus>();
        values[0] = (DescribedStatus)999;

        CollectionAssert.AreEqual(
            new[] { DescribedStatus.Active, DescribedStatus.Pending, DescribedStatus.Plain, DescribedStatus.Cancelled },
            Enums.GetValues<DescribedStatus>());
    }

    /// <summary>
    /// Verifies that <see cref="Enums.GetNames{TEnum}" /> returns the declared names as a fresh array on each call.
    /// </summary>
    [TestMethod]
    public void GetNames_WhenQueried_ShouldReturnDeclaredNames()
    {
        string[] names = Enums.GetNames<DescribedStatus>();

        CollectionAssert.AreEqual(new[] { "Active", "Pending", "Plain", "Cancelled" }, names);
        Assert.AreNotSame(names, Enums.GetNames<DescribedStatus>());
    }

    /// <summary>
    /// Verifies that mutating the array returned by <see cref="Enums.GetNames{TEnum}" /> does not affect subsequent calls.
    /// </summary>
    [TestMethod]
    public void GetNames_WhenReturnedArrayMutated_ShouldNotAffectSubsequentCalls()
    {
        string[] names = Enums.GetNames<DescribedStatus>();
        names[0] = "Corrupted";

        CollectionAssert.AreEqual(new[] { "Active", "Pending", "Plain", "Cancelled" }, Enums.GetNames<DescribedStatus>());
    }

    /// <summary>
    /// Verifies that <see cref="Enums.TryParse{TEnum}(string, out TEnum)" /> parses valid names and rejects invalid text.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextValidOrInvalid_ShouldReportOutcome()
    {
        Assert.IsTrue(Enums.TryParse<DescribedStatus>("Pending", out DescribedStatus parsed));
        Assert.AreEqual(DescribedStatus.Pending, parsed);

        Assert.IsFalse(Enums.TryParse<DescribedStatus>("Nope", out _));
    }

    /// <summary>
    /// Verifies that the case-insensitive overload of <see cref="Enums.TryParse{TEnum}(string, bool, out TEnum)" /> ignores case.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenIgnoreCase_ShouldMatchRegardlessOfCase()
    {
        Assert.IsTrue(Enums.TryParse<DescribedStatus>("active", ignoreCase: true, out DescribedStatus parsed));
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
        Assert.IsTrue(Enums.TryParseDescription<DescribedStatus>("Is Active", out DescribedStatus byDescription));
        Assert.AreEqual(DescribedStatus.Active, byDescription);

        Assert.IsTrue(Enums.TryParseDescription<DescribedStatus>("Pend", out DescribedStatus byDisplay));
        Assert.AreEqual(DescribedStatus.Pending, byDisplay);

        Assert.IsFalse(Enums.TryParseDescription<DescribedStatus>("unknown", out _));
    }

    /// <summary>
    /// Verifies that <see cref="Enums.TryParseDescription{TEnum}(string, out TEnum)" /> resolves a description shared by
    /// two members to the first declaring value, deterministically.
    /// </summary>
    [TestMethod]
    public void TryParseDescription_WhenDescriptionIsDuplicated_ShouldResolveFirstDeclaringValue()
    {
        Assert.IsTrue(Enums.TryParseDescription<DuplicateDescribed>("Shared", out DuplicateDescribed resolved));
        Assert.AreEqual(DuplicateDescribed.First, resolved);
    }
}
