using Bodu.Collections.Generic.Extensions;

namespace Bodu.Collections.Generic
{
    public partial class ShuffleHelpersTests
    {
        /// <summary>
        /// Asserts that two arrays differ in order, indicating successful shuffling.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="buffer">The shuffled buffer.</param>
        /// <param name="original">The original buffer before shuffling.</param>
        /// <param name="message">The assertion message to display on failure.</param>
        private static void AssertOrderChanged<T>(T[] buffer, T[] original, string message)
        {
            bool changed = !buffer.SequenceEqual(original);
            Assert.IsTrue(changed, message);
        }

        /// <summary>
        /// Verifies that passing a null array to Shuffle throws <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenArrayIsNull_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ShuffleHelpers.Shuffle<int>(null!, new SystemRandomAdapter());
            });
        }

        /// <summary>
        /// Verifies that passing a null RNG to Shuffle (array) throws <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenRngIsNull_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ShuffleHelpers.Shuffle(new[] { 1, 2, 3 }, null!);
            });
        }

#if !NETSTANDARD2_0

        /// <summary>
        /// Verifies that passing a null RNG to Shuffle (span) throws <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenValidSpanAndRngIsNull_ShouldThrowExactly()
        {
            var buffer = new[] { 1, 2, 3 };
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ShuffleHelpers.Shuffle(buffer.AsSpan(), null!);
            });
        }

        /// <summary>
        /// Verifies that passing a null RNG to Shuffle (memory) throws <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenValidMemoryAndRngIsNull_ShouldThrowExactly()
        {
            var buffer = new[] { 1, 2, 3 };
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ShuffleHelpers.Shuffle(buffer.AsMemory(), null!);
            });
        }

        /// <summary>
        /// Verifies that shuffling an empty span does not throw.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenSpanIsEmpty_ShouldNotThrow()
        {
            Span<int> span = Span<int>.Empty;
            ShuffleHelpers.Shuffle(span, new SystemRandomAdapter());
        }

        /// <summary>
        /// Verifies that shuffling a single-element span does not change its contents.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenSpanHasOneItem_ShouldReturnSameItem()
        {
            int[] buffer = { 42 };
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer.AsSpan(), new SystemRandomAdapter());
            CollectionAssert.AreEqual(original, buffer);
        }

        /// <summary>
        /// Verifies that shuffling a multi-item span changes the original order.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenCalled_ShouldMutateOriginalOrder_UsingSpan()
        {
            int[] buffer = Enumerable.Range(1, 20).ToArray();
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer.AsSpan(), new SystemRandomAdapter());
            AssertOrderChanged(buffer, original, "Span shuffle did not alter order.");
        }

        /// <summary>
        /// Verifies that shuffling a span retains all original values.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenCalled_ShouldContainSameElements_UsingSpan()
        {
            int[] buffer = Enumerable.Range(1, 100).ToArray();
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer.AsSpan(), new SystemRandomAdapter());
            CollectionAssert.AreEquivalent(original, buffer);
        }

        /// <summary>
        /// Verifies that shuffling a memory block retains all original values.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenCalled_ShouldContainSameElements_UsingMemory()
        {
            int[] buffer = Enumerable.Range(1, 50).ToArray();
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer.AsMemory(), new SystemRandomAdapter());
            CollectionAssert.AreEquivalent(original, buffer);
        }

        /// <summary>
        /// Verifies that shuffling a memory block changes the original order.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenCalled_ShouldMutateOriginalOrder_UsingMemory()
        {
            int[] buffer = Enumerable.Range(1, 50).ToArray();
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer.AsMemory(), new SystemRandomAdapter());
            AssertOrderChanged(buffer, original, "Memory shuffle did not alter order.");
        }

        /// <summary>
        /// Verifies that shuffling a single-element memory block does not change its contents.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenMemoryHasOneItem_ShouldReturnSameItem()
        {
            int[] buffer = { 42 };
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer.AsMemory(), new SystemRandomAdapter());
            CollectionAssert.AreEqual(original, buffer);
        }
#endif

        /// <summary>
        /// Verifies that shuffling a single-element array does not change its contents.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenArrayHasOneItem_ShouldReturnSameItem()
        {
            int[] buffer = { 42 };
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer, new SystemRandomAdapter());
            CollectionAssert.AreEqual(original, buffer);
        }

        /// <summary>
        /// Verifies that shuffling a multi-item array changes the original order.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenCalled_ShouldMutateOriginalOrder_UsingArray()
        {
            int[] buffer = Enumerable.Range(1, 20).ToArray();
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer, new SystemRandomAdapter());
            AssertOrderChanged(buffer, original, "Array shuffle did not alter order.");
        }

        /// <summary>
        /// Verifies that shuffling a multi-item array retains all elements.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenCalled_ShouldContainSameElements_UsingArray()
        {
            int[] buffer = Enumerable.Range(1, 50).ToArray();
            int[] original = buffer.ToArray();

            ShuffleHelpers.Shuffle(buffer, new SystemRandomAdapter());
            CollectionAssert.AreEquivalent(original, buffer);
        }

        /// <summary>
        /// Runs 20,000 in-place shuffles of a 10-element array to validate statistical uniformity of output positions. Each value should
        /// appear roughly equally in each position, with no more than 2 statistically significant outliers.
        /// </summary>
        [TestMethod]
        public void Shuffle_WhenRepeated_ShouldDistributeItemsStatistically()
        {
            const int runs = 20000;
            const int size = 10;
            var tracker = new int[size, size];
            var original = Enumerable.Range(0, size).ToArray();

            for (int r = 0; r < runs; r++)
            {
                var buffer = original.ToArray();
                ShuffleHelpers.Shuffle(buffer, new SystemRandomAdapter());

                for (int i = 0; i < size; i++)
                    tracker[i, buffer[i]]++;
            }

            AssertStatisticalUniformity(tracker, size, label: nameof(ShuffleHelpers.Shuffle));
        }
    }
}