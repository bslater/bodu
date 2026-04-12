namespace Bodu.Collections.Generic
{
    public partial class CircularBufferTests
    {
        /// <summary>
        /// Verifies that the Capacity property returns the size defined at construction time.
        /// </summary>
        [TestMethod]
        public void Capacity_WhenConstructed_ShouldReturnDefinedSize()
        {
            var buffer = new CircularBuffer<string>(10);
            Assert.AreEqual(10, buffer.Capacity);
        }
    }
}