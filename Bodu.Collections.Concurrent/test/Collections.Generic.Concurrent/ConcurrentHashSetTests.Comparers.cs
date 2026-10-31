// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Comparers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// An <see cref="int" /> equality comparer that returns a single constant hash code for every value, forcing every
    /// element into the same bucket so that bucket-chain logic is exercised deterministically.
    /// </summary>
    private sealed class ConstantHashComparer
        : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) =>
            x == y;

        public int GetHashCode(int obj) =>
            0;
    }

    /// <summary>
    /// An <see cref="int" /> equality comparer whose <see cref="GetHashCode" /> throws an
    /// <see cref="InvalidOperationException" /> for one designated value, modeling a user comparer that faults while
    /// hashing an element.
    /// </summary>
    private sealed class ThrowingHashCodeComparer
        : IEqualityComparer<int>
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
    /// modeling a user comparer that faults while comparing two elements during a bucket-chain walk.
    /// </summary>
    private sealed class ThrowingEqualsComparer
        : IEqualityComparer<int>
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

    /// <summary>
    /// An <see cref="int" /> equality comparer that pins every element to a known split-order position in bucket zero
    /// - <c>1</c> and <c>2</c> hash to <c>1 &lt;&lt; 16</c> and <c>2 &lt;&lt; 16</c>, every other value to
    /// <c>3 &lt;&lt; 16</c> - and runs a one-shot callback from the first <see cref="Equals" /> call after it is armed,
    /// so a test can mutate the set from inside a traversal that has reached a known node.
    /// </summary>
    /// <remarks>
    /// Reversing those hash bits orders the list as <c>2</c>, then <c>1</c>, then the colliding run of every other
    /// value, so a callback that adds <c>1</c> inserts between <c>2</c> and that run, and one that adds another
    /// colliding value appends to the run.
    /// </remarks>
    private sealed class ReentrantComparer
        : IEqualityComparer<int>
    {
        /// <summary>
        /// Gets or sets the callback the next <see cref="Equals" /> call runs before comparing. It is cleared before it
        /// runs, so a mutation it makes cannot trigger it again.
        /// </summary>
        public Action? OnEquals { get; set; }

        /// <inheritdoc />
        public bool Equals(int x, int y)
        {
            Action? callback = OnEquals;
            if (callback is not null)
            {
                OnEquals = null;
                callback();
            }

            return x == y;
        }

        /// <inheritdoc />
        public int GetHashCode(int obj) =>
            Math.Min(obj, 3) << 16;
    }
}
