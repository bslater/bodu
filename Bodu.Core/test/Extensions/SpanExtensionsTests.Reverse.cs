// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpanExtensionsTests.Reverse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions;

public partial class SpanExtensionsTests
{
    // =========================================================================
    // ReadOnlySpan<T> — full reverse
    // =========================================================================

    /// <summary>
    /// Verifies that reversing integer spans of varying lengths always returns all
    /// elements in reverse order, covering odd, even, single-element, and empty cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(FullReverseIntData), typeof(SpanExtensionsTests))]
    public void Reverse_WhenSourceHasDifferentLengths_ForReadOnlySpan_ShouldReturnAllElementsReversed(
        int[] input, int[] expected)
    {
        ReadOnlySpan<int> source = input;
        CollectionAssert.AreEqual(expected, source.Reverse().ToArray());
    }

    /// <summary>
    /// Verifies that reversing a span of reference-type elements returns all elements
    /// in reverse order, exercising the safe <c>new T[]</c> allocation path.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForReadOnlySpanOfReferenceType_ShouldReturnElementsReversed()
    {
        ReadOnlySpan<string?> source = Strings;
        CollectionAssert.AreEqual(new[] { "e", "d", "c", "b", "a" }, source.Reverse().ToArray());
    }

    /// <summary>
    /// Verifies that reversing a span of structs containing reference fields returns all
    /// elements in reverse order, exercising the safe allocation path for types where
    /// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences{T}"/>
    /// returns <see langword="true"/>.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForReadOnlySpanOfStructWithReferenceField_ShouldReturnElementsReversed()
    {
        ReadOnlySpan<Wrapper> source = Wrappers;
        CollectionAssert.AreEqual(
            new[] { new Wrapper("c"), new Wrapper("b"), new Wrapper("a") },
            source.Reverse().ToArray());
    }

