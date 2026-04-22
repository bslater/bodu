// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotOfType.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Not Of Type, when String Value Is Not Int, throws Exception.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenStringValueIsNotInt_ShouldThrowException()
        {
            object value = "string";

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNotOfType<int>(value);
            });
        }

        /// <summary>
        /// Verifies that Throw If Not Of Type, when Null Value And Target Is Non Nullable, throws Exception.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenNullValueAndTargetIsNonNullable_ShouldThrowException()
        {
            object? value = null;

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNotOfType<int>(value);
            });
        }

        /// <summary>
        /// Verifies that Throw If Not Of Type, when Int Value Is String, throws Exception.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenIntValueIsString_ShouldThrowException()
        {
            object value = 42;

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfNotOfType<string>(value);
            });
        }

        /// <summary>
        /// Verifies that Throw If Not Of Type, when Value Is Of Expected Type, does not Throw.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsOfExpectedType_ShouldNotThrow()
        {
            object value = 42;
            ThrowHelper.ThrowIfNotOfType<int>(value);
        }

        /// <summary>
        /// Verifies that Throw If Not Of Type, when Value Is Null Reference Type, does not Throw.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsNullReferenceType_ShouldNotThrow()
        {
            object? value = null;
            ThrowHelper.ThrowIfNotOfType<string>(value);
        }

        /// <summary>
        /// Verifies that Throw If Not Of Type, when Value Is Null Nullable Value Type, does not Throw.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsNullNullableValueType_ShouldNotThrow()
        {
            object? value = null;
            ThrowHelper.ThrowIfNotOfType<int?>(value);
        }
    }
}