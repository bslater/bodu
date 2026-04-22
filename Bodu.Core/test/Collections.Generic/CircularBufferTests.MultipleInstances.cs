// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.MultipleInstances.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that multiple CircularBuffer instances maintain independent state and contents.
        /// </summary>
        [TestMethod]
        public void MultipleInstances_ShouldMaintainIndependentState()
        {
            var buffer1 = new CircularBuffer<string>(3);
            var buffer2 = new CircularBuffer<string>(3);

            buffer1.Enqueue("A");
            buffer1.Enqueue("B");

            buffer2.Enqueue("X");
            buffer2.Enqueue("Y");

            CollectionAssert.AreEqual(new[] { "A", "B" }, buffer1.ToArray());
            CollectionAssert.AreEqual(new[] { "X", "Y" }, buffer2.ToArray());
        }

        /// <summary>
        /// Verifies that event handlers added to one buffer do not affect another buffer.
        /// </summary>
        [TestMethod]
        public void MultipleInstances_ShouldHaveIsolatedEventHandlers()
        {
            var events1 = new List<string>();
            var events2 = new List<string>();

            var buffer1 = new CircularBuffer<string>(2, allowOverwrite: true);
            var buffer2 = new CircularBuffer<string>(2, allowOverwrite: true);

            buffer1.ItemEvicted += item => events1.Add("Buffer1:" + item);
            buffer2.ItemEvicted += item => events2.Add("Buffer2:" + item);

            buffer1.Enqueue("a");
            buffer1.Enqueue("b");
            buffer1.Enqueue("c"); // Evict a

            buffer2.Enqueue("x");
            buffer2.Enqueue("y");
            buffer2.Enqueue("z"); // Evict x

            CollectionAssert.AreEqual(new[] { "Buffer1:a" }, events1);
            CollectionAssert.AreEqual(new[] { "Buffer2:x" }, events2);
        }
    }
}