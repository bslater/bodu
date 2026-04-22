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
        /// Verifies that Throw If Destination Span Too Small, when Destination Too Small, throws Argument Exception.
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
        /// Verifies that Throw If Destination Span Too Small, when Destination Sufficient, does not Throw.
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