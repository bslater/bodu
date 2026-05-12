// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultisetTests.ExplicitInterfaceEnumeration.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Collections.Generic;

public partial class MultisetTests
{
    /// <summary>
    /// Verifies that the explicit <see cref="IEnumerable{T}.GetEnumerator" /> on a <see cref="Multiset{T}" /> yields elements with their
    /// respective multiplicity.
    /// </summary>
    [TestMethod]
    public void GenericIEnumerableGetEnumerator_ShouldYieldElementsWithMultiplicity()
    {
        var multiset = new Multiset<int>();
        multiset.Add(1, 3);
        multiset.Add(2, 2);

        IEnumerable<int> generic = multiset;
        var observed = new List<int>();
        foreach (int value in generic)
            observed.Add(value);

        observed.Sort();
        CollectionAssert.AreEqual(new[] { 1, 1, 1, 2, 2 }, observed);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> on a <see cref="Multiset{T}" /> yields elements with their
    /// respective multiplicity.
    /// </summary>
    [TestMethod]
    public void NonGenericIEnumerableGetEnumerator_ShouldYieldElementsWithMultiplicity()
    {
        var multiset = new Multiset<int>();
        multiset.Add(7, 2);
        multiset.Add(8, 1);

        IEnumerable nonGeneric = multiset;
        var observed = new List<int>();
        foreach (object item in nonGeneric)
            observed.Add((int)item);

        observed.Sort();
        CollectionAssert.AreEqual(new[] { 7, 7, 8 }, observed);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerator.Current" /> on the <see cref="Multiset{T}.Enumerator" /> returns the same
    /// value as the strongly typed <c>Current</c> after a successful <c>MoveNext</c>.
    /// </summary>
    [TestMethod]
    public void EnumeratorNonGenericCurrent_AfterMoveNext_ShouldReturnSameAsTypedCurrent()
    {
        var multiset = new Multiset<int>();
        multiset.Add(99);

        var typed = multiset.GetEnumerator();
        IEnumerator legacy = typed;

        Assert.IsTrue(typed.MoveNext());
        Assert.AreEqual(99, legacy.Current);
        typed.Dispose();
    }
}
