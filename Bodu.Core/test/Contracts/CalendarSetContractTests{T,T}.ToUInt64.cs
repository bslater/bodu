// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.ToUInt64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that the value at index <c>n</c> of the domain sets bit <c>n</c>, and only that bit.
    /// </summary>
    [TestMethod]
    public void ToUInt64_WhenSetSelectsOneValue_ShouldSetTheBitAtItsIndex()
    {
        for (int index = 0; index < DomainSize; index++)
            Assert.AreEqual(1UL << index, ToUInt64(Create(ElementAt(index))), $"value {ElementAt(index)}");
    }

    /// <summary>
    /// Verifies that a set of several values sets the bit at each value's index.
    /// </summary>
    [TestMethod]
    public void ToUInt64_WhenSetSelectsSeveralValues_ShouldSetEachValuesBit()
    {
        int last = DomainSize - 1;

        TSet set = Create(ElementAt(0), ElementAt(2), ElementAt(last));

        Assert.AreEqual(1UL | (1UL << 2) | (1UL << last), ToUInt64(set));
    }
}
