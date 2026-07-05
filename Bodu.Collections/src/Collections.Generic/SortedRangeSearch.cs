// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SortedRangeSearch.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides shared binary-search bounds over the sorted start-endpoint array used by the interval collections.
/// </summary>
internal static class SortedRangeSearch
{
    /// <summary>
    /// Returns the lowest index in <paramref name="starts" /> whose start endpoint is not less than
    /// <paramref name="value" />.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <param name="starts">The sorted start-endpoint array.</param>
    /// <param name="count">The number of live entries at the head of <paramref name="starts" />.</param>
    /// <param name="value">The endpoint to locate.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <returns>The lower-bound index for <paramref name="value" />.</returns>
    internal static int LowerBound<T>(T[] starts, int count, T value, IComparer<T> comparer)
    {
        int low = 0;
        int high = count;

        while (low < high)
        {
            int middle = low + ((high - low) >> 1);

            if (comparer.Compare(starts[middle], value) < 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }

    /// <summary>
    /// Returns the lowest index in <paramref name="starts" /> whose start endpoint is greater than
    /// <paramref name="value" />.
    /// </summary>
    /// <typeparam name="T">The endpoint type.</typeparam>
    /// <param name="starts">The sorted start-endpoint array.</param>
    /// <param name="count">The number of live entries at the head of <paramref name="starts" />.</param>
    /// <param name="value">The endpoint to locate.</param>
    /// <param name="comparer">The comparer that orders endpoints.</param>
    /// <returns>The upper-bound index for <paramref name="value" />.</returns>
    internal static int UpperBound<T>(T[] starts, int count, T value, IComparer<T> comparer)
    {
        int low = 0;
        int high = count;

        while (low < high)
        {
            int middle = low + ((high - low) >> 1);

            if (comparer.Compare(starts[middle], value) <= 0)
                low = middle + 1;
            else
                high = middle;
        }

        return low;
    }
}
