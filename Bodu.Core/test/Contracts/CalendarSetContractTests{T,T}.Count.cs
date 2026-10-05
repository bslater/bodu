// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <c>Count</c> is zero for the empty set.
    /// </summary>
    [TestMethod]
    public void Count_WhenSetIsEmpty_ShouldReturnZero()
    {
        Assert.AreEqual(0, Count(Empty));
    }

    /// <summary>
    /// Verifies that <c>Count</c> equals the number of values in the domain the set selects.
    /// </summary>
    [TestMethod]
    public void Count_WhenSetSelectsValues_ShouldReturnNumberSelected()
    {
        foreach (TSet set in SampleSets())
        {
            int expected = Membership(set).Count(selected => selected);

            Assert.AreEqual(expected, Count(set), $"set {ToUInt64(set):X}");
        }
    }
}
