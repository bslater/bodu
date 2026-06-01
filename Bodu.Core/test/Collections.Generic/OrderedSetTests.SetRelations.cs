// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.SetRelations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{

    /// <summary>
    /// Verifies that an empty set is not a proper subset of itself.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenBothEmpty_ShouldReturnFalse()
    {
        var sut = new OrderedSet<int>();

        Assert.IsFalse(sut.IsProperSubsetOf([]));
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.IsProperSubsetOf(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet([1, 2]);

        Assert.IsTrue(sut.IsProperSubsetOf([1, 2, 3]));
        Assert.IsFalse(sut.IsProperSubsetOf([1, 2]));
        Assert.IsFalse(sut.IsProperSubsetOf([1]));
    }

    // --------------------------------------------------------
    // IsProperSubsetOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsProperSubsetOf(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IsProperSubsetOf(null!);
        });
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.IsProperSupersetOf(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.IsTrue(sut.IsProperSupersetOf([1, 2]));
        Assert.IsFalse(sut.IsProperSupersetOf([1, 2, 3]));
        Assert.IsFalse(sut.IsProperSupersetOf([1, 2, 3, 4]));
    }

    /// <summary>
    /// Verifies that a populated set is a proper superset of the empty collection.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenOtherIsEmptyAndSetPopulated_ShouldReturnTrue()
    {
        OrderedSet<int> sut = CreateSet([1]);

        Assert.IsTrue(sut.IsProperSupersetOf([]));
    }

    // --------------------------------------------------------
    // IsProperSupersetOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsProperSupersetOf(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IsProperSupersetOf(null!);
        });
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.IsSubsetOf(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet([1, 2]);

        Assert.IsTrue(sut.IsSubsetOf([1, 2, 3]));
        Assert.IsTrue(sut.IsSubsetOf([1, 2]));
        Assert.IsFalse(sut.IsSubsetOf([1, 3]));
        Assert.IsFalse(sut.IsSubsetOf([1]));
        Assert.IsFalse(sut.IsSubsetOf([]));
    }
    // --------------------------------------------------------
    // IsSubsetOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsSubsetOf(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IsSubsetOf(null!);
        });
    }

    /// <summary>
    /// Verifies that an empty set is a subset of every collection, including the empty collection.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenSetIsEmpty_ShouldReturnTrue()
    {
        var sut = new OrderedSet<int>();

        Assert.IsTrue(sut.IsSubsetOf([]));
        Assert.IsTrue(sut.IsSubsetOf([1, 2, 3]));
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.IsSupersetOf(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.IsTrue(sut.IsSupersetOf([1, 2]));
        Assert.IsTrue(sut.IsSupersetOf([1, 2, 3]));
        Assert.IsFalse(sut.IsSupersetOf([1, 4]));
        Assert.IsFalse(sut.IsSupersetOf([1, 2, 3, 4]));
    }

    /// <summary>
    /// Verifies that every set is a superset of the empty collection.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenOtherIsEmpty_ShouldReturnTrue()
    {
        var empty = new OrderedSet<int>();
        OrderedSet<int> populated = CreateSet([1, 2]);

        Assert.IsTrue(empty.IsSupersetOf([]));
        Assert.IsTrue(populated.IsSupersetOf([]));
    }

    // --------------------------------------------------------
    // IsSupersetOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsSupersetOf(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IsSupersetOf(null!);
        });
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.Overlaps(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.IsTrue(sut.Overlaps([3, 4]));
        Assert.IsTrue(sut.Overlaps([1]));
        Assert.IsFalse(sut.Overlaps([4, 5]));
        Assert.IsFalse(sut.Overlaps([]));
    }

    // --------------------------------------------------------
    // Overlaps
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Overlaps(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.Overlaps(null!);
        });
    }

    /// <summary>
    /// Verifies that an empty set never overlaps any collection.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new OrderedSet<int>();

        Assert.IsFalse(sut.Overlaps([1, 2]));
        Assert.IsFalse(sut.Overlaps([]));
    }

    /// <summary>
    /// Verifies that two empty collections are reported as set-equal.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenBothEmpty_ShouldReturnTrue()
    {
        var sut = new OrderedSet<int>();

        Assert.IsTrue(sut.SetEquals([]));
    }

    /// <summary>
    /// Verifies that differing collections are reported as not equal.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenContentsDiffer_ShouldReturnFalse()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.IsFalse(sut.SetEquals([1, 2]));
        Assert.IsFalse(sut.SetEquals([1, 2, 3, 4]));
        Assert.IsFalse(sut.SetEquals([4, 5, 6]));
    }

    // --------------------------------------------------------
    // SetEquals
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.SetEquals(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenOtherIsNull_ShouldThrowExactly()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.SetEquals(null!);
        });
    }

    /// <summary>
    /// Verifies that two collections containing the same elements are reported as equal, regardless of order
    /// or duplicates in the source.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenSameElementsInDifferentOrder_ShouldReturnTrue()
    {
        OrderedSet<int> sut = CreateSet([1, 2, 3]);

        Assert.IsTrue(sut.SetEquals([3, 2, 1]));
        Assert.IsTrue(sut.SetEquals([1, 2, 3, 1, 2]));
    }

}
