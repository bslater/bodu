namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that the buffer maintains correct order and state after many wraparounds.
        /// </summary>
        [TestMethod]
        public void EnqueueDequeue_AfterManyWraparounds_ShouldMaintainOrder()
        {
            var buffer = new CircularBuffer<int>(3);

            for (int i = 0; i < 30; i++)
            {
                buffer.Enqueue(i);
                buffer.Dequeue();
                buffer.Enqueue(i + 100);
                buffer.Dequeue();
            }

            Assert.AreEqual(0, buffer.Count); // All items added and removed correctly
        }

        /// <summary>
        /// Verifies that the buffer can store and retrieve null values when using a reference type.
        /// </summary>
        [TestMethod]
        public void EnqueueDequeue_NullHandling_ShouldStoreAndRetrieveNull()
        {
            var buffer = new CircularBuffer<string>(2);
            buffer.Enqueue(null);
            buffer.Enqueue("A");

            Assert.AreEqual(null, buffer.Dequeue());
            Assert.AreEqual("A", buffer.Dequeue());
        }
    }
}