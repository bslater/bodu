// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.GetEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that enumeration yields exactly the selected values, each once, in bit order.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetSelectsValues_ShouldYieldThemInBitOrder()
    {
        foreach (TSet set in SampleSets())
        {
            TElement[] expected = Domain.Where(value => Contains(set, value)).ToArray();

            CollectionAssert.AreEqual(expected, set.ToArray(), $"set {ToUInt64(set):X}");
        }
    }

    /// <summary>
    /// Verifies that enumerating the empty set yields nothing.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenSetIsEmpty_ShouldYieldNothing()
    {
        using IEnumerator<TElement> enumerator = Empty.GetEnumerator();

        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the non-generic enumerator yields the same values as the generic one.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEnumeratedWithoutGenerics_ShouldYieldSameValues()
    {
        TSet set = SampleSets().Last();
        var values = new List<object>();

        IEnumerator enumerator = ((IEnumerable)set).GetEnumerator();
        while (enumerator.MoveNext())
            values.Add(enumerator.Current);

        CollectionAssert.AreEqual(set.Cast<object>().ToArray(), values.ToArray());
    }

    /// <summary>
    /// Verifies that <c>Reset</c> returns the enumerator to its start, so a second pass yields the same values.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenReset_ShouldRestartFromFirstValue()
    {
        TSet set = SampleSets().Last();
        using IEnumerator<TElement> enumerator = set.GetEnumerator();

        var first = new List<TElement>();
        while (enumerator.MoveNext())
            first.Add(enumerator.Current);

        enumerator.Reset();

        var second = new List<TElement>();
        while (enumerator.MoveNext())
            second.Add(enumerator.Current);

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that the type's public <c>GetEnumerator</c> returns a value-type enumerator, so <see langword="foreach" />
    /// over a set does not allocate.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenBoundByForeach_ShouldReturnStructEnumerator()
    {
        Type? returnType = typeof(TSet).GetMethod(nameof(IEnumerable.GetEnumerator), Type.EmptyTypes)?.ReturnType;

        Assert.IsNotNull(returnType);
        Assert.IsTrue(returnType.IsValueType, returnType.FullName);
        Assert.IsTrue(typeof(IEnumerator<TElement>).IsAssignableFrom(returnType), returnType.FullName);
    }
}
