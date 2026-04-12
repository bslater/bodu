// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIsNullOrWhiteSpace.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        [DataRow(null)]
        public void ThrowIsNullOrWhiteSpace_WhenNull_ShouldThrowArgumentNullException(string? value)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIsNullOrWhiteSpace(value!);
            });
        }

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