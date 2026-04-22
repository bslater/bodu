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
        /// Verifies that <see cref="ThrowHelper.ThrowIsNullOrWhiteSpace" />, when Null, throws <see cref="ArgumentNullException" />.
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
        /// Verifies that <see cref="ThrowHelper.ThrowIsNullOrWhiteSpace" />, when EmptyOrWhitespace, throws <see cref="ArgumentException" />.
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
        /// Verifies that <see cref="ThrowHelper.ThrowIsNullOrWhiteSpace" />, when ValueIsValid, NotThrow.
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