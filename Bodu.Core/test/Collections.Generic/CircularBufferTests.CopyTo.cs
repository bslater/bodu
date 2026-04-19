using Bodu.Collections.Generic.Concurrent;
using System.Collections;

namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that CopyTo correctly copies all elements to the destination array starting at index zero.
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
        public void CopyTo_WhenArrayIsNull_ShouldThrowExactly()
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
        public void CopyTo_WhenIndexIsNegative_ShouldThrowExactly()
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
        public void CopyTo_WhenArrayIsTooSmall_ShouldThrowExactly()
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
        /// Verifies that CopyTo throws ArgumentException when the target index offset leaves insufficient space for all elements.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenIndexOffsetLeavesInsufficientSpace_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            var array = new int[4];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                buffer.CopyTo(array, 2);
            });
        }

        /// <summary>
        /// Verifies that CopyTo throws ArgumentException when the target index equals the array length.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenIndexEqualsArrayLength_ShouldThrowExactly()
        {
            var buffer = new CircularBuffer<int>(1);
            buffer.Enqueue(42);
            var array = new int[3];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                buffer.CopyTo(array, 3);
            });
        }

        /// <summary>
        /// Verifies that CopyTo does not modify the destination array when the buffer is empty.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenBufferIsEmpty_ShouldLeaveArrayUnchanged()
        {
            var buffer = new CircularBuffer<int>(3);
            var array = new[] { 9, 8, 7 };
            buffer.CopyTo(array, 0);
            CollectionAssert.AreEqual(new[] { 9, 8, 7 }, array);
        }

        /// <summary>
        /// Verifies that CopyTo writes elements at the correct offset when a non-zero index is supplied.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenIndexIsNonZero_ShouldCopyToCorrectOffset()
        {
            var buffer = new CircularBuffer<int>(2);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            var array = new int[4];
            buffer.CopyTo(array, 2);
            Assert.AreEqual(0, array[0]);
            Assert.AreEqual(0, array[1]);
            Assert.AreEqual(10, array[2]);
            Assert.AreEqual(20, array[3]);
        }

        /// <summary>
        /// Verifies that CopyTo preserves logical enqueue order when the internal buffer has wrapped around.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenBufferHasWrappedAround_ShouldCopyInEnqueueOrder()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            buffer.Dequeue();
            buffer.Enqueue(4);
            var array = new int[3];
            buffer.CopyTo(array, 0);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, array);
        }

        /// <summary>
        /// Verifies that CopyTo includes null entries in the destination array when the buffer contains nulls.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenBufferContainsNulls_ShouldIncludeNullsInArray()
        {
            var buffer = new CircularBuffer<string>(2);
            buffer.Enqueue(null);
            buffer.Enqueue("X");
            var array = new string[2];
            buffer.CopyTo(array, 0);
            Assert.IsNull(array[0]);
            Assert.AreEqual("X", array[1]);
        }

        /// <summary>
        /// Verifies that CopyTo succeeds and copies elements to the correct positions when the destination array is larger than required.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenArrayIsLargerThanRequired_ShouldCopyWithoutThrowing()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(1);
            buffer.Enqueue(2);
            buffer.Enqueue(3);
            var array = new int[10];
            buffer.CopyTo(array, 0);
            Assert.AreEqual(1, array[0], "First element should be copied to index 0.");
            Assert.AreEqual(2, array[1], "Second element should be copied to index 1.");
            Assert.AreEqual(3, array[2], "Third element should be copied to index 2.");
            Assert.AreEqual(0, array[9], "Trailing slots beyond the copied range should remain unmodified.");
        }

        /// <summary>
        /// Verifies that CopyTo copies all elements correctly when the destination array is exactly the same size as the buffer count.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenArrayIsExactFit_ShouldCopyAllElements()
        {
            var buffer = new CircularBuffer<int>(3);
            buffer.Enqueue(10);
            buffer.Enqueue(20);
            buffer.Enqueue(30);
            var array = new int[3];
            buffer.CopyTo(array, 0);
            Assert.AreEqual(10, array[0], "First element should be copied to index 0.");
            Assert.AreEqual(20, array[1], "Second element should be copied to index 1.");
            Assert.AreEqual(30, array[2], "Third element should be copied to index 2.");
        }

        /// <summary>
        /// Verifies that CopyTo does not throw when the index equals the array length and the buffer is empty, since zero elements require no space.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenIndexEqualsArrayLengthAndBufferIsEmpty_ShouldNotThrow()
        {
            var buffer = new CircularBuffer<int>(3);
            var array = new int[3];
            buffer.CopyTo(array, array.Length);
            CollectionAssert.AreEqual(new int[3], array,
                "Array should remain unmodified when the buffer is empty, even when index equals array length.");
        }
        /// <summary>
        /// Verifies that CopyTo produces an independent snapshot such that replacing an element in the destination
        /// array does not affect the contents of the buffer.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenDestinationElementIsReplaced_ShouldNotAffectBuffer()
        {
            var buffer = new CircularBuffer<TestItem>(3);
            buffer.Enqueue(new TestItem(1));
            buffer.Enqueue(new TestItem(2));
            buffer.Enqueue(new TestItem(3));
            var array = new TestItem[3];
            buffer.CopyTo(array, 0);

            // Replace the slot in the copied array — this must not reach into the buffer
            array[0] = new TestItem(99);

            var verify = new TestItem[3];
            buffer.CopyTo(verify, 0);

            Assert.AreEqual(1, verify[0]?.Value,
                "Replacing array[0] with a new instance should not affect the buffer's first element; CopyTo must produce an independent array.");
            Assert.AreEqual(2, verify[1]?.Value,
                "Buffer's second element should be unaffected by any modification to the destination array.");
            Assert.AreEqual(3, verify[2]?.Value,
                "Buffer's third element should be unaffected by any modification to the destination array.");
        }

        /// <summary>
        /// Verifies that CopyTo performs a shallow copy, meaning that mutating the state of a copied reference-type
        /// element is reflected in the buffer, as both hold a reference to the same object.
        /// </summary>
        [TestMethod]
        public void CopyTo_WhenCopiedReferenceTypeElementIsMutated_ShouldReflectInBuffer()
        {
            var buffer = new CircularBuffer<TestItem>(2);
            buffer.Enqueue(new TestItem(1));
            buffer.Enqueue(new TestItem(2));
            var array = new TestItem[2];
            buffer.CopyTo(array, 0);

            // Mutate the object via the copied reference — both array[0] and the buffer slot point at the same instance
            array[0].Value = 99;

            var verify = new TestItem[2];
            buffer.CopyTo(verify, 0);

            Assert.AreEqual(99, verify[0]?.Value,
                "CopyTo is a shallow copy — mutating a property on a reference-type element via the destination array " +
                "affects the same object held in the buffer, as both reference the same instance.");
            Assert.AreEqual(2, verify[1]?.Value,
                "Buffer's second element should be unaffected as only the first element's property was mutated.");
        }
    }
}