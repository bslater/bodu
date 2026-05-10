// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ArrayExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Extensions;

/// <summary>
/// Base class for <see cref="ArrayExtensions"/> tests. Provides shared fixtures,
/// test data sources, and assertion helpers used across all array extension method
/// test suites.
/// </summary>
[TestClass]
public partial class ArrayExtensionsTests
{
    // -------------------------------------------------------------------------
    // Shared type fixtures
    // -------------------------------------------------------------------------

    /// <summary>
    /// A struct containing a reference field. Used to verify that
    /// <see cref="System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences{T}"/>
    /// correctly routes to the safe <c>new T[]</c> allocation path in
    /// <see cref="ArrayExtensions.ReverseCore{T}"/>.
    /// </summary>
    protected readonly record struct Wrapper(string Value);

    // -------------------------------------------------------------------------
    // Shared test data
    // -------------------------------------------------------------------------

    /// <summary>
    /// An odd-length unmanaged integer array. Exercises the
    /// <see cref="GC.AllocateUninitializedArray{T}"/> allocation path.
    /// </summary>
    protected static int[] Ints => [1, 2, 3, 4, 5];

    /// <summary>An even-length unmanaged integer array.</summary>
    protected static int[] TwoInts => [1, 2];

    /// <summary>A single-element unmanaged integer array.</summary>
    protected static int[] OneInt => [42];

    /// <summary>An empty unmanaged integer array.</summary>
    protected static int[] NoInts => [];

    /// <summary>
    /// An odd-length reference-type array. Exercises the safe <c>new T[]</c>
    /// allocation path.
    /// </summary>
    protected static string?[] Strings => ["a", "b", "c", "d", "e"];

    /// <summary>
    /// An odd-length array of <see cref="Wrapper"/> structs. Exercises the safe
    /// allocation path for structs that contain reference fields.
    /// </summary>
    protected static Wrapper[] Wrappers =>
        [new Wrapper("a"), new Wrapper("b"), new Wrapper("c")];

    // -------------------------------------------------------------------------
    // Data sources — full reverse, varying lengths
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides (input, expected) integer array pairs of varying lengths for
    /// full-reverse data-driven tests, covering odd, even, single-element, and empty cases.
    /// </summary>
    public static IEnumerable<object[]> FullReverseIntData =>
        [
            new object[] { new[] { 1, 2, 3, 4, 5 }, new[] { 5, 4, 3, 2, 1 } }, // odd length
            [new[] { 1, 2, 3, 4 },    new[] { 4, 3, 2, 1 }], // even length
            [new[] { 42 },             new[] { 42 }], // single element
            [Array.Empty<int>(),       Array.Empty<int>()], // empty
        ];

    // -------------------------------------------------------------------------
    // Data sources — partial reverse, index + count
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides (index, count, expected) triples for partial-reverse data-driven
    /// tests, covering middle, first, last, and full-length section positions.
    /// </summary>
    public static IEnumerable<object[]> PartialReverseIntData =>
        [
            new object[] { 1, 3, new[] { 1, 4, 3, 2, 5 } }, // middle section
            [0, 3, new[] { 3, 2, 1, 4, 5 }], // first section
            [2, 3, new[] { 1, 2, 5, 4, 3 }], // last section
            [0, 5, new[] { 5, 4, 3, 2, 1 }], // full array
        ];

    /// <summary>
    /// Provides (index, count) pairs where count is zero or one, confirming the
    /// operation produces a straight copy with no reversal performed.
    /// </summary>
    public static IEnumerable<object[]> DegenerateCountData =>
        [
            new object[] { 0, 0 }, // count zero at start
            [2, 0], // count zero at middle
            [0, 1], // count one at start
            [2, 1], // count one at middle
            [4, 1], // count one at last element
        ];

    // -------------------------------------------------------------------------
    // Data sources — Range expressions
    // Note: Range is not a valid [DataRow] argument; [DynamicData] is required
    // for all Range-based parameterised tests.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides (start, end, startFromEnd, endFromEnd, expected int[]) tuples for Range-based
    /// partial-reverse data-driven tests over <c>{ 1, 2, 3, 4, 5 }</c>, covering start-relative,
    /// end-relative, full-span, empty, and single-element ranges.
    /// </summary>
    public static IEnumerable<object[]> ReverseIntRangeData =>
        [
            new object[] { 1, 4, false, false, new[] { 1, 4, 3, 2, 5 } }, // 1..4   start-relative, middle
            [4, 1, true,  true,  new[] { 1, 4, 3, 2, 5 }], // ^4..^1 end-relative, equivalent
            [0, 0, false, true,  new[] { 5, 4, 3, 2, 1 }], // 0..^0  full array
            [0, 3, false, false, new[] { 3, 2, 1, 4, 5 }], // 0..3   first section
            [2, 5, false, false, new[] { 1, 2, 5, 4, 3 }], // 2..5   last section
            [2, 2, false, false, new[] { 1, 2, 3, 4, 5 }], // 2..2   empty range — straight copy
            [2, 3, false, false, new[] { 1, 2, 3, 4, 5 }], // 2..3   single element — straight copy
        ];

    // -------------------------------------------------------------------------
    // Data sources — non-generic Array, element type coverage
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides (Array input, Array expected) pairs of different element types for
    /// non-generic full-reverse data-driven tests.
    /// </summary>
    public static IEnumerable<object[]> NonGenericArrayFullReverseData =>
        [
            new object[] { new int[]    { 1, 2, 3, 4, 5 }, new int[]    { 5, 4, 3, 2, 1 } },
            [new string[] { "a", "b", "c" }, new string[] { "c", "b", "a" }],
            [Array.Empty<int>(),             Array.Empty<int>()],
        ];

    /// <summary>
    /// Provides (Range, expected int[]) pairs for non-generic Array Range-based
    /// data-driven tests over <c>{ 1, 2, 3, 4, 5 }</c>.
    /// </summary>
    public static IEnumerable<object[]> NonGenericArrayRangeData =>
        [
            new object[] { 0,  0,  false, false, new[] { 1, 2, 3, 4, 5 } }, // 0..0   empty range, no-op
            [1,  4,  false, false, new[] { 1, 4, 3, 2, 5 }], // 1..4   start-relative
            [4,  1,  true,  true,  new[] { 1, 4, 3, 2, 5 }], // ^4..^1 end-relative
            [0,  0,  false, true,  new[] { 5, 4, 3, 2, 1 }], // 0..^0  full array
        ];

    // -------------------------------------------------------------------------
    // Data sources — ReverseCore<T> and ReverseArrayCore
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides (index, count, expected) triples for <see cref="ArrayExtensions.ReverseCore{T}"/>
    /// and <see cref="ArrayExtensions.ReverseArrayCore"/> data-driven tests, covering
    /// full-span, partial, and degenerate cases.
    /// </summary>
    public static IEnumerable<object[]> ReverseCoreData =>
        [
            new object[] { 0, 5, new[] { 5, 4, 3, 2, 1 } }, // full reverse
            [1, 3, new[] { 1, 4, 3, 2, 5 }], // partial — middle
            [0, 3, new[] { 3, 2, 1, 4, 5 }], // partial — first section
            [2, 3, new[] { 1, 2, 5, 4, 3 }], // partial — last section
            [2, 0, new[] { 1, 2, 3, 4, 5 }], // count zero — straight copy
            [2, 1, new[] { 1, 2, 3, 4, 5 }], // count one — straight copy
        ];

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
}