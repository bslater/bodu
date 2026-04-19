// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthIsNotEqualTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        // Span<T> overload

        [TestMethod]
        [DataRow(0, 4)]
        [DataRow(3, 4)]
        [DataRow(5, 4)]
        public void ThrowIfSpanLengthIsNotEqualTo_Span_WhenLengthDiffers_ShouldThrowArgumentException(int spanLength, int expectedLength)
        {
            int[] buffer = new int[spanLength];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(buffer.AsSpan(), expectedLength);
            });
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(4, 4)]
        [DataRow(16, 16)]
        public void ThrowIfSpanLengthIsNotEqualTo_Span_WhenLengthMatches_ShouldNotThrow(int spanLength, int expectedLength)
        {
            int[] buffer = new int[spanLength];
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(buffer.AsSpan(), expectedLength);
        }

        // ReadOnlySpan<T> overload

        [TestMethod]
        [DataRow(0, 4)]
        [DataRow(3, 4)]
        [DataRow(5, 4)]
        public void ThrowIfSpanLengthIsNotEqualTo_ReadOnlySpan_WhenLengthDiffers_ShouldThrowArgumentException(int spanLength, int expectedLength)
        {
            int[] buffer = new int[spanLength];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfSpanLengthIsNotEqualTo((ReadOnlySpan<int>)buffer, expectedLength);
            });
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(4, 4)]
        [DataRow(16, 16)]
        public void ThrowIfSpanLengthIsNotEqualTo_ReadOnlySpan_WhenLengthMatches_ShouldNotThrow(int spanLength, int expectedLength)
        {
            int[] buffer = new int[spanLength];
            ThrowHelper.ThrowIfSpanLengthIsNotEqualTo((ReadOnlySpan<int>)buffer, expectedLength);
        }
    }
}
