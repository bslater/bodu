// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfDestinationSpanTooSmall.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationSpanTooSmall" />, when DestinationTooSmall, throws <see cref="ArgumentException" />.
        /// </summary>
        [TestMethod]
        [DataRow(10, 5)] // destination too small
        [DataRow(5, 4)]
        public void ThrowIfDestinationSpanTooSmall_WhenDestinationTooSmall_ShouldThrowArgumentException(int sourceLength, int destinationLength)
        {
            var source = new byte[sourceLength];
            var destination = new byte[destinationLength];

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfDestinationSpanTooSmall<byte, byte>(source.AsSpan(), destination.AsSpan());
            });
        }

        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationSpanTooSmall" />, when DestinationSufficient, NotThrow.
        /// </summary>
        [TestMethod]
        [DataRow(5, 5)]
        [DataRow(3, 5)]
        [DataRow(0, 0)]
        public void ThrowIfDestinationSpanTooSmall_WhenDestinationSufficient_ShouldNotThrow(int sourceLength, int destinationLength)
        {
            var source = new int[sourceLength];
            var destination = new int[destinationLength];

            ThrowHelper.ThrowIfDestinationSpanTooSmall<int, int>(source.AsSpan(), destination.AsSpan());
        }
    }
}