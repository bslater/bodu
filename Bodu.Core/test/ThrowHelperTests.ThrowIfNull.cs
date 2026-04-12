// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNull.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        [DataRow(null)]
        public void ThrowIfNull_WhenValueIsNull_ShouldThrow(object? value)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfNull(value);
            });
        }

        [TestMethod]
        [DataRow("test")]
        [DataRow(123)]
        [DataRow(typeof(string))]
        public void ThrowIfNull_WhenValueIsNotNull_ShouldNotThrow(object value)
        {
            ThrowHelper.ThrowIfNull(value);
        }

        [TestMethod]
        [DataRow(null, "Custom message")]
        public void ThrowIfNull_WithMessage_WhenValueIsNull_ShouldThrowWithMessage(object? value, string message)
        {
            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfNull(value, message);
            });

            StringAssert.Contains(ex.Message, message);
        }

        [TestMethod]
        [DataRow("hello", "Custom message")]
        [DataRow(99, "Another message")]
        public void ThrowIfNull_WithMessage_WhenValueIsNotNull_ShouldNotThrow(object value, string message)
        {
            ThrowHelper.ThrowIfNull(value, message);
        }
    }
}