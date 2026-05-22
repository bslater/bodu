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

    /// <summary>
    /// An <see cref="int" /> equality comparer whose <see cref="GetHashCode" /> throws an
    /// <see cref="InvalidOperationException" /> for one designated value, modelling a user comparer that faults while
    /// hashing an element.
    /// </summary>
    private sealed class ThrowingHashCodeComparer : IEqualityComparer<int>
    {
        private readonly int _faultingValue;

        public ThrowingHashCodeComparer(int faultingValue)
        {
            _faultingValue = faultingValue;
        }

        public bool Equals(int x, int y) =>
            x == y;

        public int GetHashCode(int obj)
        {
            if (obj == _faultingValue)
                throw new InvalidOperationException("Comparer hash fault.");

            return obj;
        }
    }

    /// <summary>
    /// An <see cref="int" /> equality comparer that funnels every element into one bucket and, once
    /// <see cref="FaultArmed" /> is set, throws an <see cref="InvalidOperationException" /> from <see cref="Equals" />,
    /// modelling a user comparer that faults while comparing two elements during a bucket-chain walk.
    /// </summary>
    private sealed class ThrowingEqualsComparer : IEqualityComparer<int>
    {
        /// <summary>
        /// When <see langword="true" />, every <see cref="Equals" /> call throws.
        /// </summary>
        public bool FaultArmed;

        public bool Equals(int x, int y)
        {
            if (FaultArmed)
                throw new InvalidOperationException("Comparer equality fault.");

            return x == y;
        }

        public int GetHashCode(int obj) =>
            0;
    }
}
