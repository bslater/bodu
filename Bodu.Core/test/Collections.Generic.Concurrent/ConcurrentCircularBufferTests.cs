// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bodu.Collections.Generic.Concurrent
{
    [TestClass]
    public partial class ConcurrentCircularBufferTests
    {
        public TestContext TestContext { get; set; }

        private const int MinCapacity = 2;
        private const int DefaultCapacity = 16;

        private sealed record TestItem
        {
            public int Value { get; set; }

            public TestItem(int value) { Value = value; }

            public override string ToString() => $"Item({Value})";
        }

        /// <summary>
        /// A reference type that intentionally does not override Equals or GetHashCode,
        /// so that equality falls back to reference identity. Used to test Contains behaviour
        /// for types without custom equality.
        /// </summary>
        private sealed class ReferenceItem
        {
            public int Value { get; }
            public ReferenceItem(int value) => Value = value;
        }

        private static void AssertBufferContainsExactlyValues(
            ConcurrentCircularBuffer<TestItem> buffer,
            params int[] expectedValues)
        {
            var snapshot = buffer.ToArray();
            Assert.AreEqual(expectedValues.Length, snapshot.Length, "Buffer item count mismatch.");

            for (int i = 0; i < expectedValues.Length; i++)
            {
                Assert.IsNotNull(snapshot[i], $"Item at index {i} was null.");
                Assert.AreEqual(expectedValues[i], snapshot[i].Value, $"Item at index {i} did not match expected value.");
            }
        }

        private static void AssertBufferContainsOnlyValuesInRange(
            ConcurrentCircularBuffer<TestItem> buffer,
            int expectedCount,
            int minInclusive,
            int maxInclusive)
        {
            var snapshot = buffer.ToArray();

            Assert.AreEqual(expectedCount, snapshot.Length, $"Expected buffer to contain {expectedCount} items.");

            foreach (var item in snapshot)
            {
                Assert.IsNotNull(item, "Buffer contained a null item.");
                Assert.IsTrue(item.Value >= minInclusive && item.Value <= maxInclusive,
                    $"Item value {item.Value} was outside the expected range [{minInclusive}, {maxInclusive}].");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (WeakReference wr1, WeakReference wr2) EnqueueItemsAndReturnWeakReferences(
            ConcurrentCircularBuffer<TestItem> buffer)
        {
            var item1 = new TestItem(1);
            var item2 = new TestItem(2);
            buffer.Enqueue(item1);
            buffer.Enqueue(item2);
            return (new WeakReference(item1), new WeakReference(item2));
        }
    }
}