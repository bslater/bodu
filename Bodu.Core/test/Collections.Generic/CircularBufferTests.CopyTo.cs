namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that CopyTo transfers buffer items to an external array.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenBufferHasElements_ShouldCopyElementsToArray()
        {
            var buffer = new CircularBuffer<char>(3);
            buffer.Enqueue('a');
            buffer.Enqueue('b');
            var target = new char[3];
            buffer.CopyTo(target, 0);

            Assert.AreEqual('a', target[0]);
            Assert.AreEqual('b', target[1]);
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentNullException when the destination array is null.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenTargetArrayIsNull_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<string>(1);
            buffer.Enqueue("a");

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                buffer.CopyTo(null, 0);
            });
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentOutOfRangeException when the target index is negative.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenTargetIndexIsNegative_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(2);
            buffer.Enqueue(1);
            var array = new int[3];

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                buffer.CopyTo(array, -1);
            });
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentException when the destination array is too small to hold all elements.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenTargetArrayIsTooSmall_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(2);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            var array = new int[1];

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                buffer.CopyTo(array, 0);
            });
        }

        /// <summary>
        /// Verifies that CopyTo includes nulls when copying buffer contents to an array.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenBufferContainsNulls_ShouldIncludeNullsInTargetArray()
        {
            var buffer = new CircularBuffer<string>(2);
            buffer.Enqueue(null);
            buffer.Enqueue("X");

            var array = new string[2];
            buffer.CopyTo(array, 0);

            Assert.IsNull(array[0]);
            Assert.AreEqual("X", array[1]);
        }
    }
}