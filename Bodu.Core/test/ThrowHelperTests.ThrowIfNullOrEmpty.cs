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
        /// Verifies that Throw If Null Or Empty, when Value Is Null, throws Argument Null Exception.
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
        /// Verifies that Throw If Null Or Empty, when Value Is Empty, throws Argument Exception.
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
        /// Verifies that Throw If Null Or Empty, when Value Is Non Empty, does not Throw.
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