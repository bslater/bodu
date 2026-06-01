// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpanExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Base class for <see cref="SpanExtensions"/> tests. Provides shared fixtures,
/// test data sources, and assertion helpers used across all span extension method
/// test suites.
/// </summary>
[TestClass]
public partial class SpanExtensionsTests
{

    /// <summary>
    /// Provides (index, count) pairs where count is zero or one, confirming the
    /// operation produces a straight copy with no reversal performed.
    /// </summary>
    public static IEnumerable<object[]> DegenerateCountData =>
        [
            [0, 0], // count zero at start
            [2, 0], // count zero at middle
            [0, 1], // count one at start
            [2, 1], // count one at middle
            [4, 1], // count one at last element
        ];

    // -------------------------------------------------------------------------
    // Data sources — full reverse, varying lengths
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides (input, expected) integer array pairs of varying lengths for
    /// full-reverse data-driven tests, covering odd, even, single-element, and empty cases.
    /// </summary>
    public static IEnumerable<object[]> FullReverseIntData =>
        [
            [new[] { 1, 2, 3, 4, 5 }, new[] { 5, 4, 3, 2, 1 }], // odd length
            [new[] { 1, 2, 3, 4 },    new[] { 4, 3, 2, 1 }], // even length
            [new[] { 42 },             new[] { 42 }], // single element
            [Array.Empty<int>(),       Array.Empty<int>()], // empty
        ];

    // -------------------------------------------------------------------------
    // Data sources — partial reverse, index + count
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides (index, count, expected) triples for partial-reverse data-driven
    /// tests, covering middle, first, last, and full-span section positions.
    /// </summary>
    public static IEnumerable<object[]> PartialReverseIntData =>
        [
            [1, 3, new[] { 1, 4, 3, 2, 5 }], // middle section
            [0, 3, new[] { 3, 2, 1, 4, 5 }], // first section
            [2, 3, new[] { 1, 2, 5, 4, 3 }], // last section
            [0, 5, new[] { 5, 4, 3, 2, 1 }], // full span
        ];

    // -------------------------------------------------------------------------
    // Data sources — Range expressions
    // Note: Range is not a valid [DataRow] argument; [DynamicData] is required
    // for all Range-based parameterised tests.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides (Range, expected int[]) pairs for Range-based partial-reverse
    /// data-driven tests over <c>{ 1, 2, 3, 4, 5 }</c>, covering start-relative,
    /// end-relative, full-span, empty, and single-element ranges.
    /// </summary>
    public static IEnumerable<object[]> ReverseIntRangeData =>
        [
        [1, 4, false, false, new[] { 1, 4, 3, 2, 5 }], // 1..4   start-relative, middle
        [4, 1, true,  true,  new[] { 1, 4, 3, 2, 5 }], // ^4..^1 end-relative, equivalent
        [0, 0, false, true,  new[] { 5, 4, 3, 2, 1 }], // 0..^0  full array
        [0, 3, false, false, new[] { 3, 2, 1, 4, 5 }], // 0..3   first section
        [2, 5, false, false, new[] { 1, 2, 5, 4, 3 }], // 2..5   last section
        [2, 2, false, false, new[] { 1, 2, 3, 4, 5 }], // 2..2   empty range — straight copy
        [2, 3, false, false, new[] { 1, 2, 3, 4, 5 }], // 2..3   single element — straight copy
        ];

    // -------------------------------------------------------------------------
    // Shared test data
    // -------------------------------------------------------------------------

    /// <summary>
    /// An odd-length unmanaged integer array. Exercises the
    /// <see cref="GC.AllocateUninitializedArray{T}"/> allocation path.
    /// </summary>
    protected static int[] Ints => [1, 2, 3, 4, 5];

    /// <summary>An empty unmanaged integer array.</summary>
    protected static int[] NoInts => [];

    /// <summary>A single-element unmanaged integer array.</summary>
    protected static int[] OneInt => [42];

    /// <summary>
    /// An odd-length reference-type array. Exercises the safe <c>new T[]</c>
    /// allocation path.
    /// </summary>
    protected static string?[] Strings => ["a", "b", "c", "d", "e"];

    /// <summary>An even-length unmanaged integer array.</summary>
    protected static int[] TwoInts => [1, 2];

    /// <summary>
    /// An odd-length array of <see cref="Wrapper"/> structs. Exercises the safe
    /// allocation path for structs that contain reference fields.
    /// </summary>
    protected static Wrapper[] Wrappers =>
        [new Wrapper("a"), new Wrapper("b"), new Wrapper("c")];

    // -------------------------------------------------------------------------
    // Shared assertion helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Asserts that <paramref name="original"/> contains exactly the values
    /// <c>{ 1, 2, 3, 4, 5 }</c>, confirming the source was not mutated by the
    /// operation under test.
    /// </summary>
    protected static void AssertIntsSourceIsUnmodified(int[] original) =>
        CollectionAssert.AreEqual(
            new[] { 1, 2, 3, 4, 5 },
            original,
            "Source array was unexpectedly modified by the operation.");

    /// <summary>
    /// Asserts that <paramref name="result"/> is not the same object reference as
    /// <paramref name="source"/>, confirming the operation produced a new heap
    /// allocation rather than returning the original.
    /// </summary>
    protected static void AssertIsNewAllocation(object source, object result) =>
        Assert.AreNotSame(
            source,
            result,
            "Operation returned the original instance rather than a new allocation.");
    // -------------------------------------------------------------------------
    // Shared type fixtures
    // -------------------------------------------------------------------------

    /// <summary>
    /// A struct containing a reference field. Used to verify that
    /// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences{T}"/>
    /// correctly routes to the safe <c>new T[]</c> allocation path.
    /// </summary>
    protected readonly record struct Wrapper(string Value);

}