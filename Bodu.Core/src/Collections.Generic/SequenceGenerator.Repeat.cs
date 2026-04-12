// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Repeat.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Generates an infinite sequence repeating the specified value.
    /// </summary>
    /// <typeparam name="T">The type of the repeated value.</typeparam>
    /// <param name="value">The value to repeat in the sequence.</param>
    /// <returns>An infinite sequence of <paramref name="value" />.</returns>
    /// <remarks>This method yields the same value forever and is lazily evaluated.</remarks>
    public static IEnumerable<T> Repeat<T>(T value)
    {
        while (true)
            yield return value;
    }

    /// <summary>
    /// Generates a finite sequence repeating the specified value a given number of times.
    /// </summary>
    /// <typeparam name="T">The type of the repeated value.</typeparam>
    /// <param name="value">The value to repeat.</param>
    /// <param name="count">The number of repetitions.</param>
    /// <returns>A sequence of <paramref name="value" /> repeated <paramref name="count" /> times.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    public static IEnumerable<T> Repeat<T>(T value, int count)
    {
        ThrowHelper.ThrowIfLessThan(count, 0);

        for (int i = 0; i < count; i++)
            yield return value;
    }
}