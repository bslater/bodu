// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Batch.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0

using Bodu.Buffers;

#endif

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Batches the source sequence into subsequences of the specified size.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The size of each batch. Must be greater than 0.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> where each inner <see cref="IEnumerable{T}"/> contains up to <paramref name="size"/> elements
    /// from the source sequence. The final batch may contain fewer than <paramref name="size"/> elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than or equal to 0.</exception>
    /// <remarks>
    /// This method uses deferred execution. Enumeration of the source sequence and batches occurs only when the result is enumerated.
    /// </remarks>
    public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int size) =>
        source.Batch(size, static (item, _) => item);

    /// <summary>
    /// Projects each element of a sequence and batches the transformed elements into subsequences of the specified size.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of result elements.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The size of each batch. Must be greater than 0.</param>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> where each inner <see cref="IEnumerable{T}"/> contains up to <paramref name="size"/>
    /// transformed elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than or equal to 0.</exception>
    /// <remarks>This method uses deferred execution. The transformation and batching occur only during enumeration.</remarks>
    public static IEnumerable<IEnumerable<TResult>> Batch<TSource, TResult>(this IEnumerable<TSource> source, int size, Func<TSource, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(selector);
        return source.Batch(size, (item, _) => selector(item));
    }

    /// <summary>
    /// Projects each element of a sequence into a new form using its index, and batches the transformed elements into subsequences of
    /// the specified size.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of result elements.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The size of each batch. Must be greater than 0.</param>
    /// <param name="selector">A projection function that receives the item and its index.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> where each inner <see cref="IEnumerable{T}"/> contains up to <paramref name="size"/>
    /// transformed elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than or equal to 0.</exception>
    /// <remarks>This method uses deferred execution. The projection and batching occur only during enumeration.</remarks>
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
                TResult[] batch = new TResult[size];
                int count = 0;

                do
                {
                    batch[count++] = selector(enumerator.Current, index++);
                } while (count < size && enumerator.MoveNext());

                // Trim the final batch if it did not fill completely, to avoid exposing
                // unused default(T) slots to callers.
                if (count < size)
                    Array.Resize(ref batch, count);

                yield return batch;
            }
        }
    }

#if !NETSTANDARD2_0

    /// <summary>
    /// Batches and transforms a sequence using a pooled array to reduce allocations.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of transformed result.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The maximum number of items per batch.</param>
    /// <param name="selector">A projection function that receives the source item and its index.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="ReadOnlyMemory{TResult}"/> batches, backed by a pooled buffer to reduce
    /// per-batch allocations.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than or equal to 0.</exception>
    /// <remarks>
    /// <para>
    /// Each yielded batch is a snapshot copied from the pooled buffer at the point of yield. If you need to retain a batch beyond the
    /// current iteration step, copy it using <c>.ToArray()</c> before advancing the enumerator.
    /// </para>
    /// <para>
    /// This method should be consumed via <c>foreach</c>. Each call creates a new pooled buffer for the duration of the enumeration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// <![CDATA[
    /// Input:  Enumerable.Range(1, 10)
    /// Batch size: 4
    /// Selector: (x, i) => $"Item {i}: {x}"
    ///
    /// Expected output:
    /// Batch 1: "Item 0: 1", "Item 1: 2", "Item 2: 3", "Item 3: 4"
    /// Batch 2: "Item 4: 5", "Item 5: 6", "Item 6: 7", "Item 7: 8"
    /// Batch 3: "Item 8: 9", "Item 9: 10"
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

#if NET5_0_OR_GREATER || NETSTANDARD2_1

        IEnumerable<ReadOnlyMemory<TResult>> BatchIterator()
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();
            PooledBufferBuilder<TResult> buffer = new PooledBufferBuilder<TResult>(size);
            int index = 0;

            try
            {
                while (enumerator.MoveNext())
                {
                    buffer.Append(selector(enumerator.Current, index++));

                    if (buffer.Count == size)
                    {
                        // Snapshot the batch before resetting the buffer for the next batch.
                        // If PooledBufferBuilder exposes a Reset() or Clear() method that retains
                        // the rented array, prefer that over Dispose + re-create to avoid a pool
                        // round-trip on every batch boundary.
                        yield return buffer.AsSpan().ToArray();
                        buffer.Dispose();
                        buffer = new PooledBufferBuilder<TResult>(size);
                    }
                }

                if (buffer.Count > 0)
                    yield return buffer.AsSpan().ToArray();
            }
            finally
            {
                buffer.Dispose();
            }
        }

#else
#error BatchPooled is only supported on netstandard2.1 or greater.
#endif
    }

    /// <summary>
    /// Batches a sequence using a pooled array to reduce allocations.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to batch.</param>
    /// <param name="size">The maximum number of items per batch.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="ReadOnlyMemory{TSource}"/> batches, backed by a pooled buffer to reduce
    /// per-batch allocations.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="size"/> is less than or equal to 0.</exception>
    /// <remarks>
    /// This overload returns untransformed batches of the original element type. See
    /// <see cref="BatchPooled{TSource,TResult}(IEnumerable{TSource},int,Func{TSource,int,TResult})"/> for a variant that applies a
    /// projection.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// Input:  Enumerable.Range(1, 9)
    /// Batch size: 3
    ///
    /// Expected output:
    /// Batch 1: 1, 2, 3
    /// Batch 2: 4, 5, 6
    /// Batch 3: 7, 8, 9
    /// var source = Enumerable.Range(1, 9);
    /// foreach (var batch in source.BatchPooled(3))
    /// {
    ///     Console.WriteLine(string.Join(", ", batch.ToArray()));
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<ReadOnlyMemory<TSource>> BatchPooled<TSource>(
            this IEnumerable<TSource> source,
            int size) => source.BatchPooled(size, static (x, _) => x);
}

#endif
