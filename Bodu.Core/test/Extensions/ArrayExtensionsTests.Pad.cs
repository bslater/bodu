// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.Pad.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{

    /// <summary>
    /// Verifies that <c>PadLeft</c> throws <see cref="ArgumentNullException"/> when the source is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenArrayIsNull_ShouldThrowExactly()
    {
        int[]? array = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = array!.PadLeft(5, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> works on reference-type arrays.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenArrayIsReferenceType_ShouldRightAlignWithPadding()
    {
        string?[] source = ["a", "b"];

        var result = source.PadLeft(4, "x");

        CollectionAssert.AreEqual(new[] { "x", "x", "a", "b" }, result);
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> with a pad value equal to <c>default(T)</c> produces a result whose
    /// leading positions are zero.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenPadValueIsDefault_ShouldZeroFillPrefix()
    {
        int[] source = [1, 2, 3];

        var result = source.PadLeft(6, 0);

        CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 2, 3 }, result);
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> on an empty source produces an array entirely filled with the pad value.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenSourceIsEmpty_ShouldReturnArrayOfPadValues()
    {
        var source = Array.Empty<int>();

        var result = source.PadLeft(4, 7);

        CollectionAssert.AreEqual(new[] { 7, 7, 7, 7 }, result);
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> returns a fresh copy when <c>totalLength</c> equals the source length.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenTotalLengthEqualsSource_ShouldReturnFreshCopyWithNoPadding()
    {
        int[] source = [1, 2, 3];

        var result = source.PadLeft(3, 9);

        CollectionAssert.AreEqual(source, result);
        AssertIsNewAllocation(source, result);
    }
    /// <summary>
    /// Verifies that <c>PadLeft</c> right-aligns the source within the result and fills the prefix with the pad value.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenTotalLengthExceedsSource_ShouldRightAlignWithPadding()
    {
        int[] source = [1, 2, 3];

        var result = source.PadLeft(6, 9);

        CollectionAssert.AreEqual(new[] { 9, 9, 9, 1, 2, 3 }, result);
        AssertIsNewAllocation(source, result);
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <paramref name="totalLength"/> is smaller than the source.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenTotalLengthIsLessThanSourceLength_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.PadLeft(2, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> and <c>PadRight</c> on reference-type arrays correctly carry
    /// <see langword="null"/> as the pad value.
    /// </summary>
    [TestMethod]
    public void PadLeftAndRight_WhenPadValueIsNull_ShouldFillWithNullReferences()
    {
        string?[] source = ["a"];

        var leftResult = source.PadLeft(3, null);
        var rightResult = source.PadRight(3, null);

        CollectionAssert.AreEqual(new string?[] { null, null, "a" }, leftResult);
        CollectionAssert.AreEqual(new string?[] { "a", null, null }, rightResult);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> throws <see cref="ArgumentNullException"/> when the source is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenArrayIsNull_ShouldThrowExactly()
    {
        int[]? array = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = array!.PadRight(5, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> on a reference-type array left-aligns the elements and fills the suffix
    /// with the pad reference.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenArrayIsReferenceType_ShouldLeftAlignWithPadding()
    {
        string?[] source = ["a", "b"];

        var result = source.PadRight(4, "x");

        CollectionAssert.AreEqual(new[] { "a", "b", "x", "x" }, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> with a pad value equal to <c>default(T)</c> produces a result whose
    /// trailing positions are zero.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenPadValueIsDefault_ShouldZeroFillSuffix()
    {
        int[] source = [1, 2, 3];

        var result = source.PadRight(6, 0);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 0, 0, 0 }, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> on an empty source produces an array entirely filled with the pad value.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenSourceIsEmpty_ShouldReturnArrayOfPadValues()
    {
        var source = Array.Empty<int>();

        var result = source.PadRight(4, 7);

        CollectionAssert.AreEqual(new[] { 7, 7, 7, 7 }, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> returns a fresh copy when <c>totalLength</c> equals the source length.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenTotalLengthEqualsSource_ShouldReturnFreshCopyWithNoPadding()
    {
        int[] source = [1, 2, 3];

        var result = source.PadRight(3, 9);

        CollectionAssert.AreEqual(source, result);
        AssertIsNewAllocation(source, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> left-aligns the source within the result and fills the suffix with the pad value.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenTotalLengthExceedsSource_ShouldLeftAlignWithPadding()
    {
        int[] source = [1, 2, 3];

        var result = source.PadRight(6, 9);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 9, 9, 9 }, result);
        AssertIsNewAllocation(source, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <paramref name="totalLength"/> is smaller than the source.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenTotalLengthIsLessThanSourceLength_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.PadRight(2, 0);
        });
    }

}
