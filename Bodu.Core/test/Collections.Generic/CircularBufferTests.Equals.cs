// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CircularBufferTests.Equals.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that <see cref="CircularBuffer{T}.Equals" /> returns true when comparing the buffer with itself.
        /// </summary>
        [TestMethod]
        public void Equals_ShouldReturnTrue_WhenComparingSameReference()
        {
            var buffer = new CircularBuffer<int>(3);
            Assert.IsTrue(buffer.Equals(buffer));
        }

        /// <summary>
        /// Verifies that <see cref="CircularBuffer{T}.Equals" /> returns false when comparing the buffer with null.
        /// </summary>
        [TestMethod]
        public void Equals_ShouldReturnFalse_WhenComparingWithNull()
        {
            var buffer = new CircularBuffer<int>(3);
            Assert.IsFalse(buffer.Equals(null));
        }

        /// <summary>
        /// Verifies that <see cref="CircularBuffer{T}.Equals" /> returns false when comparing the buffer with an object of another type.
        /// </summary>
        [TestMethod]
        public void Equals_ShouldReturnFalse_WhenComparingDifferentType()
        {
            var buffer = new CircularBuffer<string>(2);
            Assert.IsFalse(buffer.Equals("not a buffer"));
        }

        /// <summary>
        /// Verifies that Equals returns false when comparing two different buffer instances even if
        /// contents match.
        /// </summary>
        [TestMethod]
        public void Equals_ShouldReturnFalse_WhenComparingTwoDistinctInstancesWithSameContents()
        {
            var buffer1 = new CircularBuffer<string>(3);
            var buffer2 = new CircularBuffer<string>(3);

            buffer1.Enqueue("A");
            buffer2.Enqueue("A");

            Assert.IsFalse(buffer1.Equals(buffer2));
        }
    }
}