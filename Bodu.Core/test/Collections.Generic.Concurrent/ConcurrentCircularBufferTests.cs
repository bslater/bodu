// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBufferTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace Bodu.Collections.Generic.Concurrent
{
    [TestClass]
    public partial class ConcurrentCircularBufferTests
    {
        public TestContext TestContext { get; set; }

        private const int MinCapacity = 2;
        private const int DefaultCapacity = 16;

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
