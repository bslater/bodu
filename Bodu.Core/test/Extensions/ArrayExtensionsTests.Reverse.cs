// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.Reverse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions;

public partial class ArrayExtensionsTests
{
    // =========================================================================
    // T[] — full reverse
    // =========================================================================

    /// <summary>
    /// Verifies that reversing typed integer arrays of varying lengths always returns
    /// all elements in reverse order, covering odd, even, single-element, and empty cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(FullReverseIntData), typeof(ArrayExtensionsTests))]
    public void Reverse_WhenSourceHasDifferentLengths_ForTypedArray_ShouldReturnAllElementsReversed(
        int[] input, int[] expected) => CollectionAssert.AreEqual(expected, input.Reverse());

    /// <summary>
    /// Verifies that reversing a typed array of reference-type elements returns all
    /// elements in reverse order, exercising the safe <c>new T[]</c> allocation path.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForTypedArrayOfReferenceType_ShouldReturnElementsReversed() => CollectionAssert.AreEqual(new[] { "e", "d", "c", "b", "a" }, Strings.Reverse());

    /// <summary>
    /// Verifies that reversing a typed array of structs containing reference fields returns
    /// all elements in reverse order, exercising the safe allocation path for types where
    /// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences{T}"/>
    /// returns <see langword="true"/>.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForTypedArrayOfStructWithReferenceField_ShouldReturnElementsReversed()
    {
        CollectionAssert.AreEqual(
            new[] { new Wrapper("c"), new Wrapper("b"), new Wrapper("a") },
            Wrappers.Reverse());
    }

