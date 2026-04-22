// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfDestinationTooSmall.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Destination Too Small, Array, when Destination Too Small, throws Argument Exception.
        /// </summary>
        [TestMethod]
        [DataRow(5, 3)] // destination too small
        [DataRow(4, 2)]
        public void ThrowIfDestinationTooSmall_Array_WhenDestinationTooSmall_ShouldThrowArgumentException(int sourceLength, int destinationLength)
        {
            var source = new int[sourceLength];
            var destination = new byte[destinationLength];

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfDestinationTooSmall(source, destination);
            });
        }

        /// <summary>
        /// Verifies that Throw If Destination Too Small, Array, when Destination Sufficient, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(5, 5)]
        [DataRow(3, 5)]
        [DataRow(0, 0)]
        public void ThrowIfDestinationTooSmall_Array_WhenDestinationSufficient_ShouldNotThrow(int sourceLength, int destinationLength)
        {
            var source = new int[sourceLength];
            var destination = new byte[destinationLength];

            ThrowHelper.ThrowIfDestinationTooSmall(source, destination);
        }
    }
}