// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Contains.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Contains" /> returns <see langword="true" /> for every stored
    /// element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsPresent_ShouldReturnTrue()
    {
        var sut = CreateSet(4, 2, 6, 1, 3, 5, 7);

        foreach (int item in new[] { 1, 2, 3, 4, 5, 6, 7 })
            Assert.IsTrue(sut.Contains(item), $"Contains({item}) should be true.");
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Contains" /> returns <see langword="false" /> for absent elements,
    /// including values between, below, and above the stored elements.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsAbsent_ShouldReturnFalse()
    {
        var sut = CreateSet(10, 20, 30);

        Assert.IsFalse(sut.Contains(5));
        Assert.IsFalse(sut.Contains(15));
        Assert.IsFalse(sut.Contains(35));
    }

    /// <summary>
    /// Verifies that <see cref="NavigableSet{T}.Contains" /> returns <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void Contains_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var sut = new NavigableSet<int>();

        Assert.IsFalse(sut.Contains(1));
    }

    /// <summary>
    /// Verifies that membership follows the active comparer rather than default equality.
    /// </summary>
    [TestMethod]
    public void Contains_WhenComparerEqualElementStored_ShouldReturnTrue()
    {
        var sut = new NavigableSet<string>(StringComparer.OrdinalIgnoreCase);
        sut.Add("Alpha");

        Assert.IsTrue(sut.Contains("ALPHA"));
    }

    /// <summary>
    /// Verifies that querying a <see langword="null" /> item throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut.Contains(null!);
        }, "item");
    }
}
