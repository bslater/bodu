// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionCapacity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides shared capacity-growth arithmetic for the array-backed generic collections.
/// </summary>
internal static class CollectionCapacity
{
    /// <summary>
    /// Computes the next backing-array capacity by doubling the current length, clamped to
    /// <see cref="Array.MaxLength" /> and floored at a required minimum.
    /// </summary>
    /// <param name="currentLength">The current backing-array length. A length of zero selects <paramref name="defaultCapacity" />.</param>
    /// <param name="defaultCapacity">The capacity to use when the backing array is currently empty.</param>
    /// <param name="minimum">The minimum capacity the result must satisfy.</param>
    /// <returns>The chosen capacity, never below <paramref name="minimum" /> and never above <see cref="Array.MaxLength" />.</returns>
    internal static int Grow(int currentLength, int defaultCapacity, int minimum)
    {
        int capacity = currentLength == 0 ? defaultCapacity : currentLength * 2;

        if ((uint)capacity > Array.MaxLength)
            capacity = Array.MaxLength;

        if (capacity < minimum)
            capacity = minimum;

        return capacity;
    }
}
