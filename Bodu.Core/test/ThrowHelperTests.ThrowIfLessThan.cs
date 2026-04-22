// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        // Non-nullable overloads

        /// <summary>
        /// Verifies that Throw If Less Than, when Value Is Less Than Min, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        [DataRow(-1, 0)]
        [DataRow(0, 1)]
        [DataRow(5, 6)]
        public void ThrowIfLessThan_WhenValueIsLessThanMin_ShouldThrowArgumentOutOfRangeException(int value, int min)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfLessThan(value, min);
            });
        }

        /// <summary>
        /// Verifies that Throw If Less Than, when Value Is Greater Than Or Equal To Min, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(6, 5)]
        [DataRow(int.MaxValue, int.MinValue)]
        public void ThrowIfLessThan_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(int value, int min)
        {
            ThrowHelper.ThrowIfLessThan(value, min);
        }

        // Nullable overloads

        /// <summary>
        /// Verifies that Throw If Less Than, Nullable, when Value Is Null And Throw If Null, throws Argument Null Exception.
        /// </summary>
        [TestMethod]
        [DataRow(null, 5, true)]
        public void ThrowIfLessThan_Nullable_WhenValueIsNullAndThrowIfNull_ShouldThrowArgumentNullException(int? value, int min, bool throwIfNull)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
            });
        }

        /// <summary>
        /// Verifies that Throw If Less Than, Nullable, when Value Is Null And Throw If Null Is False, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(null, 5, false)]
        public void ThrowIfLessThan_Nullable_WhenValueIsNullAndThrowIfNullIsFalse_ShouldNotThrow(int? value, int min, bool throwIfNull)
        {
            ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
        }

        /// <summary>
        /// Verifies that Throw If Less Than, Nullable, when Value Is Less Than Min, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        [DataRow(2, 5, false)]
        [DataRow(-1, 0, false)]
        public void ThrowIfLessThan_Nullable_WhenValueIsLessThanMin_ShouldThrowArgumentOutOfRangeException(int? value, int min, bool throwIfNull)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
            });
        }

        /// <summary>
        /// Verifies that Throw If Less Than, Nullable, when Value Is Greater Than Or Equal To Min, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(5, 5, false)]
        [DataRow(6, 5, false)]
        public void ThrowIfLessThan_Nullable_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(int? value, int min, bool throwIfNull)
        {
            ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
        }
    }
}