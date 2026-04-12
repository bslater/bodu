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

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(6, 5)]
        [DataRow(int.MaxValue, int.MinValue)]
        public void ThrowIfLessThan_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(int value, int min)
        {
            ThrowHelper.ThrowIfLessThan(value, min);
        }

        // Nullable overloads

        [TestMethod]
        [DataRow(null, 5, true)]
        public void ThrowIfLessThan_Nullable_WhenValueIsNullAndThrowIfNull_ShouldThrowArgumentNullException(int? value, int min, bool throwIfNull)
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
            });
        }

        [TestMethod]
        [DataRow(null, 5, false)]
        public void ThrowIfLessThan_Nullable_WhenValueIsNullAndThrowIfNullIsFalse_ShouldNotThrow(int? value, int min, bool throwIfNull)
        {
            ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
        }

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

        [TestMethod]
        [DataRow(5, 5, false)]
        [DataRow(6, 5, false)]
        public void ThrowIfLessThan_Nullable_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(int? value, int min, bool throwIfNull)
        {
            ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
        }
    }
}