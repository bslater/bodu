// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.ValueType.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that a set of a value-type element supports the full add / remove / contains / count surface.
    /// </summary>
    [TestMethod]
    public void ValueType_WhenElementTypeIsInt_ShouldSupportCoreOperations()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsTrue(set.Add(1));
        Assert.IsTrue(set.Add(2));
        Assert.IsFalse(set.Add(1));
        Assert.IsTrue(set.Contains(2));
        Assert.IsTrue(set.Remove(1));
        Assert.AreEqual(1, set.Count);
        CollectionAssert.AreEquivalent(new[] { 2 }, set.ToArray());
    }

    /// <summary>
    /// Verifies that the default value of a value type is a valid, storable element.
    /// </summary>
    [TestMethod]
    public void ValueType_WhenElementIsDefaultValue_ShouldBeStoredAsAnElement()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsTrue(set.Add(0));
        Assert.IsTrue(set.Contains(0));
        Assert.AreEqual(1, set.Count);
    }

    /// <summary>
    /// Verifies that a set of a struct element type honors structural value equality.
    /// </summary>
    [TestMethod]
    public void ValueType_WhenElementTypeIsStruct_ShouldUseStructuralEquality()
    {
        var set = new ConcurrentHashSet<(int X, int Y)>();

        Assert.IsTrue(set.Add((1, 2)));
        Assert.IsFalse(set.Add((1, 2)));
        Assert.IsTrue(set.Contains((1, 2)));
        Assert.IsFalse(set.Contains((2, 1)));
    }

    /// <summary>
    /// Verifies that a value-type set supports the bulk <see cref="ISet{T}" /> operations.
    /// </summary>
    [TestMethod]
    public void ValueType_WhenElementTypeIsInt_ShouldSupportSetAlgebra()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.UnionWith([3, 4]);
        AssertContainsExactly(set, 1, 2, 3, 4);

        set.IntersectWith([2, 3, 4, 5]);
        AssertContainsExactly(set, 2, 3, 4);

        Assert.IsTrue(set.IsSubsetOf([1, 2, 3, 4, 5]));
    }
}
