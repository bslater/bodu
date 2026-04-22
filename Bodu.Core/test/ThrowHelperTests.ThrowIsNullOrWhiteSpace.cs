// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIsNullOrWhiteSpace.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw Is Null Or White Space, when Null, throws Argument Null Exception.
        /// </summary>
        [TestMethod]
        [DataRow(null)]
        public void ThrowIsNullOrWhiteSpace_WhenNull_ShouldThrowArgumentNullException(string? value)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIsNullOrWhiteSpace(value!);
            });
        }

        /// <summary>
        /// Verifies that Throw Is Null Or White Space, when Empty Or Whitespace, throws Argument Exception.
        /// </summary>
        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("\t")]
        [DataRow("\n")]
        public void ThrowIsNullOrWhiteSpace_WhenEmptyOrWhitespace_ShouldThrowArgumentException(string value)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIsNullOrWhiteSpace(value);
            });
        }

        /// <summary>
        /// Verifies that Throw Is Null Or White Space, when Value Is Valid, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow("Valid")]
        [DataRow("x")]
        [DataRow("  trimmed")]
        [DataRow("middle space")]
        public void ThrowIsNullOrWhiteSpace_WhenValueIsValid_ShouldNotThrow(string value)
        {
            ThrowHelper.ThrowIsNullOrWhiteSpace(value);
        }
    }
}