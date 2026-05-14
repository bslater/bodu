// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.Pad.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{
    /// <summary>
    /// Verifies that <c>PadLeft</c> right-aligns the source within the result and fills the prefix with the pad value.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenTotalLengthExceedsSource_ShouldRightAlignWithPadding()
    {
        int[] source = { 1, 2, 3 };

        int[] result = source.PadLeft(6, 9);

        CollectionAssert.AreEqual(new[] { 9, 9, 9, 1, 2, 3 }, result);
        AssertIsNewAllocation(source, result);
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> returns a fresh copy when <c>totalLength</c> equals the source length.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenTotalLengthEqualsSource_ShouldReturnFreshCopyWithNoPadding()
    {
        int[] source = { 1, 2, 3 };

        int[] result = source.PadLeft(3, 9);

        CollectionAssert.AreEqual(source, result);
        AssertIsNewAllocation(source, result);
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> throws <see cref="ArgumentNullException"/> when the source is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        int[]? array = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = array!.PadLeft(5, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <paramref name="totalLength"/> is smaller than the source.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenTotalLengthIsLessThanSourceLength_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.PadLeft(2, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> left-aligns the source within the result and fills the suffix with the pad value.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenTotalLengthExceedsSource_ShouldLeftAlignWithPadding()
    {
        int[] source = { 1, 2, 3 };

        int[] result = source.PadRight(6, 9);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 9, 9, 9 }, result);
        AssertIsNewAllocation(source, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> returns a fresh copy when <c>totalLength</c> equals the source length.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenTotalLengthEqualsSource_ShouldReturnFreshCopyWithNoPadding()
    {
        int[] source = { 1, 2, 3 };

        int[] result = source.PadRight(3, 9);

        CollectionAssert.AreEqual(source, result);
        AssertIsNewAllocation(source, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> throws <see cref="ArgumentNullException"/> when the source is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        int[]? array = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = array!.PadRight(5, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <paramref name="totalLength"/> is smaller than the source.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenTotalLengthIsLessThanSourceLength_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.PadRight(2, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> works on reference-type arrays.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenArrayIsReferenceType_ShouldRightAlignWithPadding()
    {
        string?[] source = { "a", "b" };

        string?[] result = source.PadLeft(4, "x");

        CollectionAssert.AreEqual(new[] { "x", "x", "a", "b" }, result);
    }
}
