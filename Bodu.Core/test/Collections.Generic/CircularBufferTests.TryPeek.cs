namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that TryPeek returns true and the oldest item when the buffer is not empty.
        /// </summary>
        [TestMethod]
        public void TryPeek_WhenBufferIsNotEmpty_ShouldReturnTrueAndOldestItem()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(5);
            buffer.Enqueue(10);

            var success = buffer.TryPeek(out int actual);

            Assert.IsTrue(success);
            Assert.AreEqual(5, actual);
        }

        /// <summary>
        /// Verifies that TryPeek returns false and default when the buffer is empty.
        /// </summary>
        [TestMethod]
        public void TryPeek_WhenBufferIsEmpty_ShouldReturnFalseAndDefault()
        {
            var buffer = new CircularBuffer<string>(2);

            var success = buffer.TryPeek(out string actual);

            Assert.IsFalse(success);
            Assert.IsNull(actual);
        }

        /// <summary>
        /// Verifies that TryPeek returns true and outputs null when null is the current item.
        /// </summary>
        [TestMethod]
        public void TryPeek_WhenItemIsNull_ShouldReturnTrueAndNull()
        {
            var buffer = new CircularBuffer<string>(1);
            buffer.Enqueue(null);

            var success = buffer.TryPeek(out var actual);

            Assert.IsTrue(success);
            Assert.IsNull(actual);
        }
    }
}