// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Combinations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Emits every combination of the specified size drawn from the source sequence (the <em>k</em>-subsets), each
    /// preserving source order within the row.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to draw combinations from.</param>
    /// <param name="size">The number of elements in each combination row. Must not be negative.</param>
    /// <returns>
    /// A sequence of <em>C(n, k)</em> rows for a source of <em>n</em> elements, each row a fresh
    /// <see cref="IReadOnlyList{T}" /> snapshot of exactly <paramref name="size" /> elements in source order. When
    /// <paramref name="size" /> is 0 the result contains a single empty row; when <paramref name="size" /> exceeds the
    /// source count the result is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size" /> is negative.</exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution. Negative <paramref name="size" /> values are rejected eagerly at the call
    /// site, but the source count is only known once enumeration begins, so the <c>size &gt; count</c> contract —
    /// producing an empty sequence rather than throwing — is observed on iteration. The source is materialized into a
    /// buffer when enumeration begins and is enumerated exactly once.
    /// </para>
    /// <para>
    /// Rows are ordered lexicographically by the source positions they select: the first row is the first
    /// <paramref name="size" /> elements, and the last row is the final <paramref name="size" /> elements. Elements are
    /// treated positionally, so duplicate values produce duplicate rows. Each row is an independent snapshot —
    /// retaining or mutating one row never affects another. A <paramref name="size" /> of 0 yields exactly one empty
    /// row, matching the mathematical convention that <em>C(n, 0)</em> = 1.
    /// </para>
    /// <para>
    /// Unlike <see cref="Permutations{TSource}(IEnumerable{TSource}, int)" />, element order within a row is not
    /// significant: each subset appears exactly once, with its elements in source order.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Emit all C(4, 2) = 6 subsets of two elements.
    /// foreach (var row in new[] { 1, 2, 3, 4 }.Combinations(2))
    ///     Console.WriteLine(string.Join(", ", row));
    /// // => 1, 2
    /// // => 1, 3
    /// // => 1, 4
    /// // => 2, 3
    /// // => 2, 4
    /// // => 3, 4
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<IReadOnlyList<TSource>> Combinations<TSource>(
        this IEnumerable<TSource> source,
        int size)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNegative(size);

        return Iterator();

        IEnumerable<IReadOnlyList<TSource>> Iterator()
        {
            var items = source.ToArray();

            foreach (IReadOnlyList<TSource> row in CombinationIterator(items, size))
                yield return row;
        }
    }

    /// <summary>
    /// Generates the <paramref name="size" />-combinations of the buffered elements in lexicographic position order,
    /// allocating a fresh row array per yield.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the buffered source.</typeparam>
    /// <param name="items">The materialized source elements.</param>
    /// <param name="size">The number of elements per row; validated as non-negative by the caller.</param>
    /// <returns>
    /// The combination rows in lexicographic order of the selected source positions; a single empty row when
    /// <paramref name="size" /> is 0, and no rows when <paramref name="size" /> exceeds the element count.
    /// </returns>
    private static IEnumerable<IReadOnlyList<TSource>> CombinationIterator<TSource>(TSource[] items, int size)
    {
        if (size > items.Length)
            yield break;

        if (size == 0)
        {
            yield return Array.Empty<TSource>();
            yield break;
        }

        // indices[] holds the strictly increasing source positions of the current combination, starting at the first
        // 'size' positions. Advancing the rightmost incrementable position (and resetting those after it to the
        // smallest ascending run) walks the combinations in lexicographic position order.
        var indices = new int[size];
        for (int i = 0; i < size; i++)
            indices[i] = i;

        while (true)
        {
            var row = new TSource[size];
            for (int i = 0; i < size; i++)
                row[i] = items[indices[i]];

            yield return row;

            // Find the rightmost position that has not reached its maximum value (n - size + i).
            int pivot = size - 1;
            while (pivot >= 0 && indices[pivot] == items.Length - size + pivot)
                pivot--;

            if (pivot < 0)
                yield break;

            indices[pivot]++;
            for (int i = pivot + 1; i < size; i++)
                indices[i] = indices[i - 1] + 1;
        }
    }
}
