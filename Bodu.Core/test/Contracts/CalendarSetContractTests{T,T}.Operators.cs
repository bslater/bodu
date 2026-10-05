// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <c>|</c> selects the values either set selects.
    /// </summary>
    [TestMethod]
    public void OrOperator_WhenSetsCombined_ShouldSelectValuesEitherSelects()
    {
        foreach ((TSet left, TSet right) in SamplePairs())
        {
            bool[] l = Membership(left);
            bool[] r = Membership(right);

            AssertSelects(i => l[i] || r[i], left | right, $"{ToUInt64(left):X} | {ToUInt64(right):X}");
        }
    }

    /// <summary>
    /// Verifies that <c>&amp;</c> selects the values both sets select.
    /// </summary>
    [TestMethod]
    public void AndOperator_WhenSetsCombined_ShouldSelectValuesBothSelect()
    {
        foreach ((TSet left, TSet right) in SamplePairs())
        {
            bool[] l = Membership(left);
            bool[] r = Membership(right);

            AssertSelects(i => l[i] && r[i], left & right, $"{ToUInt64(left):X} & {ToUInt64(right):X}");
        }
    }

    /// <summary>
    /// Verifies that <c>^</c> selects the values exactly one of the sets selects.
    /// </summary>
    [TestMethod]
    public void XorOperator_WhenSetsCombined_ShouldSelectValuesExactlyOneSelects()
    {
        foreach ((TSet left, TSet right) in SamplePairs())
        {
            bool[] l = Membership(left);
            bool[] r = Membership(right);

            AssertSelects(i => l[i] != r[i], left ^ right, $"{ToUInt64(left):X} ^ {ToUInt64(right):X}");
        }
    }

    /// <summary>
    /// Verifies that <c>~</c> selects exactly the values of the domain the set does not select.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenApplied_ShouldSelectExactlyTheUnselectedValues()
    {
        foreach (TSet set in SampleSets())
        {
            bool[] before = Membership(set);

            AssertSelects(i => !before[i], ~set, $"~{ToUInt64(set):X}");
        }
    }

    /// <summary>
    /// Verifies that <c>~</c> sets no bit above the domain, so the complement counts, compares and round-trips like
    /// any other set.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenApplied_ShouldSetNoBitAboveDomain()
    {
        foreach (TSet set in SampleSets())
        {
            TSet complement = ~set;

            Assert.AreEqual(0UL, ToUInt64(complement) & ~DomainBits, $"~{ToUInt64(set):X}");
            Assert.AreEqual(DomainSize - Count(set), Count(complement), $"~{ToUInt64(set):X}");
            Assert.AreEqual(set, ~complement, $"~~{ToUInt64(set):X}");
        }
    }

    /// <summary>
    /// Verifies that the complement of the empty set is <c>All</c>, and the complement of <c>All</c> is empty.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenAppliedToEmptyOrAll_ShouldReturnTheOther()
    {
        Assert.AreEqual(All, ~Empty);
        Assert.AreEqual(Empty, ~All);
    }

    /// <summary>
    /// Verifies that <c>==</c> answers <see langword="true" /> and <c>!=</c> answers <see langword="false" /> for sets
    /// that select the same values, however they were built.
    /// </summary>
    [TestMethod]
    public void EqualityOperator_WhenSetsSelectSameValues_ShouldReturnTrue()
    {
        foreach (TSet set in SampleSets())
        {
            TSet rebuilt = Create(set.ToArray());

            Assert.IsTrue(set == rebuilt, $"{ToUInt64(set):X} == rebuilt");
            Assert.IsFalse(set != rebuilt, $"{ToUInt64(set):X} != rebuilt");
        }
    }

    /// <summary>
    /// Verifies that <c>==</c> answers <see langword="false" /> and <c>!=</c> answers <see langword="true" /> for sets
    /// that differ in a single value.
    /// </summary>
    [TestMethod]
    public void InequalityOperator_WhenSetsDifferInOneValue_ShouldReturnTrue()
    {
        foreach (TSet set in SampleSets())
        {
            TSet other = FromUInt64(ToUInt64(set) ^ 1UL);

            Assert.IsTrue(set != other, $"{ToUInt64(set):X} != {ToUInt64(other):X}");
            Assert.IsFalse(set == other, $"{ToUInt64(set):X} == {ToUInt64(other):X}");
        }
    }
}
