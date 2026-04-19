namespace Bodu.Collections.Generic
{
    [TestClass]
    public partial class CircularBufferTests
    {
        private sealed record TestItem
        {
            public int Value { get; set; }

            public TestItem(int  value) { Value = value; }

            public override string ToString() => $"Item({Value})";
        }

        private const int DefaultCapacity = 16;
    }
}