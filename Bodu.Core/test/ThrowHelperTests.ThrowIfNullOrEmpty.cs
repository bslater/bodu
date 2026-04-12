// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullOrEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        [DataRow(null)]
        public void ThrowIfNullOrEmpty_WhenValueIsNull_ShouldThrowArgumentNullException(string? value)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfNullOrEmpty(value!);
            });
        }

        [TestMethod]
        [DataRow("")]
        public void ThrowIfNullOrEmpty_WhenValueIsEmpty_ShouldThrowArgumentException(string value)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNullOrEmpty(value);
            });
        }

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