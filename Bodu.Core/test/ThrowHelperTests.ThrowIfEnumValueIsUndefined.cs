// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public enum TestEnum
    {
        A = 0,
        B = 1,
    }

    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Enum Value Is Undefined, when Value Is Undefined, throws Argument Out Of Range Exception.
        /// </summary>
        [TestMethod]
        [DataRow((TestEnum)99)]
        [DataRow((TestEnum)(-1))]
        public void ThrowIfEnumValueIsUndefined_WhenValueIsUndefined_ShouldThrowArgumentOutOfRangeException(TestEnum value)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                ThrowHelper.ThrowIfEnumValueIsUndefined(value);
            });
        }

        /// <summary>
        /// Verifies that Throw If Enum Value Is Undefined, when Value Is Defined, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(TestEnum.A)]
        [DataRow(TestEnum.B)]
        public void ThrowIfEnumValueIsUndefined_WhenValueIsDefined_ShouldNotThrow(TestEnum value)
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
        }
    }
}