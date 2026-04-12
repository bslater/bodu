using System.Collections;

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        private static ICollection CreateWrappedBuffer()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Enqueue(4);
            buffer.Enqueue(5);
            buffer.Dequeue();
            buffer.Enqueue(6);
            return buffer;
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo copies elements correctly.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenBufferPopulated_ShouldCopyElements()
        {
            ICollection collection = new CircularBuffer<int>(3);
            ((CircularBuffer<int>)collection).Enqueue(10);
            ((CircularBuffer<int>)collection).Enqueue(20);

            var array = new int[3];
            collection.CopyTo(array, 0);

            Assert.AreEqual(10, array[0]);
            Assert.AreEqual(20, array[1]);
        }

        /// <summary>
        /// Verifies that ICollection.SyncRoot is not null and IsSynchronized is false.
        /// </summary>
        [TestMethod]
        public void ICollection_SyncRootAndIsSynchronized_WhenAccessed_ShouldReturnExpectedValues()
        {
            ICollection collection = new CircularBuffer<string>(1);
            Assert.IsNotNull(collection.SyncRoot);
            Assert.IsFalse(collection.IsSynchronized);
        }

        /// <summary>
        /// Verifies that ICollection.Count returns correct value.
        /// </summary>
        [TestMethod]
        public void ICollection_Count_WhenQueried_ShouldMatchEnqueuedItemCount()
        {
            ICollection collection = new CircularBuffer<int>(3);
            ((CircularBuffer<int>)collection).Enqueue(100);
            ((CircularBuffer<int>)collection).Enqueue(200);

            Assert.AreEqual(2, collection.Count);
        }

        /// <summary>
        /// Verifies that ICollection.Count matches CircularBuffer.Count.
        /// </summary>
        [TestMethod]
        public void ICollection_Count_WhenComparedToBuffer_ShouldBeEqual()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            ICollection collection = buffer;

            Assert.AreEqual(buffer.Count, collection.Count);
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo throws ArgumentNullException if target array is null.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenTargetArrayIsNull_ShouldThrowExactly()
        {
            ICollection buffer = new CircularBuffer<string>(2);
            Assert.ThrowsExactly<ArgumentNullException>(() => buffer.CopyTo(null, 0));
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo throws ArgumentException for multi-dimensional arrays.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenTargetArrayIsMultiDimensional_ShouldThrowExactly()
        {
            ICollection buffer = new CircularBuffer<int>(2);
            var multidim = new int[2, 2];
            Assert.ThrowsExactly<ArgumentException>(() => buffer.CopyTo(multidim, 0));
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo throws ArgumentException for non-zero lower-bound arrays.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenArrayHasNonZeroLowerBound_ShouldThrowExactly()
        {
            ICollection buffer = new CircularBuffer<int>(2);
            Array array = Array.CreateInstance(typeof(int), new[] { 5 }, new[] { 1 });
            Assert.ThrowsExactly<ArgumentException>(() => buffer.CopyTo(array, 0));
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo throws ArgumentOutOfRangeException for negative index.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
        {
            ICollection buffer = new CircularBuffer<int>(2);
            var array = new int[3];
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => buffer.CopyTo(array, -1));
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo throws ArgumentException when the array is too small.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenArrayTooSmall_ShouldThrowExactly()
        {
            ICollection buffer = new CircularBuffer<int>(3);
            ((CircularBuffer<int>)buffer).Enqueue(1);
            ((CircularBuffer<int>)buffer).Enqueue(2);

            var array = new int[1];
            Assert.ThrowsExactly<ArgumentException>(() => buffer.CopyTo(array, 0));
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo on an empty buffer does not throw.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenBufferIsEmpty_ShouldNotThrow()
        {
            ICollection buffer = new CircularBuffer<int>(3);
            var array = new int[5];
            buffer.CopyTo(array, 0);
            Assert.AreEqual(0, ((CircularBuffer<int>)buffer).Count);
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo handles wrapped buffers correctly.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenBufferIsWrapped_ShouldCopyCorrectly()
        {
            ICollection buffer = CreateWrappedBuffer();
            var actual = new int[5];
            buffer.CopyTo(actual, 0);

            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6 }, actual);
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo throws if destination array type does not match buffer
        /// element type.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenArrayTypeMismatch_ShouldThrowExactly()
        {
            ICollection buffer = new CircularBuffer<string>(2);
            ((CircularBuffer<string>)buffer).Enqueue("test");

            var wrongTypeArray = new int[5];
            Assert.ThrowsExactly<ArgumentException>(() => buffer.CopyTo(wrongTypeArray, 0));
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo copies all elements when buffer is contiguous.
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenContiguous_ShouldCopyAllElements()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);

            var array = new int[3];
            ((ICollection)buffer).CopyTo(array, 0);

            CollectionAssert.AreEqual(new[] { 10, 20, 30 }, array);
        }

        /// <summary>
        /// Verifies that ICollection.CopyTo works when buffer is full and wrapped (head == tail).
        /// </summary>
        [TestMethod]
        public void ICollection_CopyTo_WhenWrappedAndFull_ShouldCopyAllElements()
        {
            var buffer = new CircularBuffer<int>(5);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Enqueue(4);
            buffer.Enqueue(5);
            buffer.Dequeue();
            buffer.Enqueue(6);

            var array = new int[5];
            ((ICollection)buffer).CopyTo(array, 0);

            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6 }, array);
        }
    }
}