// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderedSetStorageTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class OrderedSetStorageTests
{
    // --------------------------------------------------------
    // Constructor — argument validation
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
            _ = new OrderedSetStorage<int>(capacity, null);
        });
    }

    /// <summary>
    /// Verifies that a positive capacity is preserved on the underlying element array.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(16)]
    [DataRow(1024)]
    public void Ctor_WhenCapacityIsPositive_ShouldAllocateRequestedCapacity(int capacity)
    {
        var sut = new OrderedSetStorage<int>(capacity, null);

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(capacity, sut.Capacity);
    }

    // --------------------------------------------------------
    // Constructor — capacity handling
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a zero capacity initialises empty backing arrays.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsZero_ShouldHaveZeroCapacity()
    {
        var sut = new OrderedSetStorage<int>(0, null);

        Assert.AreEqual(0, sut.Count);
        Assert.AreEqual(0, sut.Capacity);
    }

    // --------------------------------------------------------
    // Constructor — comparer handling
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer is replaced with the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var sut = new OrderedSetStorage<string>(0, null);

        Assert.AreSame(EqualityComparer<string>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that the provided comparer is used to determine element equality during subsequent operations.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldGovernEquality()
    {
        var sut = new OrderedSetStorage<string>(0, StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(sut.Add("HELLO"));
        Assert.IsFalse(sut.Add("hello"));
        Assert.AreEqual(1, sut.Count);
        Assert.IsTrue(sut.Contains("Hello"));
    }

    /// <summary>
    /// Verifies that a non-<see langword="null" /> comparer is stored verbatim.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseSpecifiedComparer()
    {
        IEqualityComparer<string> comparer = StringComparer.OrdinalIgnoreCase;

        var sut = new OrderedSetStorage<string>(0, comparer);

        Assert.AreSame(comparer, sut.Comparer);
    }

}
