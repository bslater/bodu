// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Comparers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// An <see cref="int" /> equality comparer that returns a single constant hash code for every value, forcing every
    /// element into the same bucket so that bucket-chain logic is exercised deterministically.
    /// </summary>
    private sealed class ConstantHashComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) =>
            x == y;

        public int GetHashCode(int obj) =>
            0;
    }
}
