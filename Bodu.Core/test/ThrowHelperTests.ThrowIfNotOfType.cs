// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotOfType.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        public void ThrowIfNotOfType_WhenStringValueIsNotInt_ShouldThrowArgumentException()
        {
            object value = "string";

            Assert.ThrowsException<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNotOfType<int>(value);
            });
        }

        [TestMethod]
        public void ThrowIfNotOfType_WhenNullValueAndTargetIsNonNullable_ShouldThrowArgumentException()
        {
            object? value = null;

            Assert.ThrowsException<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNotOfType<int>(value);
            });
        }

        [TestMethod]
        public void ThrowIfNotOfType_WhenIntValueIsString_ShouldThrowArgumentException()
        {
            object value = 42;

            Assert.ThrowsException<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNotOfType<string>(value);
            });
        }

        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsOfExpectedType_ShouldNotThrow()
        {
            object value = 42;
            ThrowHelper.ThrowIfNotOfType<int>(value);
        }

        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsNullReferenceType_ShouldNotThrow()
        {
            object? value = null;
            ThrowHelper.ThrowIfNotOfType<string>(value);
        }

        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsNullNullableValueType_ShouldNotThrow()
        {
            object? value = null;
            ThrowHelper.ThrowIfNotOfType<int?>(value);
        }
    }
}