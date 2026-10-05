// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.Equals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <c>Equals</c> answers <see langword="true" /> for sets that select the same values, both typed and
    /// boxed.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSetsSelectSameValues_ShouldReturnTrue()
    {
        foreach (TSet set in SampleSets())
        {
            TSet rebuilt = Create(set.ToArray());

            Assert.IsTrue(set.Equals(rebuilt), $"{ToUInt64(set):X}");
            Assert.IsTrue(set.Equals((object)rebuilt), $"{ToUInt64(set):X} boxed");
        }
    }

    /// <summary>
    /// Verifies that <c>Equals</c> answers <see langword="false" /> for sets that differ in a single value, both typed
    /// and boxed.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSetsDifferInOneValue_ShouldReturnFalse()
    {
        foreach (TSet set in SampleSets())
        {
            TSet other = FromUInt64(ToUInt64(set) ^ (1UL << (DomainSize - 1)));

            Assert.IsFalse(set.Equals(other), $"{ToUInt64(set):X}");
            Assert.IsFalse(set.Equals((object)other), $"{ToUInt64(set):X} boxed");
        }
    }

    /// <summary>
    /// Verifies that <c>Equals(object)</c> answers <see langword="false" /> for <see langword="null" /> and for an object
    /// of another type, including the set's own bits boxed as a <see cref="ulong" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenObjectIsNullOrAnotherType_ShouldReturnFalse()
    {
        TSet set = Create(ElementAt(0));

        Assert.IsFalse(set.Equals(null));
        Assert.IsFalse(set.Equals((object)ToUInt64(set)));
        Assert.IsFalse(set.Equals((object)set.ToString()));
    }
}