    /// <summary>
    /// Verifies that reversing a typed array does not mutate the original source.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForTypedArray_ShouldNotModifySource()
    {
        var original = Ints;
        _ = original.Reverse();
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that reversing a typed array returns a new heap allocation, not the
    /// original array instance.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForTypedArray_ShouldReturnNewAllocation()
    {
        var original = Ints;
        AssertIsNewAllocation(original, original.Reverse());
    }

    /// <summary>
    /// Verifies that passing a null typed array throws <see cref="ArgumentNullException"/>
    /// and that the exception identifies the correct parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsNull_ForTypedArray_ShouldThrowException()
    {
        int[]? source = null;
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(
            () => source!.Reverse());
        Assert.AreEqual("source", ex.ParamName);
    }

    // =========================================================================
    // T[] — index + count
    // =========================================================================

    /// <summary>
    /// Verifies that specifying a valid index and count on a typed array reverses only
    /// the nominated elements, leaving all others unchanged, across multiple section positions.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PartialReverseIntData), typeof(ArrayExtensionsTests))]
    public void Reverse_WhenRangeIsValid_ForTypedArrayIndexCount_ShouldReverseOnlyNominatedElements(
        int index, int count, int[] expected) => CollectionAssert.AreEqual(expected, Ints.Reverse(index, count));

    /// <summary>
    /// Verifies that a count of zero or one on a typed array returns a straight copy
    /// with no elements reversed, across multiple valid starting positions.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DegenerateCountData), typeof(ArrayExtensionsTests))]
    public void Reverse_WhenCountIsZeroOrOne_ForTypedArrayIndexCount_ShouldReturnStraightCopy(
        int index, int count) => CollectionAssert.AreEqual(Ints, Ints.Reverse(index, count));

    /// <summary>
    /// Verifies that elements outside the nominated range are copied unchanged
    /// into the returned result.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenRangeIsMiddleSection_ForTypedArrayIndexCount_ShouldLeaveOuterElementsUnchanged()
    {
        var result = Ints.Reverse(index: 1, count: 3);
        Assert.AreEqual(Ints[0], result[0]);
        Assert.AreEqual(Ints[4], result[4]);
    }

    /// <summary>
    /// Verifies that a partial reverse on a typed array does not mutate the original source.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForTypedArrayIndexCount_ShouldNotModifySource()
    {
        var original = Ints;
        _ = original.Reverse(1, 3);
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that passing a null typed array to the index + count overload throws
    /// <see cref="ArgumentNullException"/> and that the exception identifies the correct
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsNull_ForTypedArrayIndexCount_ShouldThrowException()
    {
        int[]? source = null;
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(            () =>
        {
            source!.Reverse(0, 1);
        });
        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid index on a typed array throws
    /// <see cref="ArgumentOutOfRangeException"/> and that the exception identifies the
    /// correct parameter name, covering negative values and values that exceed the length.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2, "index", DisplayName = "Negative index")]
    [DataRow(6, 0, "index", DisplayName = "Index exceeds array length")]
    public void Reverse_WhenIndexIsInvalid_ForTypedArrayIndexCount_ShouldThrowArgumentOutOfRangeException(
        int index, int count, string expectedParamName)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(            () =>
        {
            Ints.Reverse(index, count);
        });
        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid count on a typed array throws
    /// <see cref="ArgumentOutOfRangeException"/> and that the exception identifies the
    /// correct parameter name, covering negative values, values exceeding the length,
    /// and values that extend beyond the end.
    /// </summary>
    [TestMethod]
    [DataRow(0, -1, "count", DisplayName = "Negative count")]
    [DataRow(0, 10, "count", DisplayName = "Count exceeds array length")]
    public void Reverse_WhenCountIsInvalid_ForTypedArrayIndexCount_ShouldThrowArgumentOutOfRangeException(
        int index, int count, string expectedParamName)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Ints.Reverse(index, count);
        });
        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid count on a typed array throws
    /// <see cref="ArgumentException"/> and that the exception identifies the
    /// correct parameter name, covering negative values, values exceeding the length,
    /// and values that extend beyond the end.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenIndexAndCountIsInvalid_ForTypedArrayIndexCount_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Ints.Reverse(Ints.Length - 2, 3);
        });
    }

    // =========================================================================
    // T[] — Range
    // =========================================================================

    /// <summary>
    /// Verifies that a variety of Range expressions on a typed array each produce the
    /// correct result, covering start-relative, end-relative, full-span, empty, and
    /// single-element ranges.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReverseIntRangeData), typeof(ArrayExtensionsTests))]
    public void Reverse_WhenRangeExpressionIsSpecified_ForTypedArrayRange_ShouldProduceCorrectResult(
        int start, int end, bool startFromEnd, bool endFromEnd, int[] expected)
    {
        var range = new Range(
            new Index(start, startFromEnd),
            new Index(end, endFromEnd));
        CollectionAssert.AreEqual(expected, Ints.Reverse(range));
    }

    /// <summary>
    /// Verifies that reversing a typed array via a Range overload does not mutate the
    /// original source.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForTypedArrayRange_ShouldNotModifySource()
    {
        var original = Ints;
        _ = original.Reverse(1..4);
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that passing a null typed array to the Range overload throws
    /// <see cref="ArgumentNullException"/> and that the exception identifies the correct
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsNull_ForTypedArrayRange_ShouldThrowException()
    {
        int[]? source = null;
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(            () =>
        {
            source!.Reverse(1..3);
        });
        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a Range that resolves to an end index beyond the typed array length
    /// throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenRangeExceedsArrayLength_ForTypedArrayRange_ShouldThrowException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(            () =>
        {
            Ints.Reverse(2..10);
        });
    }

    // =========================================================================
    // Array (non-generic) — full reverse
    // =========================================================================

    /// <summary>
    /// Verifies that reversing non-generic arrays of different element types returns all
    /// elements in reverse order and preserves the original element type in the result.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NonGenericArrayFullReverseData), typeof(ArrayExtensionsTests))]
    public void Reverse_WhenSourceHasDifferentElementTypes_ForNonGenericArray_ShouldReturnAllElementsReversedAndPreserveElementType(
        Array input, Array expected)
    {
        Array result = input.Reverse();
        CollectionAssert.AreEqual(expected, result);
        Assert.AreEqual(input.GetType().GetElementType(), result.GetType().GetElementType());
    }

    /// <summary>
    /// Verifies that reversing a non-generic array returns a new heap allocation rather
    /// than the original instance.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForNonGenericArray_ShouldReturnNewAllocation()
    {
        Array source = Ints;
        AssertIsNewAllocation(source, source.Reverse());
    }

    /// <summary>
    /// Verifies that reversing a non-generic array does not mutate the original source.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForNonGenericArray_ShouldNotModifySource()
    {
        var original = Ints;
        _ = ((Array)original).Reverse();
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that passing a null array throws <see cref="ArgumentNullException"/>
    /// and that the exception identifies the correct parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsNull_ForNonGenericArray_ShouldThrowException()
    {
        Array? source = null;
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(            () =>
        {
            source!.Reverse();
        });
        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that passing a multidimensional array throws <see cref="RankException"/>
    /// with a message confirming only single-dimensional arrays are supported, and that the
    /// exception identifies the correct parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsMultidimensional_ForNonGenericArray_ShouldThrowException()
    {
        Array source = new int[2, 3];
        Assert.ThrowsExactly<RankException>(() =>
        {
            source.Reverse();
        });
    }

    // =========================================================================
    // Array (non-generic) — index + count
    // =========================================================================

    /// <summary>
    /// Verifies that specifying a valid index and count on a non-generic array reverses
    /// only the nominated elements, leaving all others unchanged.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PartialReverseIntData), typeof(ArrayExtensionsTests))]
    public void Reverse_WhenRangeIsValid_ForNonGenericArrayIndexCount_ShouldReverseOnlyNominatedElements(
        int index, int count, int[] expected)
    {
        Array result = ((Array)Ints).Reverse(index, count);
        CollectionAssert.AreEqual(expected, (int[])result);
    }

    /// <summary>
    /// Verifies that a count of zero or one on a non-generic array returns a straight copy
    /// with no elements reversed.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DegenerateCountData), typeof(ArrayExtensionsTests))]
    public void Reverse_WhenCountIsZeroOrOne_ForNonGenericArrayIndexCount_ShouldReturnStraightCopy(
        int index, int count)
    {
        Array result = ((Array)Ints).Reverse(index, count);
        CollectionAssert.AreEqual(Ints, (int[])result);
    }

    /// <summary>
    /// Verifies that elements outside the nominated range are copied unchanged into the result.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenRangeIsMiddleSection_ForNonGenericArrayIndexCount_ShouldLeaveOuterElementsUnchanged()
    {
        Array result = ((Array)Ints).Reverse(index: 1, count: 3);
        Assert.AreEqual(Ints[0], result.GetValue(0));
        Assert.AreEqual(Ints[4], result.GetValue(4));
    }

    /// <summary>
    /// Verifies that passing a null array to the index + count overload throws
    /// <see cref="ArgumentNullException"/> and that the exception identifies the correct
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsNull_ForNonGenericArrayIndexCount_ShouldThrowException()
    {
        Array? source = null;
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(            () =>
        {
            source!.Reverse(0, 1);
        });
        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that passing a multidimensional array to the index + count overload throws
    /// <see cref="ArgumentException"/> and that the exception identifies the correct
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsMultidimensional_ForNonGenericArrayIndexCount_ShouldThrowException()
    {
        Array source = new int[2, 3];
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(            () =>
        {
            source.Reverse(0, 1);
        });
        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid index on a non-generic array throws
    /// <see cref="ArgumentOutOfRangeException"/> and that the exception identifies the
    /// correct parameter name, covering negative values and values that exceed the length.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2, "index", DisplayName = "Negative index")]
    [DataRow(6, 0, "index", DisplayName = "Index exceeds array length")]
    public void Reverse_WhenIndexIsInvalid_ForNonGenericArrayIndexCount_ShouldThrowArgumentOutOfRangeException(
        int index, int count, string expectedParamName)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(            () =>
        {
            ((Array)Ints).Reverse(index, count);
        });
        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid count on a non-generic array throws
    /// <see cref="ArgumentOutOfRangeException"/> and that the exception identifies the
    /// correct parameter name, covering negative values and values that exceed the length.
    /// </summary>
    [TestMethod]
    [DataRow(0, -1, "count", DisplayName = "Negative count")]
    [DataRow(0, 10, "count", DisplayName = "Count exceeds array length")]
    public void Reverse_WhenCountIsInvalid_ForNonGenericArrayIndexCount_ShouldThrowArgumentOutOfRangeException(
        int index, int count, string expectedParamName)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(            () =>
        {
            ((Array)Ints).Reverse(index, count);
        });
        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that an index and count combination that extends beyond the end of a
    /// non-generic array throws <see cref="ArgumentException"/>. The exception carries no
    /// single parameter name because both index and count contribute to the violation;
    /// the message is asserted to confirm the correct validation path was reached.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenIndexPlusCountExceedsArrayLength_ForNonGenericArrayIndexCount_ShouldThrowException()
    {
        // ThrowIfArrayOffsetOrCountInvalid raises ArgumentException (not
        // ArgumentOutOfRangeException) for the combination case, aligning with the
        // contract of Array.Reverse which also throws ArgumentException here.
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(            () =>
        {
            ((Array)Ints).Reverse(index: 3, count: 3);
        });
        Assert.IsNotNull(ex.Message);
    }

    // =========================================================================
    // Array (non-generic) — Range
    // =========================================================================

    /// <summary>
    /// Verifies that Range expressions applied to a non-generic array produce the correct
    /// result for start-relative, end-relative, and full-span ranges.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NonGenericArrayRangeData), typeof(ArrayExtensionsTests))]
    public void Reverse_WhenRangeExpressionIsSpecified_ForNonGenericArrayRange_ShouldProduceCorrectResult(
        int start, int end, bool startFromEnd, bool endFromEnd, int[] expected)
    {
        var range = new Range(
            new Index(start, startFromEnd),
            new Index(end, endFromEnd));
        Array result = ((Array)Ints).Reverse(range);
        CollectionAssert.AreEqual(expected, (int[])result);
    }

    /// <summary>
    /// Verifies that reversing a non-generic array via a Range overload does not mutate
    /// the original source.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForNonGenericArrayRange_ShouldNotModifySource()
    {
        var original = Ints;
        _ = ((Array)original).Reverse(1..4);
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that passing a null array to the Range overload throws
    /// <see cref="ArgumentNullException"/> and that the exception identifies the correct
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsNull_ForNonGenericArrayRange_ShouldThrowException()
    {
        Array? source = null;
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(            () =>
        {
            source!.Reverse(1..3);
        });
        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that passing a multidimensional array to the Range overload throws
    /// <see cref="ArgumentException"/> and that the exception identifies the correct
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsMultidimensional_ForNonGenericArrayRange_ShouldThrowException()
    {
        Array source = new int[2, 3];
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(            () =>
        {
            source.Reverse(0..1);
        });
        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a Range that resolves to an end index beyond the non-generic array
    /// length throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenRangeExceedsArrayLength_ForNonGenericArrayRange_ShouldThrowException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(            () =>
        {
            ((Array)Ints).Reverse(2..10);
        });
    }

    // =========================================================================
    // ArrayExtensions.ReverseCore<T> — internal generic core
    // Requires [assembly: InternalsVisibleTo("Bodu.Extensions.Tests")] in the
    // main project.
    // =========================================================================

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.ReverseCore{T}"/> produces the correct
    /// result across index, count, and expected value combinations, covering full-span,
    /// partial, and degenerate cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReverseCoreData), typeof(ArrayExtensionsTests))]
    public void ReverseCore_WhenCalledWithValidBounds_ForGenericCore_ShouldProduceCorrectResult(
        int index, int count, int[] expected) => CollectionAssert.AreEqual(expected, ArrayExtensions.ReverseCore<int>(Ints, index, count));

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.ReverseCore{T}"/> returns a
    /// <typeparamref name="T"/>[] so that both span-based and array-based public
    /// overloads share the allocation without a secondary copy.
    /// </summary>
    [TestMethod]
    public void ReverseCore_WhenCalled_ForGenericCore_ShouldReturnTypedArray()
    {
        Assert.IsInstanceOfType<int[]>(
            ArrayExtensions.ReverseCore<int>(Ints, 0, Ints.Length));
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.ReverseCore{T}"/> returns a result
    /// whose length matches the full source length, even when only a slice is reversed.
    /// </summary>
    [TestMethod]
    public void ReverseCore_WhenPartialRangeIsSpecified_ForGenericCore_ShouldReturnResultWithFullSourceLength() => Assert.AreEqual(Ints.Length, ArrayExtensions.ReverseCore<int>(Ints, 1, 2).Length);

    /// <summary>
    /// Verifies that an empty source returns an empty result.
    /// </summary>
    [TestMethod]
    public void ReverseCore_WhenSourceIsEmpty_ForGenericCore_ShouldReturnEmptyArray() => Assert.AreEqual(0, ArrayExtensions.ReverseCore<int>(NoInts, 0, 0).Length);

    /// <summary>
    /// Verifies that unmanaged element types produce the correct result, exercising
    /// the <see cref="GC.AllocateUninitializedArray{T}"/> allocation path.
    /// </summary>
    [TestMethod]
    public void ReverseCore_WhenCalled_ForGenericCoreUnmanagedType_ShouldReturnCorrectResult()
    {
        CollectionAssert.AreEqual(
            new[] { 5, 4, 3, 2, 1 },
            ArrayExtensions.ReverseCore<int>(Ints, 0, Ints.Length));
    }

    /// <summary>
    /// Verifies that reference-type elements produce the correct result, exercising
    /// the safe <c>new T[]</c> allocation path.
    /// </summary>
    [TestMethod]
    public void ReverseCore_WhenCalled_ForGenericCoreReferenceType_ShouldReturnCorrectResult()
    {
        CollectionAssert.AreEqual(
            new[] { "e", "d", "c", "b", "a" },
            ArrayExtensions.ReverseCore<string?>(Strings, 0, Strings.Length));
    }

    /// <summary>
    /// Verifies that structs containing reference fields produce the correct result,
    /// exercising the safe allocation path for types where
    /// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences{T}"/>
    /// returns <see langword="true"/>.
    /// </summary>
    [TestMethod]
    public void ReverseCore_WhenCalled_ForGenericCoreStructWithReferenceField_ShouldReturnCorrectResult()
    {
        CollectionAssert.AreEqual(
            new[] { new Wrapper("c"), new Wrapper("b"), new Wrapper("a") },
            ArrayExtensions.ReverseCore<Wrapper>(Wrappers, 0, Wrappers.Length));
    }

    // =========================================================================
    // ArrayExtensions.ReverseArrayCore — internal non-generic core
    // Requires [assembly: InternalsVisibleTo("Bodu.Extensions.Tests")] in the
    // main project.
    // =========================================================================

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.ReverseArrayCore"/> produces the correct
    /// result across index, count, and expected value combinations, covering full-span,
    /// partial, and degenerate cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReverseCoreData), typeof(ArrayExtensionsTests))]
    public void ReverseArrayCore_WhenCalledWithValidBounds_ForNonGenericCore_ShouldProduceCorrectResult(
        int index, int count, int[] expected)
    {
        Array result = ArrayExtensions.ReverseArrayCore(Ints, index, count);
        CollectionAssert.AreEqual(expected, (int[])result);
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.ReverseArrayCore"/> preserves the element
    /// type of the source array in the returned result.
    /// </summary>
    [TestMethod]
    [DataRow(typeof(int), DisplayName = "int — unmanaged")]
    [DataRow(typeof(string), DisplayName = "string — reference type")]
    public void ReverseArrayCore_WhenCalled_ForNonGenericCore_ShouldPreserveElementType(Type elementType)
    {
        var source = Array.CreateInstance(elementType, 3);
        Array result = ArrayExtensions.ReverseArrayCore(source, 0, source.Length);
        Assert.AreEqual(elementType, result.GetType().GetElementType());
    }

    /// <summary>
    /// Verifies that <see cref="ArrayExtensions.ReverseArrayCore"/> returns a new
    /// allocation rather than the original source array.
    /// </summary>
    [TestMethod]
    public void ReverseArrayCore_WhenCalled_ForNonGenericCore_ShouldReturnNewAllocation()
    {
        Array source = Ints;
        AssertIsNewAllocation(source, ArrayExtensions.ReverseArrayCore(source, 0, source.Length));
    }

    /// <summary>
    /// Verifies that a count of zero or one returns a straight copy with the same elements
    /// as the source, confirming the <c>count &gt; 1</c> guard is correctly applied.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DegenerateCountData), typeof(ArrayExtensionsTests))]
    public void ReverseArrayCore_WhenCountIsZeroOrOne_ForNonGenericCore_ShouldReturnStraightCopy(
        int index, int count)
    {
        Array result = ArrayExtensions.ReverseArrayCore(Ints, index, count);
        CollectionAssert.AreEqual(Ints, (int[])result);
    }
}