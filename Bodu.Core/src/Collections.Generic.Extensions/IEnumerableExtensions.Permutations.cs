// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Permutations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Emits every full permutation of the source sequence, one row per distinct ordering of all elements.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to permute.</param>
    /// <returns>
    /// A sequence of <em>n</em>! rows for a source of <em>n</em> elements, each row a fresh
    /// <see cref="IReadOnlyList{T}" /> snapshot containing all elements in one ordering. An empty source yields a
    /// single empty row (the empty permutation).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution. The source is materialized into a buffer when enumeration of the result
    /// begins and is enumerated exactly once; rows are then generated on demand without precomputing the full set.
    /// </para>
    /// <para>
    /// Rows are ordered lexicographically by the source positions they select: the first row preserves source order,
    /// and each subsequent row is the next arrangement in position order. Elements are treated positionally, so
    /// duplicate values produce duplicate rows. Each row is an independent snapshot — retaining or mutating one row
    /// never affects another.
    /// </para>
    /// <para>
    /// The row count grows factorially with the source length; bound the result with
    /// <see cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)" /> when the source may be large.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Emit all 3! orderings of the source.
    /// foreach (var row in new[] { 1, 2, 3 }.Permutations())
    ///     Console.WriteLine(string.Join(", ", row));
    /// // => 1, 2, 3
    /// // => 1, 3, 2
    /// // => 2, 1, 3
    /// // => 2, 3, 1
    /// // => 3, 1, 2
    /// // => 3, 2, 1
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<IReadOnlyList<TSource>> Permutations<TSource>(this IEnumerable<TSource> source)
    {
        ThrowHelper.ThrowIfNull(source);

        return Iterator();

        IEnumerable<IReadOnlyList<TSource>> Iterator()
        {
            var items = source.ToArray();

            foreach (IReadOnlyList<TSource> row in PermutationIterator(items, items.Length))
                yield return row;
        }
    }

    /// <summary>
    /// Emits every permutation of the specified size drawn from the source sequence (the <em>k</em>-permutations).
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to permute.</param>
    /// <param name="size">The number of elements in each permutation row. Must not be negative.</param>
    /// <returns>
    /// A sequence of <em>P(n, k)</em> rows for a source of <em>n</em> elements, each row a fresh
    /// <see cref="IReadOnlyList{T}" /> snapshot of exactly <paramref name="size" /> elements. When
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
    /// Rows are ordered lexicographically by the source positions they select. Elements are treated positionally, so
    /// duplicate values produce duplicate rows. Each row is an independent snapshot — retaining or mutating one row
    /// never affects another. A <paramref name="size" /> of 0 yields exactly one empty row, matching the mathematical
    /// convention that <em>P(n, 0)</em> = 1.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Emit all P(3, 2) = 6 ordered selections of two elements.
    /// foreach (var row in new[] { 1, 2, 3 }.Permutations(2))
    ///     Console.WriteLine(string.Join(", ", row));
    /// // => 1, 2
    /// // => 1, 3
    /// // => 2, 1
    /// // => 2, 3
    /// // => 3, 1
    /// // => 3, 2
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<IReadOnlyList<TSource>> Permutations<TSource>(
        this IEnumerable<TSource> source,
        int size)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNegative(size);

        return Iterator();

        IEnumerable<IReadOnlyList<TSource>> Iterator()
        {
            var items = source.ToArray();

            foreach (IReadOnlyList<TSource> row in PermutationIterator(items, size))
                yield return row;
        }
    }

    /// <summary>
    /// Generates the <paramref name="size" />-permutations of the buffered elements in lexicographic position order,
    /// allocating a fresh row array per yield.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the buffered source.</typeparam>
    /// <param name="items">The materialized source elements.</param>
    /// <param name="size">The number of elements per row; validated as non-negative by the caller.</param>
    /// <returns>
    /// The permutation rows in lexicographic order of the selected source positions; a single empty row when
    /// <paramref name="size" /> is 0, and no rows when <paramref name="size" /> exceeds the element count.
    /// </returns>
    private static IEnumerable<IReadOnlyList<TSource>> PermutationIterator<TSource>(TSource[] items, int size)
    {
        if (size > items.Length)
            yield break;

        if (size == 0)
        {
            yield return Array.Empty<TSource>();
            yield break;
        }

        // Iterative backtracking over source positions: indices[d] is the position chosen at depth d, and used[]
        // tracks which positions are taken by shallower depths. Advancing each depth to the next free position in
        // ascending order yields rows in lexicographic position order.
        var indices = new int[size];
        var used = new bool[items.Length];
        int depth = 0;
        indices[0] = -1;

        while (depth >= 0)
        {
            // Release the position currently held at this depth, then advance to the next free one.
            int candidate = indices[depth];
            if (candidate >= 0)
                used[candidate] = false;

            do
            {
                candidate++;
            }
            while (candidate < items.Length && used[candidate]);

            if (candidate == items.Length)
            {
                // No further position available at this depth — backtrack.
                depth--;
                continue;
            }

            indices[depth] = candidate;
            used[candidate] = true;

            if (depth == size - 1)
            {
                var row = new TSource[size];
                for (int i = 0; i < size; i++)
                    row[i] = items[indices[i]];

                yield return row;
            }
            else
            {
                depth++;
                indices[depth] = -1;
            }
        }
    }
}
