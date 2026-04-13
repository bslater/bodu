// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.Farey.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Generates the Farey sequence of a given order n, consisting of reduced fractions between 0 and 1 inclusive.
    /// </summary>
    /// <param name="order">The order of the Farey sequence. Must be positive.</param>
    /// <returns>A sequence of tuples representing simplified fractions (numerator, denominator) in ascending order.</returns>
    /// <remarks>The Farey sequence of order n includes all unique fractions a/b such that: 0 ≤ a ≤ b ≤ n, GCD(a, b) = 1.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="order"/> is less than 1.</exception>
    public static IEnumerable<(int Numerator, int Denominator)> Farey(int order)
    {
        ThrowHelper.ThrowIfLessThan(order, 1);

        yield return (0, 1);
        int a = 0, b = 1, c = 1, d = order;

        while (c <= order)
        {
            int k = (order + b) / d;
            int tempA = c;
            int tempB = d;

            c = (k * c) - a;
            d = (k * d) - b;

            a = tempA;
            b = tempB;

            yield return (a, b);
        }
    }
}
