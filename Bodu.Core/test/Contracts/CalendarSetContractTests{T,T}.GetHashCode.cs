// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.GetHashCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that sets that select the same values, however they were built, have the same hash code.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenSetsAreEqual_ShouldReturnSameValue()
    {
        foreach (TSet set in SampleSets())
        {
            TSet rebuilt = Create(set.ToArray());
            TSet fromBits = FromUInt64(ToUInt64(set));

            Assert.AreEqual(set.GetHashCode(), rebuilt.GetHashCode(), $"{ToUInt64(set):X} rebuilt");
            Assert.AreEqual(set.GetHashCode(), fromBits.GetHashCode(), $"{ToUInt64(set):X} from bits");
        }
    }
}
