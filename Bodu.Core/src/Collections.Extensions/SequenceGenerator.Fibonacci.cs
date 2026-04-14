// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Fibonacci.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Generates the Fibonacci sequence values within the specified inclusive range.
    /// </summary>
    /// <param name="min">The inclusive minimum value for the sequence.</param>
    /// <param name="max">The exclusive maximum value for the sequence.</param>
    /// <returns>An enumerable of Fibonacci numbers between <paramref name="min"/> and <paramref name="max"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if either <paramref name="min"/> or <paramref name="max"/> is negative.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="min"/> is greater than <paramref name="max"/>.</exception>
    public static IEnumerable<long> Fibonacci(long min, long max)
    {
        ThrowHelper.ThrowIfLessThan(min, 0);
        ThrowHelper.ThrowIfLessThan(max, 0);
        ThrowHelper.ThrowIfGreaterThanOther(min, max);

        long prev = -1, next = 1;

        while (true)
        {
            long sum;
            try
            {
                checked { sum = prev + next; }
            }
            catch (OverflowException)
            {
                yield break; // stop gracefully on overflow
            }

            if (sum >= max)
                yield break;

            prev = next;
            next = sum;

            if (sum >= min)
                yield return sum;
        }
    }
}