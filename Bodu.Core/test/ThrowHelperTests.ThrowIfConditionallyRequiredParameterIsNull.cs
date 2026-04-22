// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfConditionallyRequiredParameterIsNull.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Conditionally Required Parameter Is Null, when Condition Matches And Value Is Null, throws Argument Exception.
        /// </summary>
        [TestMethod]
        [DataRow(null, true, true)]
        public void ThrowIfConditionallyRequiredParameterIsNull_WhenConditionMatchesAndValueIsNull_ShouldThrowArgumentException(string? value, bool condition, bool matchValue)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(value, condition, matchValue);
            });
        }

        /// <summary>
        /// Verifies that Throw If Conditionally Required Parameter Is Null, when Condition Does Not Match Or Value Is Not Null, does not Throw.
        /// </summary>
        [TestMethod]
        [DataRow(null, false, true)]
        [DataRow(null, true, false)]
        [DataRow("ok", true, true)]
        [DataRow("ok", false, true)]
        public void ThrowIfConditionallyRequiredParameterIsNull_WhenConditionDoesNotMatchOrValueIsNotNull_ShouldNotThrow(string? value, bool condition, bool matchValue)
        {
            ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(value, condition, matchValue);
        }
    }
}