    /// <summary>
    /// Verifies that reversing a span does not mutate the underlying source data.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForReadOnlySpan_ShouldNotModifySource()
    {
        int[] original = Ints;
        _ = ((ReadOnlySpan<int>)original).Reverse();
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that mutating the returned span does not affect the original source,
    /// confirming the result is an independent heap allocation.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForReadOnlySpan_ShouldReturnIndependentAllocation()
    {
        int[] original = Ints;
        Span<int> result = ((ReadOnlySpan<int>)original).Reverse();
        result[0] = 99;
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that the length of the result matches the length of the source span.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForReadOnlySpan_ShouldReturnResultWithSameLengthAsSource()
    {
        ReadOnlySpan<int> source = Ints;
        Assert.AreEqual(source.Length, source.Reverse().Length);
    }

    // =========================================================================
    // ReadOnlySpan<T> — index + count
    // =========================================================================

    /// <summary>
    /// Verifies that specifying a valid index and count reverses only the nominated
    /// elements, leaving all others unchanged, across multiple section positions.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PartialReverseIntData), typeof(SpanExtensionsTests))]
    public void Reverse_WhenRangeIsValid_ForReadOnlySpanIndexCount_ShouldReverseOnlyNominatedElements(
        int index, int count, int[] expected)
    {
        ReadOnlySpan<int> source = Ints;
        CollectionAssert.AreEqual(expected, source.Reverse(index, count).ToArray());
    }

    /// <summary>
    /// Verifies that a count of zero or one returns a straight copy with no elements
    /// reversed, across multiple valid starting positions.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DegenerateCountData), typeof(SpanExtensionsTests))]
    public void Reverse_WhenCountIsZeroOrOne_ForReadOnlySpanIndexCount_ShouldReturnStraightCopy(
        int index, int count)
    {
        ReadOnlySpan<int> source = Ints;
        CollectionAssert.AreEqual(Ints, source.Reverse(index, count).ToArray());
    }

    /// <summary>
    /// Verifies that an empty span with index and count both zero returns an empty result.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSourceIsEmptyAndCountIsZero_ForReadOnlySpanIndexCount_ShouldReturnEmptyResult()
    {
        ReadOnlySpan<int> source = NoInts;
        Assert.AreEqual(0, source.Reverse(0, 0).Length);
    }

    /// <summary>
    /// Verifies that elements outside the nominated range are copied unchanged
    /// into the returned result.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenRangeIsMiddleSection_ForReadOnlySpanIndexCount_ShouldLeaveOuterElementsUnchanged()
    {
        ReadOnlySpan<int> source = Ints;
        Span<int> result = source.Reverse(index: 1, count: 3);
        Assert.AreEqual(Ints[0], result[0]);
        Assert.AreEqual(Ints[4], result[4]);
    }

    /// <summary>
    /// Verifies that a partial reverse does not mutate the underlying source data.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForReadOnlySpanIndexCount_ShouldNotModifySource()
    {
        int[] original = Ints;
        _ = ((ReadOnlySpan<int>)original).Reverse(1, 3);
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that a partial reverse of a reference-type span reverses only the
    /// nominated section, exercising the safe allocation path.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForReadOnlySpanIndexCountOfReferenceType_ShouldReverseOnlyNominatedElements()
    {
        ReadOnlySpan<string?> source = Strings;
        CollectionAssert.AreEqual(
            new[] { "a", "d", "c", "b", "e" },
            source.Reverse(1, 3).ToArray());
    }

    /// <summary>
    /// Verifies that an invalid index throws <see cref="ArgumentOutOfRangeException"/>
    /// and that the exception identifies the correct parameter name, covering negative
    /// values and values that exceed the span length.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2, "index", DisplayName = "Negative index")]
    [DataRow(6, 0, "index", DisplayName = "Index exceeds span length")]
    public void Reverse_WhenIndexIsInvalid_ForReadOnlySpanIndexCount_ShouldThrowArgumentOutOfRangeException(
        int index, int count, string expectedParamName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Ints.AsSpan().AsReadOnly().Reverse(index, count);
        });
        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid count throws <see cref="ArgumentOutOfRangeException"/>
    /// and that the exception identifies the correct parameter name, covering negative
    /// values, values exceeding the span length, and values that extend beyond the end.
    /// </summary>
    [TestMethod]
    [DataRow(0, -1, "count", DisplayName = "Negative count")]
    [DataRow(0, 10, "count", DisplayName = "Count exceeds span length")]
    public void Reverse_WhenCountIsInvalid_ForReadOnlySpanIndexCount_ShouldThrowArgumentOutOfRangeException(
        int index, int count, string expectedParamName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Ints.AsSpan().AsReadOnly().Reverse(index, count);
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
    public void Reverse_WhenIndexAndCountIsInvalid_ForReadOnlySpanIndexCount_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Ints.AsSpan().AsReadOnly().Reverse(Ints.Length - 2, 3);
        });
    }

    // =========================================================================
    // ReadOnlySpan<T> — Range
    // =========================================================================

    /// <summary>
    /// Verifies that a variety of Range expressions each produce the correct result,
    /// covering start-relative, end-relative, full-span, empty, and single-element ranges.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReverseIntRangeData), typeof(SpanExtensionsTests))]
    public void Reverse_WhenRangeExpressionIsSpecified_ForReadOnlySpanRange_ShouldProduceCorrectResult(
    int start, int end, bool startFromEnd, bool endFromEnd, int[] expected)
    {
        var range = new Range(
            new Index(start, startFromEnd),
            new Index(end, endFromEnd));
        ReadOnlySpan<int> source = Ints;
        CollectionAssert.AreEqual(expected, source.Reverse(range).ToArray());
    }

