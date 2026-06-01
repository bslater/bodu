// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.WhereNotNullKatTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Collections.Generic.Extensions;

/// <summary>
/// Drives <see cref="EnumerableKat{TInput, TExpected}" /> rows against
/// <see cref="IEnumerableExtensions.WhereNotNull{TSource}(IEnumerable{TSource})" /> for reference
/// types and nullable value types. Deferred-execution and null-source coverage stays in
/// <see cref="IEnumerableExtensionsTests_WhereNotNull" /> — this class layers KAT-driven coverage on
/// top without duplicating the binary-test behaviour those partials already capture.
/// </summary>
[TestClass]
public sealed class IEnumerableExtensionsWhereNotNullKatTests
{
    private static IReadOnlyList<EnumerableKat<string?, string[]>> ReferenceKats { get; } =
        [
        new(
            Name: "empty source",
            Source: [],
            Expected: []),

        new(
            Name: "no nulls",
            Source: ["a", "b", "c"],
            Expected: ["a", "b", "c"]),

        new(
            Name: "interleaved nulls",
            Source: ["a", null, "b", null, "c"],
            Expected: ["a", "b", "c"]),

        new(
            Name: "all nulls",
            Source: [null, null, null],
            Expected: []),
    ];

    private static IReadOnlyList<EnumerableKat<int?, int[]>> NullableValueKats { get; } =
        [
        new(
            Name: "empty source",
            Source: [],
            Expected: []),

        new(
            Name: "interleaved nulls",
            Source: [1, null, 2, null, 3],
            Expected: [1, 2, 3]),

        new(
            Name: "duplicates kept",
            Source: [1, 1, null, 2, 2],
            Expected: [1, 1, 2, 2]),
    ];

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.WhereNotNull{TSource}(IEnumerable{TSource})" />
    /// removes nulls from each reference-type KAT row, preserving order and duplicates.
    /// </summary>
    [TestMethod]
    public void WhereNotNull_WhenReferenceKat_ShouldYieldExpectedSequence()
    {
        foreach (EnumerableKat<string?, string[]> kat in ReferenceKats)
        {
            var actual = kat.Source.WhereNotNull().ToArray();

            CollectionAssert.AreEqual(
                kat.Expected,
                actual,
                $"WhereNotNull reference KAT '{kat.Name}': filtered sequence differs from expected.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.WhereNotNull{TSource}(IEnumerable{Nullable{TSource}})" />
    /// removes nulls from each nullable-value-type KAT row and unwraps the remaining values, preserving
    /// order and duplicates.
    /// </summary>
    [TestMethod]
    public void WhereNotNull_WhenNullableValueKat_ShouldYieldExpectedSequence()
    {
        foreach (EnumerableKat<int?, int[]> kat in NullableValueKats)
        {
            var actual = kat.Source.WhereNotNull().ToArray();

            CollectionAssert.AreEqual(
                kat.Expected,
                actual,
                $"WhereNotNull nullable-value KAT '{kat.Name}': filtered sequence differs from expected.");
        }
    }
}
