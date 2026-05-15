// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.Slice.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{

    /// <summary>
    /// Verifies that a negative index or count throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 1, DisplayName = "Negative index")]
    [DataRow(0, -1, DisplayName = "Negative count")]
    [DataRow(10, 0, DisplayName = "Index exceeds array length")]
    [DataRow(0, 10, DisplayName = "Count exceeds array length")]
    public void Slice_WhenArgumentsAreInvalid_ForTypedArrayIndexCount_ShouldThrowArgumentOutOfRangeException(int index, int count)
    {
        int[] source = [1, 2, 3, 4, 5];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Slice(index, count);
        });
    }

    /// <summary>
    /// Verifies that slicing a reference-type array produces the expected segment with shared element references.
    /// </summary>
    [TestMethod]
    public void Slice_WhenCalled_ForReferenceTypeArray_ShouldReturnSegmentWithSharedElementReferences()
    {
        var a = new object();
        var b = new object();
        var c = new object();
        object[] source = [a, b, c];

        var result = source.Slice(1, 2);

        Assert.AreSame(b, result[0]);
        Assert.AreSame(c, result[1]);
    }

    /// <summary>
    /// Verifies that the index-only overload returns a new allocation independent of the source.
    /// </summary>
    [TestMethod]
    public void Slice_WhenCalled_ForTypedArrayIndexOnly_ShouldReturnNewAllocation()
    {
        int[] source = [1, 2, 3, 4, 5];

        var result = source.Slice(0);

        Assert.IsFalse(ReferenceEquals(source, result));
    }

    /// <summary>
    /// Verifies that an index beyond the array length throws <see cref="ArgumentOutOfRangeException" /> from the index-only overload.
    /// </summary>
    [TestMethod]
    public void Slice_WhenIndexExceedsLength_ForTypedArrayIndexOnly_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Slice(4);
        });
    }

    /// <summary>
    /// Verifies that a negative index throws <see cref="ArgumentOutOfRangeException" /> from the index-only overload.
    /// </summary>
    [TestMethod]
    public void Slice_WhenIndexIsNegative_ForTypedArrayIndexOnly_ShouldThrowArgumentOutOfRangeException()
    {
        int[] source = [1, 2, 3];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Slice(-1);
        });
    }

    /// <summary>
    /// Verifies that an index plus count that extends past the array end throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Slice_WhenIndexPlusCountExceedsLength_ForTypedArrayIndexCount_ShouldThrowArgumentException()
    {
        int[] source = [1, 2, 3, 4, 5];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = source.Slice(3, 3);
        });
    }

    /// <summary>
    /// Verifies that a slice is an independent allocation — mutating it leaves the original source unchanged.
    /// </summary>
    [TestMethod]
    public void Slice_WhenMutated_ForTypedArrayIndexCount_ShouldNotAffectSource()
    {
        int[] source = [1, 2, 3, 4, 5];

        var result = source.Slice(1, 3);
        result[0] = 99;

        Assert.AreEqual(2, source[1]);
    }

    /// <summary>
    /// Verifies that slicing returns an array whose length equals the requested count.
    /// </summary>
    [TestMethod]
    public void Slice_WhenRangeIsValid_ForTypedArrayIndexCount_ShouldReturnArrayOfRequestedLength()
    {
        int[] source = [1, 2, 3, 4, 5];

        var result = source.Slice(1, 3);

        Assert.AreEqual(3, result.Length);
    }

    // =========================================================================
    // Slice<T>(T[], int, int)
    // =========================================================================

    /// <summary>
    /// Verifies that slicing with a valid index and count returns the nominated segment.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0, new int[0])]
    [DataRow(0, 5, new[] { 1, 2, 3, 4, 5 })]
    [DataRow(1, 3, new[] { 2, 3, 4 })]
    [DataRow(2, 2, new[] { 3, 4 })]
    [DataRow(4, 1, new[] { 5 })]
    [DataRow(5, 0, new int[0])]
    public void Slice_WhenRangeIsValid_ForTypedArrayIndexCount_ShouldReturnExpectedElements(int index, int count, int[] expected)
    {
        int[] source = [1, 2, 3, 4, 5];

        var result = source.Slice(index, count);

        CollectionAssert.AreEqual(expected, result);
    }

    /// <summary>
    /// Verifies that a null array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Slice_WhenSourceIsNull_ForTypedArrayIndexCount_ShouldThrowArgumentNullException()
    {
        int[]? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.Slice(0, 1);
        });
    }

    /// <summary>
    /// Verifies that slicing from a null array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Slice_WhenSourceIsNull_ForTypedArrayIndexOnly_ShouldThrowArgumentNullException()
    {
        int[]? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.Slice(0);
        });
    }
    // =========================================================================
    // Slice<T>(T[], int)
    // =========================================================================

    /// <summary>
    /// Verifies that slicing from a valid index to the end returns the expected trailing elements.
    /// </summary>
    [TestMethod]
    [DataRow(0, new[] { 1, 2, 3, 4, 5 })]
    [DataRow(2, new[] { 3, 4, 5 })]
    [DataRow(4, new[] { 5 })]
    [DataRow(5, new int[0])]
    public void Slice_WhenStartingFromIndex_ForTypedArray_ShouldReturnTrailingElements(int index, int[] expected)
    {
        int[] source = [1, 2, 3, 4, 5];

        var result = source.Slice(index);

        CollectionAssert.AreEqual(expected, result);
    }

}
