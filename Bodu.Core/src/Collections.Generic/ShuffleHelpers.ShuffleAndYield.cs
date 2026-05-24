// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpers.ShuffleAndYield.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Bodu.Collections.Extensions;

namespace Bodu.Collections.Generic;

public static partial class ShuffleHelpers
{
    /// <summary>
    /// Lazily yields a randomized subset of an <see cref="IEnumerable{T}" /> using a partial Fisher–Yates shuffle.
    /// </summary>
    /// <typeparam name="T">The type of elements in the source sequence.</typeparam>
    /// <param name="source">The source sequence to shuffle.</param>
    /// <param name="rng">The random number generator used to select shuffled items.</param>
    /// <param name="count">The number of elements to yield from the shuffled source.</param>
    /// <returns>A lazily-evaluated sequence of randomly selected items from the source.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="rng" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown immediately if <paramref name="count" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// May be thrown upon first enumeration if <paramref name="count" /> exceeds the total number of elements in
    /// <paramref name="source" />.
    /// </exception>
    /// <remarks>
    /// Enumeration of <paramref name="source" /> and shuffling are deferred until the result is first iterated. The
    /// entire source is buffered in memory before shuffling begins. The original sequence is not modified.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ShuffleAndYield<T>(IEnumerable<T> source, IRandomGenerator rng, int count)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(rng);
        ThrowHelper.ThrowIfLessThan(count, 0);

        return LazyIterator();

        IEnumerable<T> LazyIterator()
        {
            T[] buffer = [.. source]; // Copy the source array to avoid modifying it
            ThrowHelper.ThrowIfCountExceedsAvailable(count, buffer.Length);

            var available = buffer.Length;

            while (count-- > 0)
            {
                var i = rng.Next(available);
                yield return buffer[i];
                buffer[i] = buffer[--available]; // Replace used item with the last unselected one
            }
        }
    }

    /// <summary>
    /// Yields a randomized subset of the specified array by copying and shuffling it using a partial Fisher–Yates
    /// algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The source array to shuffle. The original array is not modified.</param>
    /// <param name="rng">The random number generator used for shuffling.</param>
    /// <param name="count">The number of elements to yield.</param>
    /// <returns>A sequence of randomly selected elements from the array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> or <paramref name="rng" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="count" /> is negative or exceeds the array length.
    /// </exception>
    /// <remarks>
    /// The input array is copied before shuffling to ensure immutability of the original. Use this method when working
    /// with arrays and requiring source preservation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ShuffleAndYield<T>(T[] array, IRandomGenerator rng, int count)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNull(rng);
        ThrowHelper.ThrowIfCountExceedsAvailable(count, array.Length);

        T[] buffer = [.. array]; // Copy the source array to avoid modifying it
        var available = buffer.Length;

        while (count-- > 0)
        {
            var i = rng.Next(available);
            yield return buffer[i];
            buffer[i] = buffer[--available]; // Replace used item with the last unselected one
        }
    }

    /// <summary>
    /// Lazily yields a fully shuffled sequence from the given source.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence to shuffle.</param>
    /// <param name="rng">The random number generator.</param>
    /// <returns>A lazily-evaluated, fully shuffled sequence.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ShuffleAndYield<T>(IEnumerable<T> source, IRandomGenerator rng)
    {
        ThrowHelper.ThrowIfNull(source);
        return ShuffleAndYield(source, rng, source.CountOrDefault());
    }

    /// <summary>
    /// Yields a fully shuffled copy of the array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="array">The array to shuffle.</param>
    /// <param name="rng">The random number generator.</param>
    /// <returns>A shuffled sequence based on the array.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="array" /> is <see langword="null" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ShuffleAndYield<T>(T[] array, IRandomGenerator rng)
    {
        ThrowHelper.ThrowIfNull(array);
        return ShuffleAndYield(array, rng, array.Length);
    }

#if !NETSTANDARD2_0

    /// <summary>
    /// Yields a fully shuffled copy of the span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="span">The span to shuffle.</param>
    /// <param name="rng">The random number generator.</param>
    /// <returns>A shuffled sequence from the span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ShuffleAndYield<T>(ReadOnlySpan<T> span, IRandomGenerator rng) => ShuffleAndYield(span, rng, span.Length);

    /// <summary>
    /// Yields a randomized subset of a <see cref="ReadOnlySpan{T}" /> using a copied buffer and array shuffle.
    /// </summary>
    /// <typeparam name="T">The type of elements in the span.</typeparam>
    /// <param name="span">The span to shuffle.</param>
    /// <param name="rng">The random number generator to use.</param>
    /// <param name="count">The number of elements to yield.</param>
    /// <returns>A sequence of shuffled items from the span.</returns>
    /// <remarks>
    /// This method eagerly copies <paramref name="span" /> into a new array, which is then shuffled.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ShuffleAndYield<T>(ReadOnlySpan<T> span, IRandomGenerator rng, int count) => ShuffleAndYield(span.ToArray(), rng, count);

    /// <summary>
    /// Yields a fully shuffled copy of the memory block.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="memory">The memory block to shuffle.</param>
    /// <param name="rng">The random number generator.</param>
    /// <returns>A shuffled sequence from the memory block.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ShuffleAndYield<T>(Memory<T> memory, IRandomGenerator rng) => ShuffleAndYield(memory, rng, memory.Length);

    /// <summary>
    /// Yields a randomized subset of a <see cref="Memory{T}" /> block using a copied buffer and array shuffle.
    /// </summary>
    /// <typeparam name="T">The type of elements in memory.</typeparam>
    /// <param name="memory">The memory block to shuffle.</param>
    /// <param name="rng">The random number generator to use.</param>
    /// <param name="count">The number of elements to yield.</param>
    /// <returns>A sequence of shuffled items from the memory block.</returns>
    /// <remarks>
    /// This method eagerly copies <paramref name="memory" /> into a new array, which is then shuffled.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ShuffleAndYield<T>(Memory<T> memory, IRandomGenerator rng, int count) => ShuffleAndYield(memory.ToArray(), rng, count);

#endif

    /// <summary>
    /// Yields a randomized subset of the specified array using an in-place partial Fisher–Yates shuffle.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The buffer to shuffle. The contents are modified during shuffling.</param>
    /// <param name="rng">The random number generator used to shuffle elements.</param>
    /// <param name="count">The number of elements to yield.</param>
    /// <returns>A sequence of shuffled items from the input array.</returns>
    /// <remarks>
    /// This method assumes that <paramref name="array" /> is safe to mutate. It does not perform validation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IEnumerable<T> ShuffleAndYieldInternal<T>(T[] array, IRandomGenerator rng, int count)
    {
        var available = array.Length;

        while (count-- > 0)
        {
            var i = rng.Next(available);
            yield return array[i];
            array[i] = array[--available]; // Replace used item with the last unselected one
        }
    }
}
