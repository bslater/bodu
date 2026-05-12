// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.SetRelations.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{
    // --------------------------------------------------------
    // IsSubsetOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsSubsetOf(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenOtherIsNull_ShouldThrowArgumentNullException()
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

        Assert.IsTrue(sut.IsSubsetOf(Array.Empty<int>()));
        Assert.IsTrue(sut.IsSubsetOf(new[] { 1, 2, 3 }));
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.IsSubsetOf(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2 });

        Assert.IsTrue(sut.IsSubsetOf(new[] { 1, 2, 3 }));
        Assert.IsTrue(sut.IsSubsetOf(new[] { 1, 2 }));
        Assert.IsFalse(sut.IsSubsetOf(new[] { 1, 3 }));
        Assert.IsFalse(sut.IsSubsetOf(new[] { 1 }));
        Assert.IsFalse(sut.IsSubsetOf(Array.Empty<int>()));
    }

    // --------------------------------------------------------
    // IsSupersetOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsSupersetOf(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IsSupersetOf(null!);
        });
    }

    /// <summary>
    /// Verifies that every set is a superset of the empty collection.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenOtherIsEmpty_ShouldReturnTrue()
    {
        var empty = new OrderedSet<int>();
        OrderedSet<int> populated = CreateSet(new[] { 1, 2 });

        Assert.IsTrue(empty.IsSupersetOf(Array.Empty<int>()));
        Assert.IsTrue(populated.IsSupersetOf(Array.Empty<int>()));
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.IsSupersetOf(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        Assert.IsTrue(sut.IsSupersetOf(new[] { 1, 2 }));
        Assert.IsTrue(sut.IsSupersetOf(new[] { 1, 2, 3 }));
        Assert.IsFalse(sut.IsSupersetOf(new[] { 1, 4 }));
        Assert.IsFalse(sut.IsSupersetOf(new[] { 1, 2, 3, 4 }));
    }

    // --------------------------------------------------------
    // IsProperSubsetOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsProperSubsetOf(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IsProperSubsetOf(null!);
        });
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.IsProperSubsetOf(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2 });

        Assert.IsTrue(sut.IsProperSubsetOf(new[] { 1, 2, 3 }));
        Assert.IsFalse(sut.IsProperSubsetOf(new[] { 1, 2 }));
        Assert.IsFalse(sut.IsProperSubsetOf(new[] { 1 }));
    }

    /// <summary>
    /// Verifies that an empty set is not a proper subset of itself.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenBothEmpty_ShouldReturnFalse()
    {
        var sut = new OrderedSet<int>();

        Assert.IsFalse(sut.IsProperSubsetOf(Array.Empty<int>()));
    }

    // --------------------------------------------------------
    // IsProperSupersetOf
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsProperSupersetOf(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenOtherIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new OrderedSet<int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = sut.IsProperSupersetOf(null!);
        });
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.IsProperSupersetOf(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        Assert.IsTrue(sut.IsProperSupersetOf(new[] { 1, 2 }));
        Assert.IsFalse(sut.IsProperSupersetOf(new[] { 1, 2, 3 }));
        Assert.IsFalse(sut.IsProperSupersetOf(new[] { 1, 2, 3, 4 }));
    }

    /// <summary>
    /// Verifies that a populated set is a proper superset of the empty collection.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenOtherIsEmptyAndSetPopulated_ShouldReturnTrue()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1 });

        Assert.IsTrue(sut.IsProperSupersetOf(Array.Empty<int>()));
    }

    // --------------------------------------------------------
    // Overlaps
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.Overlaps(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenOtherIsNull_ShouldThrowArgumentNullException()
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

        Assert.IsFalse(sut.Overlaps(new[] { 1, 2 }));
        Assert.IsFalse(sut.Overlaps(Array.Empty<int>()));
    }

    /// <summary>
    /// Verifies the boundary outcomes for <see cref="OrderedSet{T}.Overlaps(System.Collections.Generic.IEnumerable{T})" />.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenComparisonsVary_ShouldReturnExpected()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        Assert.IsTrue(sut.Overlaps(new[] { 3, 4 }));
        Assert.IsTrue(sut.Overlaps(new[] { 1 }));
        Assert.IsFalse(sut.Overlaps(new[] { 4, 5 }));
        Assert.IsFalse(sut.Overlaps(Array.Empty<int>()));
    }

    // --------------------------------------------------------
    // SetEquals
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.SetEquals(System.Collections.Generic.IEnumerable{T})" /> rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenOtherIsNull_ShouldThrowArgumentNullException()
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
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        Assert.IsTrue(sut.SetEquals(new[] { 3, 2, 1 }));
        Assert.IsTrue(sut.SetEquals(new[] { 1, 2, 3, 1, 2 }));
    }

    /// <summary>
    /// Verifies that differing collections are reported as not equal.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenContentsDiffer_ShouldReturnFalse()
    {
        OrderedSet<int> sut = CreateSet(new[] { 1, 2, 3 });

        Assert.IsFalse(sut.SetEquals(new[] { 1, 2 }));
        Assert.IsFalse(sut.SetEquals(new[] { 1, 2, 3, 4 }));
        Assert.IsFalse(sut.SetEquals(new[] { 4, 5, 6 }));
    }

    /// <summary>
    /// Verifies that two empty collections are reported as set-equal.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenBothEmpty_ShouldReturnTrue()
    {
        var sut = new OrderedSet<int>();

        Assert.IsTrue(sut.SetEquals(Array.Empty<int>()));
    }
}
