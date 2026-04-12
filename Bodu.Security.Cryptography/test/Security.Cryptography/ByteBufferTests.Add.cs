using Bodu.Security.Cryptography;

namespace Bodu.Infrastructure
{
    public partial class ByteBufferTests
    {
        /// <summary>
        /// Verifies that multiple partial additions correctly track state and reach full capacity.
        /// </summary>
        [TestMethod]
        public void Add_WhenAddedInChunks_ShouldReachFullCorrectly()
        {
            var buffer = new ByteBuffer(4);
            Assert.IsFalse(buffer.Add(new byte[] { 1 }, 0, 1));
            Assert.IsFalse(buffer.Add(new byte[] { 2 }, 0, 1));
            Assert.IsFalse(buffer.Add(new byte[] { 3 }, 0, 1));
            Assert.IsTrue(buffer.Add(new byte[] { 4 }, 0, 1)); // now full
        }

        /// <summary>
        /// Verifies that passing null throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void Add_WhenArrayIsNull_ShouldThrowExactly()
        {
            var buffer = new ByteBuffer(2);
            Assert.ThrowsExactly<ArgumentNullException>(() => buffer.Add(null!, 0, 1));
        }

        /// <summary>
        /// Verifies that adding bytes to completely fill the buffer returns true.
        /// </summary>
        [TestMethod]
        public void Add_WhenBufferIsFilled_ShouldReturnTrue()
        {
            var buffer = new ByteBuffer(2);
            var result = buffer.Add(new byte[] { 1, 2 }, 0, 2);
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Verifies that writing beyond remaining buffer space throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Add_WhenCountExceedsRemainingCapacity_ShouldThrowExactly()
        {
            var buffer = new ByteBuffer(2);
            buffer.Add(new byte[] { 1 }, 0, 1);
            buffer.Add(new byte[] { 2, 3 }, 0, 2);
        }

        /// <summary>
        /// Verifies that a negative count throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        public void Add_WhenCountIsNegative_ShouldThrowExactly()
        {
            var buffer = new ByteBuffer(4);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => buffer.Add(new byte[4], 0, -1));
        }

        /// <summary>
        /// Verifies that adding 0 bytes does not change the state of the buffer.
        /// </summary>
        [TestMethod]
        public void Add_WhenCountIsZero_ShouldNotChangeState()
        {
            var buffer = new ByteBuffer(4);
            buffer.Add(new byte[] { 1 }, 0, 0);
            Assert.AreEqual(0, buffer.Count);
            Assert.IsTrue(buffer.IsEmpty);
        }

        /// <summary>
        /// Verifies that invalid index + count combination throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Add_WhenIndexAndCountAreInvalidRange_ShouldThrowExactly()
        {
            var buffer = new ByteBuffer(4);
            buffer.Add(new byte[2], 1, 2);
        }

        /// <summary>
        /// Verifies that a negative index throws ArgumentOutOfRangeException.
        /// </summary>
        [TestMethod]
        public void Add_WhenIndexIsNegative_ShouldThrowExactly()
        {
            var buffer = new ByteBuffer(4);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => buffer.Add(new byte[4], -1, 1));
        }

        /// <summary>
        /// Verifies that adding bytes works and does not mark the buffer as full until filled.
        /// </summary>
        [TestMethod]
        public void Add_WhenValidBytesAdded_ShouldReturnFalseUntilFull()
        {
            var buffer = new ByteBuffer(4);
            var result = buffer.Add(new byte[] { 1, 2 }, 0, 2);
            Assert.IsFalse(result);
        }
    }
}