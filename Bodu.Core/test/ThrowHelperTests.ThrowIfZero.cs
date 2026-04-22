// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfZero.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Zero, when Value Is Zero, throws.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        public void ThrowIfZero_WhenValueIsZero_ShouldThrow(int value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfZero(value);
            });
        }

        /// <summary>
        /// Verifies that Throw If Zero, when Value Is Non Zero, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        [DataRow(int.MaxValue)]
        public void ThrowIfZero_WhenValueIsNonZero_ShouldNotThrow(int value)
        {
            ThrowHelper.ThrowIfZero(value);
        }
    }
}