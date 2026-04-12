namespace Bodu.Collections.Generic
{
    public partial class SequenceGeneratorTests
    {
        /// <summary>
        /// Verifies that Repeat returns an infinite sequence repeating the given value.
        /// </summary>
        [TestMethod]
        public void Repeat_WhenCalled_ShouldReturnInfiniteSequence()
        {
            var actual = SequenceGenerator.Repeat("A").Take(3).ToArray();
            CollectionAssert.AreEqual(new[] { "A", "A", "A" }, actual);
        }

        /// <summary>
        /// Verifies that Repeat returns a fixed number of repetitions when a positive count is provided.
        /// </summary>
        [TestMethod]
        public void Repeat_WhenCountIsPositive_ShouldReturnFixedSequence()
        {
            var actual = SequenceGenerator.Repeat("Z", 4).ToArray();
            CollectionAssert.AreEqual(new[] { "Z", "Z", "Z", "Z" }, actual);
        }

        /// <summary>
        /// Verifies that Repeat throws an ArgumentOutOfRangeException when count is negative.
        /// </summary>
        [TestMethod]
        public void Repeat_WhenCountIsNegative_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                SequenceGenerator.Repeat("X", -2).ToArray();
            });
        }

        /// <summary>
        /// Verifies that Repeat returns an empty sequence when count is zero.
        /// </summary>
        [TestMethod]
        public void Repeat_WhenCountIsZero_ShouldReturnEmptySequence()
        {
            var actual = SequenceGenerator.Repeat("A", 0).ToArray();
            Assert.AreEqual(0, actual.Length);
        }

        /// <summary>
        /// Verifies that Repeat does not trigger enumeration until explicitly iterated.
        /// </summary>
        [TestMethod]
        public void Repeat_WhenCalled_ShouldDeferExecution()
        {
            AssertExecutionIsDeferred(
                methodName: "Repeat",
                invokeExtensionMethod: _ => SequenceGenerator.Repeat("X"),
                values: new[] { "X" });
        }

        /// <summary>
        /// Verifies that Repeat with a positive count does not trigger enumeration until explicitly iterated.
        /// </summary>
        [TestMethod]
        public void Repeat_WhenCountIsPositive_ShouldDeferExecution()
        {
            AssertExecutionIsDeferred(
                methodName: "Repeat",
                invokeExtensionMethod: _ => SequenceGenerator.Repeat("Z", 5),
                values: new[] { "Z" });
        }
    }
}