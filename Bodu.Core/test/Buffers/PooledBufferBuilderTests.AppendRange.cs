namespace Bodu.Buffers
{
    public partial class PooledBufferBuilderTests
    {
        [TestMethod]
        public void AppendRange_WhenEnumerableUsed_ShouldAppendAllItems_UsingIEnumerable()
        {
            var source = Enumerable.Range(1, 50);
            using var builder = new PooledBufferBuilder<int>();

            builder.AppendRange(source);

            CollectionAssert.AreEqual(source.ToArray(), builder.AsSpan().ToArray());
        }

        [TestMethod]
        public void AppendRange_WhenExceedsInitialSize_ShouldExpandBuffer_UsingIEnumerable()
        {
            var source = Enumerable.Range(1, 1000);
            using var builder = new PooledBufferBuilder<int>();

            builder.AppendRange(source);

            Assert.AreEqual(1000, builder.Count);
        }
    }
}