// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[])" /> resets every element of a reference-type array to <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clear_Generic_WhenArrayContainsReferenceTypes_ShouldResetAllElementsToNull()
    {
        string?[] array = ["a", "b", "c"];
        array.Clear();
        CollectionAssert.AreEqual(new string?[] { null, null, null }, array);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[])" /> succeeds on an empty array (no-op).
    /// </summary>
    [TestMethod]
    public void Clear_Generic_WhenArrayIsEmpty_ShouldBeNoOp()
    {
        var array = Array.Empty<int>();
        array.Clear();
        Assert.IsEmpty(array);
    }
    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[])" /> resets every element of an integer array to its default value.
    /// </summary>
    [TestMethod]
    public void Clear_Generic_WhenArrayIsNonEmpty_ShouldResetAllElements()
    {
        int[] array = [1, 2, 3, 4, 5];
        array.Clear();
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0 }, array);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[])" /> throws <see cref="ArgumentNullException" /> when the array is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clear_Generic_WhenArrayIsNull_ShouldThrowExactly()
    {
        int[]? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            array!.Clear();
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int)" /> throws <see cref="ArgumentNullException" /> when the array is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndex_WhenArrayIsNull_ShouldThrowExactly()
    {
        int[]? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            array!.Clear(0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int)" /> throws <see cref="ArgumentOutOfRangeException" /> when the index
    /// is equal to or greater than the array length.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndex_WhenIndexIsBeyondArray_ShouldThrowExactly()
    {
        int[] array = [1, 2, 3];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Clear(3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int)" /> clears elements from the supplied index to the end of the array.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndex_WhenIndexIsInRange_ShouldClearTrailingRange()
    {
        int[] array = [1, 2, 3, 4, 5];
        array.Clear(2);
        CollectionAssert.AreEqual(new[] { 1, 2, 0, 0, 0 }, array);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int)" /> throws <see cref="ArgumentOutOfRangeException" /> when the index
    /// is negative.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndex_WhenIndexIsNegative_ShouldThrowExactly()
    {
        int[] array = [1, 2, 3];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Clear(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int)" /> with index 0 clears the entire array.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndex_WhenIndexIsZero_ShouldClearAllElements()
    {
        int[] array = [1, 2, 3];
        array.Clear(0);
        CollectionAssert.AreEqual(new[] { 0, 0, 0 }, array);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int, int)" /> throws <see cref="ArgumentNullException" /> when the array is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndexAndCount_WhenArrayIsNull_ShouldThrowExactly()
    {
        int[]? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            array!.Clear(0, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int, int)" /> throws <see cref="ArgumentOutOfRangeException" /> when
    /// count is negative.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndexAndCount_WhenCountIsNegative_ShouldThrowExactly()
    {
        int[] array = [1, 2, 3];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Clear(0, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int, int)" /> with count zero leaves the array unchanged.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndexAndCount_WhenCountIsZero_ShouldLeaveArrayUnchanged()
    {
        int[] array = [1, 2, 3];
        array.Clear(1, 0);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, array);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int, int)" /> throws <see cref="ArgumentOutOfRangeException" /> when the
    /// index is negative.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndexAndCount_WhenIndexIsNegative_ShouldThrowExactly()
    {
        int[] array = [1, 2, 3];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Clear(-1, 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int, int)" /> throws <see cref="ArgumentException" /> when
    /// <c>index + count</c> exceeds the array length.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndexAndCount_WhenIndexPlusCountExceedsLength_ShouldThrowExactly()
    {
        int[] array = [1, 2, 3, 4, 5];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            array.Clear(3, 5);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.Clear{T}(T[], int, int)" /> clears exactly <c>count</c> elements starting at
    /// <c>index</c>.
    /// </summary>
    [TestMethod]
    public void Clear_GenericWithIndexAndCount_WhenRangeIsValid_ShouldClearSpecifiedSlice()
    {
        int[] array = [1, 2, 3, 4, 5];
        array.Clear(1, 3);
        CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 5 }, array);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array)" /> throws <see cref="ArgumentException" /> when the
    /// array is multidimensional.
    /// </summary>
    [TestMethod]
    public void Clear_NonGeneric_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        Array array = new int[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            array.Clear();
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array)" /> resets every element of a single-dimensional
    /// integer array.
    /// </summary>
    [TestMethod]
    public void Clear_NonGeneric_WhenArrayIsNonEmpty_ShouldResetAllElements()
    {
        Array array = new[] { 7, 8, 9 };
        array.Clear();
        CollectionAssert.AreEqual(new[] { 0, 0, 0 }, (int[])array);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array)" /> throws <see cref="ArgumentException" /> when the
    /// array has a non-zero lower bound.
    /// </summary>
    [TestMethod]
    public void Clear_NonGeneric_WhenArrayIsNotZeroBased_ShouldThrowExactly()
    {
        var array = Array.CreateInstance(typeof(int), [3], [5]);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            array.Clear();
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array)" /> throws <see cref="ArgumentNullException" /> when the
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clear_NonGeneric_WhenArrayIsNull_ShouldThrowExactly()
    {
        Array? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            array!.Clear();
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int)" /> throws <see cref="ArgumentException" /> when the
    /// array is multidimensional.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndex_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        Array array = new int[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            array.Clear(0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int)" /> throws <see cref="ArgumentNullException" /> when
    /// the array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndex_WhenArrayIsNull_ShouldThrowExactly()
    {
        Array? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            array!.Clear(0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int)" /> clears elements from the supplied index to the
    /// end of the array.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndex_WhenIndexIsInRange_ShouldClearTrailingRange()
    {
        Array array = new[] { 1, 2, 3, 4, 5 };
        array.Clear(2);
        CollectionAssert.AreEqual(new[] { 1, 2, 0, 0, 0 }, (int[])array);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int)" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the index is outside the addressable range.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndex_WhenIndexIsNegative_ShouldThrowExactly()
    {
        Array array = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Clear(-1);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int, int)" /> throws <see cref="ArgumentException" /> when
    /// the array is multidimensional.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndexAndCount_WhenArrayIsMultidimensional_ShouldThrowExactly()
    {
        Array array = new int[2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            array.Clear(0, 0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int, int)" /> throws <see cref="ArgumentNullException" />
    /// when the array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndexAndCount_WhenArrayIsNull_ShouldThrowExactly()
    {
        Array? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            array!.Clear(0, 0);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int, int)" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when the count is negative.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndexAndCount_WhenCountIsNegative_ShouldThrowExactly()
    {
        Array array = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            array.Clear(0, -1);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int, int)" /> throws <see cref="ArgumentException" /> when
    /// <c>index + count</c> exceeds the array length.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndexAndCount_WhenIndexPlusCountExceedsLength_ShouldThrowExactly()
    {
        Array array = new[] { 1, 2, 3, 4, 5 };
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            array.Clear(3, 5);
        });
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="ArrayExtensions.Clear(Array, int, int)" /> clears exactly <c>count</c> elements starting
    /// at <c>index</c>.
    /// </summary>
    [TestMethod]
    public void Clear_NonGenericWithIndexAndCount_WhenRangeIsValid_ShouldClearSpecifiedSlice()
    {
        Array array = new[] { 1, 2, 3, 4, 5 };
        array.Clear(1, 3);
        CollectionAssert.AreEqual(new[] { 1, 0, 0, 0, 5 }, (int[])array);
    }

}
