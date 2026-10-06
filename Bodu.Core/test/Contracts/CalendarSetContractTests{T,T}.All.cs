// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.All.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <c>All</c> selects every value of the domain.
    /// </summary>
    [TestMethod]
    public void All_WhenAccessed_ShouldSelectEveryValue()
    {
        AssertSelects(_ => true, All, "All");
        Assert.AreEqual(DomainSize, Count(All));
    }

    /// <summary>
    /// Verifies that <c>All</c> sets exactly the domain's bits, and none above them.
    /// </summary>
    [TestMethod]
    public void All_WhenAccessed_ShouldSetExactlyTheDomainBits()
    {
        Assert.AreEqual(DomainBits, ToUInt64(All));
    }
}
