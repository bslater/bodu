// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Windowed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Emits every complete, overlapping fixed-size window over the source sequence, advancing one element at a time.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to slide a window over.</param>
    /// <param name="size">The number of elements in each window. Must be greater than 0.</param>
    /// <returns>
    /// A sequence of windows, each a stable snapshot of exactly <paramref name="size" /> consecutive elements. The
    /// result is empty when <paramref name="source" /> contains fewer than <paramref name="size" /> elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="size" /> is less than 1.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution and enumerates the source exactly once, buffering at most
    /// <paramref name="size" /> elements. Only complete windows are returned, and each is a stable snapshot that is not
    /// modified when the enumerator advances.
    /// </para>
    /// <para>
    /// This method differs from <see cref="Batch{TSource}(IEnumerable{TSource}, int)" />: batching returns
    /// non-overlapping groups, whereas this method returns overlapping sliding windows.
    /// </para>
    /// </remarks>
    public static IEnumerable<IReadOnlyList<TSource>> Windowed<TSource>(
        this IEnumerable<TSource> source,
        int size) =>
        source.Windowed(size, static window => window);

    /// <summary>
    /// Applies a projection to every complete, overlapping fixed-size window over the source sequence, advancing one
    /// element at a time.
    /// </summary>
    /// <typeparam name="TSource">The type of elements in the source sequence.</typeparam>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="source">The source sequence to slide a window over.</param>
    /// <param name="size">The number of elements in each window. Must be greater than 0.</param>
    /// <param name="selector">A projection applied to each complete window.</param>
    /// <returns>
    /// A sequence containing the result of <paramref name="selector" /> applied to each complete window of exactly
    /// <paramref name="size" /> consecutive elements. The result is empty when <paramref name="source" /> contains fewer
    /// than <paramref name="size" /> elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="selector" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="size" /> is less than 1.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution and enumerates the source exactly once, buffering at most
    /// <paramref name="size" /> elements. The first result is not produced until <paramref name="size" /> elements have
    /// been consumed; each subsequent result consumes one additional element.
    /// </para>
    /// <para>
    /// The window passed to <paramref name="selector" /> is a stable snapshot; retaining it is safe after the enumerator
    /// advances.
    /// </para>
    /// </remarks>
    public static IEnumerable<TResult> Windowed<TSource, TResult>(
        this IEnumerable<TSource> source,
        int size,
        Func<IReadOnlyList<TSource>, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(selector);
        ThrowHelper.ThrowIfOutOfRange(size, 1, int.MaxValue);

        return Iterator();

        IEnumerable<TResult> Iterator()
        {
            using IEnumerator<TSource> enumerator = source.GetEnumerator();

            // Fill the ring buffer with the first window before emitting anything.
            var ring = new TSource[size];
            int count = 0;

            while (count < size && enumerator.MoveNext())
                ring[count++] = enumerator.Current;

            if (count < size)
                yield break;

            int start = 0;

            while (true)
            {
                yield return selector(CopyWindow(ring, start, size));

                if (!enumerator.MoveNext())
                    yield break;

                // Overwrite the oldest slot with the newest element and rotate the window forward by one.
                ring[start] = enumerator.Current;
                start = (start + 1) % size;
            }
        }

        static TSource[] CopyWindow(TSource[] ring, int start, int size)
        {
            var snapshot = new TSource[size];
            int right = Math.Min(size, ring.Length - start);
            Array.Copy(ring, start, snapshot, 0, right);
            if (right < size)
                Array.Copy(ring, 0, snapshot, right, size - right);

            return snapshot;
        }
    }
}
