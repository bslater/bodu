// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpers.Shuffle.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Bodu.Collections.Generic;

public static partial class ShuffleHelpers
{
    /// <summary>
    /// Performs an in-place Fisher–Yates shuffle over the provided array.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="array">The array of elements to shuffle.</param>
    /// <param name="rng">The random number generator used to shuffle elements.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="array" /> or <paramref name="rng" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This method modifies the original array using the Fisher–Yates algorithm. Each element has an equal probability of ending up in
    /// any position.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(T[] array, IRandomGenerator rng)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfNull(rng);

        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

#if !NETSTANDARD2_0

    /// <summary>
    /// Performs an in-place Fisher–Yates shuffle over a span of elements.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the span.</typeparam>
    /// <param name="span">The span of elements to shuffle.</param>
    /// <param name="rng">The random number generator used to shuffle elements.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="rng" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This method modifies the span in-place using the Fisher–Yates algorithm. It is optimized for shuffling stack-allocated or pooled
    /// data, and does not allocate memory.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(Span<T> span, IRandomGenerator rng)
    {
        ThrowHelper.ThrowIfNull(rng);

        for (int i = span.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (span[i], span[j]) = (span[j], span[i]);
        }
    }

    /// <summary>
    /// Performs an in-place Fisher–Yates shuffle over a memory region.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the memory block.</typeparam>
    /// <param name="memory">The memory region to shuffle.</param>
    /// <param name="rng">The random number generator used to shuffle elements.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="rng" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// This method shuffles the contents of the <paramref name="memory" /> region in-place by accessing its underlying span. The
    /// original memory region is modified.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Shuffle<T>(Memory<T> memory, IRandomGenerator rng)
    {
        ThrowHelper.ThrowIfNull(rng);
        Shuffle(memory.Span, rng);
    }

#endif
}