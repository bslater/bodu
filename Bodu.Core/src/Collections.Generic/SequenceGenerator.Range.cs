// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Range.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Generates a sequence of integers from <paramref name="start" /> to <paramref name="stop" />, inclusive.
    /// </summary>
    /// <param name="start">The first value in the sequence.</param>
    /// <param name="stop">The final value in the sequence, included in the result.</param>
    /// <returns>A sequence of consecutive integers.</returns>
    /// <remarks>If <paramref name="start" /> is greater than <paramref name="stop" />, the sequence will decrement.</remarks>
    public static IEnumerable<int> Range(int start, int stop) => SequenceGenerator.Range(start, stop, start < stop ? 1 : -1);

    /// <summary>
    /// Generates a sequence of integers from <paramref name="start" /> to <paramref name="stop" /> using a specified step value.
    /// </summary>
    /// <param name="start">The initial value in the sequence.</param>
    /// <param name="stop">The endpoint of the sequence, included if reached by stepping.</param>
    /// <param name="step">
    /// The amount to increment or decrement per step. If zero, an infinite sequence of <paramref name="start" /> is returned.
    /// </param>
    /// <returns>A sequence of integers spaced by <paramref name="step" /> units.</returns>
    /// <remarks>
    /// This method supports ascending, descending, and constant sequences depending on the sign of <paramref name="step" />. If
    /// <paramref name="step" /> is zero, the sequence yields <paramref name="start" /> indefinitely. The method safely handles edge
    /// cases near <see cref="int.MaxValue" /> and <see cref="int.MinValue" /> by terminating if overflow would occur.
    /// </remarks>
    public static IEnumerable<int> Range(int start, int stop, int step)
    {
        if (step == 0)
        {
            while (true)
                yield return start;
        }

        for (int i = start; step > 0 ? i <= stop : i >= stop;)
        {
            yield return i;

            try
            {
                i = checked(i + step);
            }
            catch (OverflowException)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Generates a fixed-length sequence of 64-bit integers starting at <paramref name="start" />.
    /// </summary>
    /// <param name="start">The initial value in the sequence.</param>
    /// <param name="count">The number of elements to produce.</param>
    /// <returns>A sequence of <paramref name="count" /> long values beginning at <paramref name="start" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative, or when the range would overflow a 64-bit signed integer.
    /// </exception>
    /// <remarks>This method avoids producing values beyond the limits of <see cref="long" /> to prevent overflow.</remarks>
    public static IEnumerable<long> Range(long start, int count)
    {
        ThrowHelper.ThrowIfLessThan(count, 0);
        ThrowHelper.ThrowIfSequenceRangeOverflows(start, count);

        for (int i = 0; i < count; i++)
            yield return start + i;
    }
}