// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Batch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Projects each element of a sequence and batches the transformed elements into subsequences of the specified
    /// size.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of result elements.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The size of each batch. Must be greater than 0.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> where each inner <see cref="IEnumerable{T}" /> contains up to
    /// <paramref name="size" /> transformed elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="size" /> is less than or equal to 0.
    /// </exception>
    /// <remarks>
    /// This method uses deferred execution. The transformation and batching occur only during enumeration. For plain,
    /// unprojected batching, use <see cref="System.Linq.Enumerable.Chunk{TSource}(IEnumerable{TSource}, int)" />.
    /// </remarks>
    public static IEnumerable<IEnumerable<TResult>> Batch<TSource, TResult>(this IEnumerable<TSource> source, int size, Func<TSource, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(selector);
        return source.Batch(size, (item, _) => selector(item));
    }

    /// <summary>
    /// Projects each element of a sequence into a new form using its index, and batches the transformed elements into
    /// subsequences of the specified size.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of result elements.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The size of each batch. Must be greater than 0.</param>
    /// <param name="selector">A projection function that receives the item and its index.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> where each inner <see cref="IEnumerable{T}" /> contains up to
    /// <paramref name="size" /> transformed elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="size" /> is less than or equal to 0.
    /// </exception>
    /// <remarks>
    /// This method uses deferred execution. The projection and batching occur only during enumeration.
    /// </remarks>
    public static IEnumerable<IEnumerable<TResult>> Batch<TSource, TResult>(
        this IEnumerable<TSource> source,
        int size,
        Func<TSource, int, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);
        ThrowHelper.ThrowIfOutOfRange(size, 1, int.MaxValue);

        // Use a local iterator to enable deferred execution. This ensures the source is not evaluated until enumeration begins.
        return BatchIterator();

        IEnumerable<IEnumerable<TResult>> BatchIterator()
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            int index = 0; // Tracks the global index across all batches

            while (enumerator.MoveNext())
            {
                // Allocate a fixed-size array for the current batch; size is known up-front.
                var batch = new TResult[size];
                int count = 0;

                do
                {
                    batch[count++] = selector(enumerator.Current, index++);
                }
                while (count < size && enumerator.MoveNext());

                // Trim the final batch if it did not fill completely, to avoid exposing
                // unused default(T) slots to callers.
                if (count < size)
                    Array.Resize(ref batch, count);

                yield return batch;
            }
        }
    }

    /// <summary>
    /// Batches and transforms a sequence using a pooled array to reduce allocations.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of transformed result.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The maximum number of items per batch.</param>
    /// <param name="selector">A projection function that receives the source item and its index.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> of <see cref="ReadOnlyMemory{TResult}" /> batches that alias a single pooled
    /// buffer, so a full enumeration allocates nothing per batch.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="size" /> is less than or equal to 0.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Every yielded batch is a window over the <em>same</em> pooled buffer, which is overwritten by the next iteration
    /// step and returned to the pool when enumeration ends. A batch is therefore valid only until the enumerator
    /// advances (or is disposed): to retain one, copy it with <c>.ToArray()</c> before advancing. Retaining the
    /// <see cref="ReadOnlyMemory{T}" /> values themselves — for example via <c>ToList()</c> on the returned sequence —
    /// observes overwritten or recycled data. For independently owned batches, use
    /// <see cref="System.Linq.Enumerable.Chunk{TSource}(IEnumerable{TSource}, int)" /> or the projecting
    /// <see cref="Batch{TSource, TResult}(IEnumerable{TSource}, int, Func{TSource, TResult})" /> overload instead.
    /// </para>
    /// <para>
    /// This method should be consumed via <c>foreach</c>. Each enumeration rents one buffer of <paramref name="size" />
    /// elements for its duration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Input:  Enumerable.Range(1, 10)
    /// // Batch size: 4
    /// // Selector: (x, i) => $"Item {i}: {x}"
    ///
    /// // Expected output:
    /// // Batch 1: "Item 0: 1", "Item 1: 2", "Item 2: 3", "Item 3: 4"
    /// // Batch 2: "Item 4: 5", "Item 5: 6", "Item 6: 7", "Item 7: 8"
    /// // Batch 3: "Item 8: 9", "Item 9: 10"
    /// var source = Enumerable.Range(1, 10);
    /// foreach (var batch in source.BatchPooled(4, (x, i) => $"Item {i}: {x}"))
    /// {
    ///     Console.WriteLine($"[{string.Join(", ", batch.ToArray())}]");
    /// }
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<ReadOnlyMemory<TResult>> BatchPooled<TSource, TResult>(
            this IEnumerable<TSource> source,
            int size,
            Func<TSource, int, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);
        ThrowHelper.ThrowIfOutOfRange(size, 1, int.MaxValue);

        return BatchIterator();

        IEnumerable<ReadOnlyMemory<TResult>> BatchIterator()
        {
            // One rental for the whole enumeration; every batch is a window over it. Yielding the live window rather
            // than a snapshot is the point of this operator — the copying variant is plain Batch.
            TResult[] buffer = System.Buffers.ArrayPool<TResult>.Shared.Rent(size);
            try
            {
                int filled = 0;
                int index = 0;
                foreach (TSource item in source)
                {
                    buffer[filled++] = selector(item, index++);

                    if (filled == size)
                    {
                        yield return buffer.AsMemory(0, filled);
                        filled = 0;
                    }
                }

                if (filled > 0)
                    yield return buffer.AsMemory(0, filled);
            }
            finally
            {
                System.Buffers.ArrayPool<TResult>.Shared.Return(
                    buffer,
                    clearArray: System.Runtime.CompilerServices.RuntimeHelpers.IsReferenceOrContainsReferences<TResult>());
            }
        }
    }

    /// <summary>
    /// Batches a sequence using a pooled array to reduce allocations.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The maximum number of items per batch.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}" /> of <see cref="ReadOnlyMemory{TSource}" /> batches that alias a single pooled
    /// buffer, so a full enumeration allocates nothing per batch.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="size" /> is less than or equal to 0.
    /// </exception>
    /// <remarks>
    /// This overload returns untransformed batches of the original element type. Each batch is valid only until the
    /// enumerator advances — see
    /// <see cref="BatchPooled{TSource,TResult}(IEnumerable{TSource},int,Func{TSource,int,TResult})" /> for the full
    /// lifetime contract and for a variant that applies a projection.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Input:  Enumerable.Range(1, 9)
    /// // Batch size: 3
    ///
    /// // Expected output:
    /// // Batch 1: 1, 2, 3
    /// // Batch 2: 4, 5, 6
    /// // Batch 3: 7, 8, 9
    ///
    /// var source = Enumerable.Range(1, 9);
    /// foreach (var batch in source.BatchPooled(3))
    /// {
    ///     Console.WriteLine(string.Join(", ", batch.ToArray()));
    /// }
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<ReadOnlyMemory<TSource>> BatchPooled<TSource>(
            this IEnumerable<TSource> source,
            int size) => source.BatchPooled(size, static (x, _) => x);
}
