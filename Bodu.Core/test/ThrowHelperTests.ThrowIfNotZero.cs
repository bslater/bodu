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
        /// Verifies that <see cref="ThrowHelper.ThrowIfNotZero" />, when ValueIsNotZero, throws <see cref="ArgumentOutOfRangeException" />.
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
        /// Verifies that <see cref="ThrowHelper.ThrowIfNotZero" />, when ValueIsZero, NotThrow.
        /// </summary>
        [TestMethod]
        [DataRow(0)]
        public void ThrowIfNotZero_WhenValueIsZero_ShouldNotThrow(int value)
        {
            ThrowHelper.ThrowIfNotZero(value);
        }
    }
}