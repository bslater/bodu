// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetTests.Ctor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetTests
{

    // --------------------------------------------------------
    // Capacity + comparer constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the capacity-and-comparer constructor combines both arguments correctly.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityAndComparerProvided_ShouldUseBoth()
    {
        var sut = new OrderedSet<string>(8, StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(8, sut.Capacity);
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, sut.Comparer);
    }

    // --------------------------------------------------------
    // Capacity constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a negative capacity is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-100)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenCapacityIsNegative_ShouldThrowExactly(int capacity)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new OrderedSet<int>(capacity);
        });
    }

    /// <summary>
    /// Verifies that the capacity constructor accepts non-negative values and pre-allocates the requested
    /// capacity.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(16)]
    [DataRow(1024)]
    public void Ctor_WhenCapacityIsValid_ShouldAllocateRequestedCapacity(int capacity)
    {
        var sut = new OrderedSet<int>(capacity);

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(capacity, sut.Capacity);
    }

    /// <summary>
    /// Verifies that the collection-and-comparer constructor uses the supplied comparer for deduplication.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionAndComparerProvided_ShouldUseSpecifiedComparer()
    {
        var sut = new OrderedSet<string>(
            ["A", "a", "B", "b"],
            StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual("A", sut[0]);
        Assert.AreEqual("B", sut[1]);
    }

    /// <summary>
    /// Verifies that the collection constructor preserves insertion order while deduplicating.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionContainsDuplicates_ShouldPreserveFirstOccurrenceOrder()
    {
        var sut = new OrderedSet<int>([1, 2, 2, 3, 1, 4]);

        Assert.AreEqual(4, sut.Count);
        Assert.AreEqual(1, sut[0]);
        Assert.AreEqual(2, sut[1]);
        Assert.AreEqual(3, sut[2]);
        Assert.AreEqual(4, sut[3]);
    }

    /// <summary>
    /// Verifies that the collection constructor rejects a source that yields a <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionContainsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new OrderedSet<string>(["a", null!, "b"]);
        });
    }

    /// <summary>
    /// Verifies that the collection-and-comparer constructor preallocates capacity from an
    /// <see cref="ICollection{T}" /> hint.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionHasCountHint_ShouldPreallocateCapacity()
    {
        var source = new List<int> { 1, 2, 3, 4, 5 };

        var sut = new OrderedSet<int>(source);

        Assert.AreEqual(5, sut.Count);
        Assert.IsGreaterThanOrEqualTo(5, sut.Capacity);
    }

    /// <summary>
    /// Verifies that an empty source collection produces an empty set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionIsEmpty_ShouldBeEmpty()
    {
        var sut = new OrderedSet<int>(Array.Empty<int>());

        Assert.AreEqual(0, sut.Count);
    }

    // --------------------------------------------------------
    // Collection constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the collection constructor rejects a <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCollectionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new OrderedSet<int>((IEnumerable<int>)null!);
        });
    }

    // --------------------------------------------------------
    // Comparer-only constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the comparer-only constructor defaults to the default comparer when supplied
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var sut = new OrderedSet<string>((IEqualityComparer<string>?)null);

        Assert.AreSame(EqualityComparer<string>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that the comparer-only constructor stores and uses the supplied comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseSpecifiedComparer()
    {
        var sut = new OrderedSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, sut.Comparer);

        sut.Add("alpha");
        Assert.IsTrue(sut.Contains("ALPHA"));
    }
    // --------------------------------------------------------
    // Default constructor
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the default constructor creates an empty set with the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldBeEmptyWithDefaultComparer()
    {
        var sut = new OrderedSet<int>();

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.Capacity);
        Assert.AreSame(EqualityComparer<int>.Default, sut.Comparer);
    }

    // --------------------------------------------------------
    // IsReadOnly
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="OrderedSet{T}.IsReadOnly" /> is always <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsReadOnly_WhenInspected_ShouldReturnFalse()
    {
        var sut = new OrderedSet<int>();

        Assert.IsFalse(sut.IsReadOnly);
    }

}
