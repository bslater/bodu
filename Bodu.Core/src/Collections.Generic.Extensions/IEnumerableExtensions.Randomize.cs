// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Randomize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0

using Bodu.Buffers;

#endif

using System;
using System.Collections.Generic;
using System.Linq;

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <inheritdoc cref="Randomize{T}(IEnumerable{T}, RandomizationMode, IRandomGenerator, int?)"/>
    public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source) =>
        source.Randomize(RandomizationMode.BufferAll, new SystemRandomAdapter(), null);

    /// <summary>
    /// Randomises the source sequence using a specified strategy and random number generator.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to randomise.</param>
    /// <param name="mode">The randomisation strategy to apply.</param>
    /// <param name="rng">The random number generator to use.</param>
    /// <param name="count">The number of items to return; returns all items when <see langword="null"/>.</param>
    /// <returns>A randomised sequence of <typeparamref name="T"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="rng"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="count"/> is negative, exceeds the number of available elements, or if <paramref name="mode"/> is not
    /// a defined <see cref="RandomizationMode"/> value.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="count"/> is <see langword="null"/> and <paramref name="mode"/> requires a count (i.e.
    /// <see cref="RandomizationMode.ReservoirSample"/> or <see cref="RandomizationMode.LazyShuffle"/>).
    /// </exception>
    public static IEnumerable<T> Randomize<T>(
        this IEnumerable<T> source,
        RandomizationMode mode,
        IRandomGenerator rng,
        int? count = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(rng);
        ThrowHelper.ThrowIfLessThan(count, 0);

        return mode switch
        {
            RandomizationMode.BufferAll => RandomizeBuffered(source, rng, count),
            RandomizationMode.ReservoirSample => count is null
                ? throw new ArgumentException(
                    string.Format(ResourceStrings.Arg_Required_ParameterRequiredIf, nameof(count), nameof(mode), nameof(RandomizationMode.ReservoirSample)), nameof(count))
                : ReservoirSample(source, rng, count.Value),
            RandomizationMode.StreamWindowed => StreamWindowedShuffle(source, rng),
            RandomizationMode.LazyShuffle => count is null
                ? throw new ArgumentException(
                    string.Format(ResourceStrings.Arg_Required_ParameterRequiredIf, nameof(count), nameof(mode), nameof(RandomizationMode.LazyShuffle)), nameof(count))
                : LazyShuffle(source, rng, count.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), string.Format(ResourceStrings.Arg_OutOfRangeException_EnumValue, nameof(RandomizationMode)))
        };
    }

    /// <summary>
    /// Fills a fixed-size reservoir array from the supplied enumerator and applies standard reservoir sampling
    /// to ensure a uniform random selection when the source contains more than <paramref name="count"/> elements.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="enumerator">An open enumerator positioned before the first element to consume.</param>
    /// <param name="rng">The random number generator.</param>
    /// <param name="count">The number of elements to select into the reservoir.</param>
    /// <returns>A <typeparamref name="T"/> array of length <paramref name="count"/> containing the sampled elements.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the source sequence contains fewer than <paramref name="count"/> elements.
    /// </exception>
    /// <remarks>
    /// The caller is responsible for opening and disposing the enumerator. This method consumes the enumerator to exhaustion.
    /// </remarks>
    private static T[] FillReservoir<T>(IEnumerator<T> enumerator, IRandomGenerator rng, int count)
    {
        T[] reservoir = new T[count];
        int index = 0;

        // Fill the reservoir with the first 'count' elements from the source
        while (index < count && enumerator.MoveNext())
            reservoir[index++] = enumerator.Current;

        if (index < count)
            throw new ArgumentOutOfRangeException(nameof(count), ResourceStrings.Arg_OutOfRange_CountGreaterThanSource);

        // Apply Knuth/Fisher-Yates reservoir sampling over the remaining elements.
        // Each element at position 'seen' has an equal probability of being selected.
        int seen = count;
        while (enumerator.MoveNext())
        {
            int j = rng.Next(++seen);
            if (j < count)
                reservoir[j] = enumerator.Current;
        }

        return reservoir;
    }

    /// <summary>
    /// Lazily builds a reservoir sample of <paramref name="count"/> elements from the source sequence and yields them in shuffled order.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to sample from.</param>
    /// <param name="rng">The random number generator.</param>
    /// <param name="count">The number of elements to return.</param>
    /// <returns>A lazily shuffled sequence of <paramref name="count"/> elements selected with uniform probability.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> exceeds the number of available elements.</exception>
    private static IEnumerable<T> LazyShuffle<T>(IEnumerable<T> source, IRandomGenerator rng, int count)
    {
        if (count <= 0)
            yield break;

        using IEnumerator<T> enumerator = source.GetEnumerator();
        T[] reservoir = FillReservoir(enumerator, rng, count);

        foreach (T? item in ShuffleHelpers.ShuffleAndYield(reservoir, rng, count))
            yield return item;
    }

    /// <summary>
    /// Buffers all elements from the source and yields a randomised subset using in-place shuffling.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to randomise.</param>
    /// <param name="rng">The random number generator.</param>
    /// <param name="count">The number of items to return; all items when <see langword="null"/>.</param>
    /// <returns>A shuffled subset of the source sequence.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> exceeds the number of available items.</exception>
    private static IEnumerable<T> RandomizeBuffered<T>(IEnumerable<T> source, IRandomGenerator rng, int? count)
    {
        IList<T> buffer;
        int availableCount;

#if NETSTANDARD2_0
        var list = source is ICollection<T> col ? new List<T>(col) : new List<T>(source);
        buffer = list;
        availableCount = list.Count;
#else
        using PooledBufferBuilder<T> builder = new PooledBufferBuilder<T>();

        if (source is IReadOnlyCollection<T> collection && builder.TryCopyFrom(collection))
        {
            buffer = builder.AsArray();
            availableCount = builder.Count;
        }
        else
        {
            builder.AppendRange(source);
            buffer = builder.AsArray();
            availableCount = builder.Count;
        }
#endif

        int takeCount = count ?? availableCount;
        ThrowHelper.ThrowIfGreaterThanOther(takeCount, availableCount);

        if (takeCount == availableCount)
            return ShuffleHelpers.ShuffleAndYield(buffer, rng);
        else
            return ShuffleHelpers.ShuffleAndYield(buffer.ToArray(), rng, takeCount);
    }

    /// <summary>
    /// Performs reservoir sampling to select a uniformly random subset of <paramref name="count"/> elements from the source sequence,
    /// then yields them in shuffled order.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence to sample from.</param>
    /// <param name="rng">The random number generator.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>A shuffled sequence of <paramref name="count"/> uniformly sampled elements.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> exceeds the number of available items.</exception>
    private static IEnumerable<T> ReservoirSample<T>(IEnumerable<T> source, IRandomGenerator rng, int count)
    {
        using IEnumerator<T> enumerator = source.GetEnumerator();
        T[] reservoir = FillReservoir(enumerator, rng, count);
        return ShuffleHelpers.ShuffleAndYield(reservoir, rng, count);
    }

    /// <summary>
    /// Shuffles elements from the source sequence using a fixed-size sliding window, yielding elements one at a time whilst streaming.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to randomise.</param>
    /// <param name="rng">The random number generator.</param>
    /// <param name="windowSize">
    /// The size of the rolling window used during shuffling. A larger window increases randomness at the cost of latency before the
    /// first element is yielded. Defaults to 64. Must be greater than zero.
    /// </param>
    /// <returns>A randomised sequence of elements produced using a sliding window strategy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="windowSize"/> is zero or negative.</exception>
    /// <remarks>
    /// <para>
    /// The window is filled with the first <paramref name="windowSize"/> elements before any output is produced. Thereafter, for each
    /// incoming element a random slot in the window is selected, its current occupant is yielded, and the new element takes its place.
    /// This approach avoids any per-element allocation inside the streaming loop.
    /// </para>
    /// <para>
    /// Once the source is exhausted, the remaining window contents are flushed via a final in-place shuffle.
    /// </para>
    /// </remarks>
    private static IEnumerable<T> StreamWindowedShuffle<T>(IEnumerable<T> source, IRandomGenerator rng, int windowSize = 64)
    {
        // Guard: a window of zero or fewer elements is meaningless and would cause rng.Next(0) to throw.
        ThrowHelper.ThrowIfZeroOrNegative(windowSize);

        T[] window = new T[windowSize];
        int count = 0;

        using IEnumerator<T> enumerator = source.GetEnumerator();

        // Fill the initial window up to windowSize elements before streaming begins.
        while (count < windowSize && enumerator.MoveNext())
            window[count++] = enumerator.Current;

        // For each subsequent source element: pick a random occupied slot, yield its current
        // occupant, then place the new element in that slot. No heap allocation occurs here.
        while (enumerator.MoveNext())
        {
            int i = rng.Next(count);
            yield return window[i];
            window[i] = enumerator.Current;
        }

        // Flush all remaining window elements with a final in-place shuffle.
        foreach (T item in ShuffleHelpers.ShuffleAndYield(window, rng, count))
            yield return item;
    }
}
