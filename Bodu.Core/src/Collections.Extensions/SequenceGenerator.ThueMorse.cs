// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.ThueMorse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Generates the Thue–Morse sequence as a binary sequence of 0s and 1s.
    /// </summary>
    /// <param name="count">The number of terms to generate. Must be non-negative.</param>
    /// <returns>A sequence of <see cref="int"/> values where each term is 0 or 1, representing the Thue–Morse sequence.</returns>
    /// <remarks>The n-th value is the parity of the number of 1s in the binary representation of <c>n: T(n) = bitcount(n) % 2</c>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
    public static IEnumerable<int> ThueMorse(int count)
    {
        ThrowHelper.ThrowIfLessThan(count, 0);

        for (int i = 0; i < count; i++)
        {
            int parity = 0, n = i;
            while (n > 0)
            {
                parity ^= n & 1;
                n >>= 1;
            }

            yield return parity;
        }
    }
}
