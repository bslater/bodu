// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.Pad.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{
    /// <summary>
    /// Verifies that <c>PadLeft</c> on an empty source produces an array entirely filled with the pad value.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenSourceIsEmpty_ShouldReturnArrayOfPadValues()
    {
        int[] source = Array.Empty<int>();

        int[] result = source.PadLeft(4, 7);

        CollectionAssert.AreEqual(new[] { 7, 7, 7, 7 }, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> on an empty source produces an array entirely filled with the pad value.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenSourceIsEmpty_ShouldReturnArrayOfPadValues()
    {
        int[] source = Array.Empty<int>();

        int[] result = source.PadRight(4, 7);

        CollectionAssert.AreEqual(new[] { 7, 7, 7, 7 }, result);
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> with a pad value equal to <c>default(T)</c> produces a result whose
    /// leading positions are zero.
    /// </summary>
    [TestMethod]
    public void PadLeft_WhenPadValueIsDefault_ShouldZeroFillPrefix()
    {
        int[] source = { 1, 2, 3 };

        int[] result = source.PadLeft(6, 0);

        CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 2, 3 }, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> with a pad value equal to <c>default(T)</c> produces a result whose
    /// trailing positions are zero.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenPadValueIsDefault_ShouldZeroFillSuffix()
    {
        int[] source = { 1, 2, 3 };

        int[] result = source.PadRight(6, 0);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 0, 0, 0 }, result);
    }

    /// <summary>
    /// Verifies that <c>PadRight</c> on a reference-type array left-aligns the elements and fills the suffix
    /// with the pad reference.
    /// </summary>
    [TestMethod]
    public void PadRight_WhenArrayIsReferenceType_ShouldLeftAlignWithPadding()
    {
        string?[] source = { "a", "b" };

        string?[] result = source.PadRight(4, "x");

        CollectionAssert.AreEqual(new[] { "a", "b", "x", "x" }, result);
    }

    /// <summary>
    /// Verifies that <c>PadLeft</c> and <c>PadRight</c> on reference-type arrays correctly carry
    /// <see langword="null"/> as the pad value.
    /// </summary>
    [TestMethod]
    public void PadLeftAndRight_WhenPadValueIsNull_ShouldFillWithNullReferences()
    {
        string?[] source = { "a" };

        string?[] leftResult = source.PadLeft(3, null);
        string?[] rightResult = source.PadRight(3, null);

        CollectionAssert.AreEqual(new string?[] { null, null, "a" }, leftResult);
        CollectionAssert.AreEqual(new string?[] { "a", null, null }, rightResult);
    }
}
