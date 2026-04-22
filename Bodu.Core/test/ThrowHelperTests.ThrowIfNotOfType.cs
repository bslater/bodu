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
        /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when StringValueIsNotInt, throws <see cref="ArgumentException" />.
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
        /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when NullValueAndTargetIsNonNullable, throws <see cref="ArgumentException" />.
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
        /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when IntValueIsString, throws <see cref="ArgumentException" />.
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
        /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when ValueIsOfExpectedType, NotThrow.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsOfExpectedType_ShouldNotThrow()
        {
            object value = 42;
            ThrowHelper.ThrowIfNotOfType<int>(value);
        }

        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when ValueIsNullReferenceType, NotThrow.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsNullReferenceType_ShouldNotThrow()
        {
            object? value = null;
            ThrowHelper.ThrowIfNotOfType<string>(value);
        }

        /// <summary>
        /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when ValueIsNullNullableValueType, NotThrow.
        /// </summary>
        [TestMethod]
        public void ThrowIfNotOfType_WhenValueIsNullNullableValueType_ShouldNotThrow()
        {
            object? value = null;
            ThrowHelper.ThrowIfNotOfType<int?>(value);
        }
    }
}