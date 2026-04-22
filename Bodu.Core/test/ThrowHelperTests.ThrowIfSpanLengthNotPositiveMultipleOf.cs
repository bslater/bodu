// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthNotPositiveMultipleOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        [DataRow(5, 2)]  // Not a multiple
        [DataRow(0, 1)]  // Zero length
        [DataRow(7, 3)]  // Not a multiple
        public void ThrowIfSpanLengthNotPositiveMultipleOf_ReadOnlySpan_WhenLengthInvalid_ShouldThrow(int length, int factor)
        {
            var span = new int[length];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(new ReadOnlySpan<int>(span), factor);
            });
        }

        [TestMethod]
        [DataRow(6, 3)]
        [DataRow(4, 2)]
        [DataRow(8, 4)]
        public void ThrowIfSpanLengthNotPositiveMultipleOf_ReadOnlySpan_WhenLengthValid_ShouldNotThrow(int length, int factor)
        {
            ReadOnlySpan<int> span = new int[length];
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(span, factor);
        }

        [TestMethod]
        [DataRow(5, 2)]
        [DataRow(0, 1)]
        [DataRow(7, 3)]
        public void ThrowIfSpanLengthNotPositiveMultipleOf_Span_WhenLengthInvalid_ShouldThrow(int length, int factor)
        {
            var span = new int[length];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(span.AsSpan(), factor);
            });
        }

        [TestMethod]
        [DataRow(6, 3)]
        [DataRow(4, 2)]
        [DataRow(8, 4)]
        public void ThrowIfSpanLengthNotPositiveMultipleOf_Span_WhenLengthValid_ShouldNotThrow(int length, int factor)
        {
            Span<int> span = new int[length];
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(span, factor);
        }
    }
}