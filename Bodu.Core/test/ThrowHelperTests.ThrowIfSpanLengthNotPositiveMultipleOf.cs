// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthNotPositiveMultipleOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Span Length Not Positive Multiple Of, Read Only Span, when Length Invalid, throws.
        /// </summary>
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

        /// <summary>
        /// Verifies that Throw If Span Length Not Positive Multiple Of, Read Only Span, when Length Valid, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(6, 3)]
        [DataRow(4, 2)]
        [DataRow(8, 4)]
        public void ThrowIfSpanLengthNotPositiveMultipleOf_ReadOnlySpan_WhenLengthValid_ShouldNotThrow(int length, int factor)
        {
            ReadOnlySpan<int> span = new int[length];
            ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(span, factor);
        }

        /// <summary>
        /// Verifies that Throw If Span Length Not Positive Multiple Of, Span, when Length Invalid, throws.
        /// </summary>
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

        /// <summary>
        /// Verifies that Throw If Span Length Not Positive Multiple Of, Span, when Length Valid, does not Throw.
        /// </summary>
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