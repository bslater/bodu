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
        public void ThrowIfNotOfType_WhenStringValueIsNotInt_ShouldThrowException()
        {
            object value = "string";

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNotOfType<int>(value);
            });
        }

        [TestMethod]
        public void ThrowIfNotOfType_WhenNullValueAndTargetIsNonNullable_ShouldThrowException()
        {
            object? value = null;

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNotOfType<int>(value);
            });
        }

        [TestMethod]
        public void ThrowIfNotOfType_WhenIntValueIsString_ShouldThrowException()
        {
            object value = 42;

            Assert.ThrowsExactly<ArgumentException>(() =>
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