// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotZero.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Not Zero, when Value Is Not Zero, throws.
        /// </summary>
        [TestMethod]
        [DataRow(1)]
        [DataRow(-1)]
        [DataRow(int.MaxValue)]
        [DataRow(int.MinValue)]
        public void ThrowIfNotZero_WhenValueIsNotZero_ShouldThrow(int value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfNotZero(value);
            });
        }

        /// <summary>
        /// Verifies that Throw If Not Zero, when Value Is Zero, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        public void ThrowIfNotZero_WhenValueIsZero_ShouldNotThrow(int value)
        {
            ThrowHelper.ThrowIfNotZero(value);
        }
    }
}