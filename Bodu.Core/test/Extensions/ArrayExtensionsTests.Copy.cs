// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.Copy.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{

    /// <summary>
    /// Verifies that copying a value-type array of a custom struct produces an independent array preserving element equality.
    /// </summary>
    [TestMethod]
    public void Copy_WhenCalled_ForCustomStructArray_ShouldReturnIndependentEqualArray()
    {
        DateOnly[] source =
        [
            new DateOnly(2024, 1, 1),
            new DateOnly(2024, 6, 15),
            new DateOnly(2024, 12, 31),
        ];

        DateOnly[] result = source.Copy()!;

        CollectionAssert.AreEqual(source, result);
        Assert.IsFalse(ReferenceEquals(source, result));
    }
    /// <summary>
    /// Verifies that copying a value-type array returns an array with the same elements in the same order.
    /// </summary>
    [TestMethod]
    public void Copy_WhenCalled_ForValueTypeArray_ShouldReturnArrayWithSameElements()
    {
        int[] source = [1, 2, 3, 4, 5];

        var result = source.Copy()!;

        CollectionAssert.AreEqual(source, result);
    }

    /// <summary>
    /// Verifies that copying a value-type array returns a new heap allocation, not the original instance.
    /// </summary>
    [TestMethod]
    public void Copy_WhenCalled_ForValueTypeArray_ShouldReturnNewAllocation()
    {
        int[] source = [1, 2, 3];

        var result = source.Copy()!;

        Assert.IsFalse(ReferenceEquals(source, result));
    }

    /// <summary>
    /// Verifies that mutating the returned array does not alter the original source array.
    /// </summary>
    [TestMethod]
    public void Copy_WhenMutated_ForValueTypeArray_ShouldNotAffectSource()
    {
        int[] source = [10, 20, 30];

        var result = source.Copy()!;
        result[0] = 99;

        Assert.AreEqual(10, source[0]);
    }

    /// <summary>
    /// Verifies that copying an empty value-type array returns a new empty array.
    /// </summary>
    [TestMethod]
    public void Copy_WhenSourceIsEmpty_ForValueTypeArray_ShouldReturnEmptyArray()
    {
        int[] source = [];

        var result = source.Copy()!;

        Assert.IsEmpty(result);
        Assert.IsFalse(ReferenceEquals(source, result));
    }

    /// <summary>
    /// Verifies that copying a null value-type array returns <see langword="null" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void Copy_WhenSourceIsNull_ForValueTypeArray_ShouldReturnNull()
    {
        int[]? source = null;

        var result = source!.Copy();

        Assert.IsNull(result);
    }

}
