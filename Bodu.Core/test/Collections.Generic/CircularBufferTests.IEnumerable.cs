namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that the buffer supports iteration via a foreach loop.
        /// </summary>
        [TestMethod]
        public void IEnumerable_WhenUsedInForeachLoop_ShouldEnumerateInFifoOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);

            var results = new List<int>();
            foreach (var item in buffer)
            {
                results.Add(item);
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, results);
        }

        /// <summary>
        /// Verifies that the generic enumerator produces the expected sequence.
        /// </summary>
        [TestMethod]
        public void IEnumerable_WhenUsingGenericEnumerator_ShouldEnumerateCorrectly()
        {
            var buffer = new CircularBuffer<string>(2);
            buffer.Enqueue("x");
            buffer.Enqueue("y");

            var list = buffer.ToList();
            CollectionAssert.AreEqual(new[] { "x", "y" }, list);
        }

        /// <summary>
        /// Verifies that the non-generic GetEnumerator yields items in FirstInFirstOut order.
        /// </summary>
        [TestMethod]
        public void IEnumerable_WhenUsingNonGenericEnumerator_ShouldReturnItemsAsObjects()
        {
            System.Collections.IEnumerable buffer = new CircularBuffer<int>(3);
            ((CircularBuffer<int>)buffer).Enqueue(1);
            ((CircularBuffer<int>)buffer).Enqueue(2);
            ((CircularBuffer<int>)buffer).Enqueue(3);

            var actual = new List<int>();
            foreach (var item in buffer)
            {
                actual.Add((int)item);
            }

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, actual);
        }

        /// <summary>
        /// Verifies that enumerating an empty buffer yields no results.
        /// </summary>
        [TestMethod]
        public void IEnumerable_WhenBufferIsEmpty_ShouldYieldNoElements()
        {
            var buffer = new CircularBuffer<int>(3);
            var actual = buffer.ToList();

            CollectionAssert.AreEqual(Array.Empty<int>(), actual);
        }

        /// <summary>
        /// Verifies that enumeration and CopyTo yield the same results when buffer is contiguous.
        /// </summary>
        [TestMethod]
        public void IEnumerable_WhenContiguous_ShouldMatchResultsFromCopyTo()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);

            var copyResult = new int[buffer.Count];
            ((System.Collections.ICollection)buffer).CopyTo(copyResult, 0);
            var enumResult = buffer.ToArray();

            CollectionAssert.AreEqual(copyResult, enumResult);
        }

        /// <summary>
        /// Verifies that enumeration and CopyTo yield the same results when buffer is wrapped.
        /// </summary>
        [TestMethod]
        public void IEnumerable_WhenWrapped_ShouldMatchResultsFromCopyTo()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Enqueue(4);
            buffer.Enqueue(5);
            buffer.Dequeue();
            buffer.Enqueue(6);

            var copyResult = new int[buffer.Count];
            ((System.Collections.ICollection)buffer).CopyTo(copyResult, 0);
            var enumResult = buffer.ToArray();

            CollectionAssert.AreEqual(copyResult, enumResult);
        }

        /// <summary>
        /// Verifies that enumeration and CopyTo match when buffer is full and head == tail (wrapped).
        /// </summary>
        [TestMethod]
        public void IEnumerable_WhenFullAndHeadEqualsTail_ShouldMatchResultsFromCopyTo()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Enqueue(100);
            buffer.Enqueue(200);
            buffer.Enqueue(300);
            buffer.Enqueue(400);
            buffer.Enqueue(500);
            buffer.Dequeue();
            buffer.Enqueue(600); // head == tail, wrapped full

            var copyResult = new int[buffer.Count];
            ((System.Collections.ICollection)buffer).CopyTo(copyResult, 0);
            var enumResult = buffer.ToArray();

            CollectionAssert.AreEqual(copyResult, enumResult);
        }
    }
}