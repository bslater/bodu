// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullOrEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" />, when ValueIsNull, throws <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        public void ThrowIfNullOrEmpty_WhenValueIsNull_ShouldThrowArgumentNullException(string? value)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfNullOrEmpty(value!);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" />, when ValueIsEmpty, throws <see cref="ArgumentException" />.
        /// </summary>
        [TestMethod]
        [DataRow("")]
        public void ThrowIfNullOrEmpty_WhenValueIsEmpty_ShouldThrowArgumentException(string value)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNullOrEmpty(value);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" />, when ValueIsNonEmpty, NotThrow.
        /// </summary>
        [TestMethod]
        [DataRow("a")]
        [DataRow("test")]
        [DataRow("   ")] // Optional: consider whether whitespace-only is allowed
        public void ThrowIfNullOrEmpty_WhenValueIsNonEmpty_ShouldNotThrow(string value)
        {
            ThrowHelper.ThrowIfNullOrEmpty(value);
        }
    }
}