    /// <summary>
    /// Verifies that reversing a span via a Range overload does not mutate the
    /// underlying source data.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForReadOnlySpanRange_ShouldNotModifySource()
    {
        int[] original = Ints;
        _ = ((ReadOnlySpan<int>)original).Reverse(1..4);
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that a Range that resolves to an end index beyond the span length
    /// throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenRangeExceedsSpanLength_ForReadOnlySpanRange_ShouldThrowException()
    {
        ReadOnlySpan<int> source = Ints;
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(            () =>
        {
            Ints.AsSpan().AsReadOnly().Reverse(2..10);
        });
    }

    // =========================================================================
    // Span<T> — delegation to ReadOnlySpan<T> overloads
    // =========================================================================

    /// <summary>
    /// Verifies that the mutable Span full-reverse overload produces identical results
    /// to the canonical ReadOnlySpan overload across varying lengths.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(FullReverseIntData), typeof(SpanExtensionsTests))]
    public void Reverse_WhenCalled_ForMutableSpan_ShouldProduceSameResultAsReadOnlySpanOverload(
        int[] input, int[] expected)
    {
        Span<int> source = input;
        CollectionAssert.AreEqual(expected, source.Reverse().ToArray());
    }

    /// <summary>
    /// Verifies that the mutable Span index + count overload produces identical results
    /// to the canonical ReadOnlySpan overload across multiple section positions.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PartialReverseIntData), typeof(SpanExtensionsTests))]
    public void Reverse_WhenIndexAndCountAreSpecified_ForMutableSpanIndexCount_ShouldProduceSameResultAsReadOnlySpanOverload(
        int index, int count, int[] expected)
    {
        Span<int> source = Ints;
        CollectionAssert.AreEqual(expected, source.Reverse(index, count).ToArray());
    }

    /// <summary>
    /// Verifies that the mutable Span Range overload produces identical results to
    /// the canonical ReadOnlySpan overload across a variety of Range expressions.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReverseIntRangeData), typeof(SpanExtensionsTests))]
    public void Reverse_WhenRangeExpressionIsSpecified_ForMutableSpanRange_ShouldProduceSameResultAsReadOnlySpanOverload(
    int start, int end, bool startFromEnd, bool endFromEnd, int[] expected)
    {
        var range = new Range(
            new Index(start, startFromEnd),
            new Index(end, endFromEnd));
        Span<int> source = Ints;
        CollectionAssert.AreEqual(expected, source.Reverse(range).ToArray());
    }

    /// <summary>
    /// Verifies that the mutable Span full-reverse overload does not mutate the
    /// underlying source data.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenCalled_ForMutableSpan_ShouldNotModifySource()
    {
        int[] original = Ints;
        _ = ((Span<int>)original).Reverse();
        AssertIntsSourceIsUnmodified(original);
    }

    /// <summary>
    /// Verifies that an invalid index passed to the mutable Span overload throws
    /// <see cref="ArgumentOutOfRangeException"/> with the correct parameter name,
    /// confirming argument validation is correctly delegated.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2, "index", DisplayName = "Negative index")]
    [DataRow(6, 0, "index", DisplayName = "Index exceeds span length")]
    public void Reverse_WhenIndexIsInvalid_ForMutableSpanIndexCount_ShouldThrowArgumentOutOfRangeException(
        int index, int count, string expectedParamName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(            () =>
        {
            Ints.AsSpan().Reverse(index, count);
        });
        Assert.AreEqual(expectedParamName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that an invalid count passed to the mutable Span overload throws
    /// <see cref="ArgumentOutOfRangeException"/> with the correct parameter name,
    /// confirming argument validation is correctly delegated.
    /// </summary>
    [TestMethod]
    [DataRow(0, -1, "count", DisplayName = "Negative count")]
    [DataRow(0, 10, "count", DisplayName = "Count exceeds span length")]
    public void Reverse_WhenCountIsInvalid_ForMutableSpanIndexCount_ShouldThrowArgumentOutOfRangeException(
        int index, int count, string expectedParamName)
    {
        Span<int> source = Ints;
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(            () =>
        {
            Ints.AsSpan().Reverse(index, count);
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
    public void Reverse_WhenIndexAndCountIsInvalid_ForMutableSpanIndexCount_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Ints.AsSpan().Reverse(Ints.Length - 2, 3);
        });
    }
}