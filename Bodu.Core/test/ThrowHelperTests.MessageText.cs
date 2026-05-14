// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.MessageText.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsInsufficient" /> embeds the formatted minimum
    /// length in the thrown <see cref="ArgumentException" />'s message. The message is resource-backed via
    /// <c>Arg_Invalid_ArrayTooShort</c> which contains a <c>{0}</c> placeholder for the minimum.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayLengthIsInsufficient_WhenArrayIsBelowMinimum_ShouldIncludeMinimumLengthInMessage()
    {
        const int minimum = 16;
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(new int[3], minimum, "array");
        });

        Assert.IsTrue(
            ex.Message.Contains(minimum.ToString(System.Globalization.CultureInfo.CurrentCulture), StringComparison.Ordinal),
            $"Exception message did not include the minimum length '{minimum}'. Actual message: {ex.Message}");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" /> embeds the divisor in
    /// the thrown <see cref="ArgumentException" />'s message. The message comes from the
    /// <c>Arg_Invalid_ArrayLengthMultipleOf</c> resource which formats the divisor as <c>{0}</c>.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenLengthIsNotMultiple_ShouldIncludeDivisorInMessage()
    {
        const int divisor = 8;
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(new int[5], divisor, "array");
        });

        Assert.IsTrue(
            ex.Message.Contains(divisor.ToString(System.Globalization.CultureInfo.CurrentCulture), StringComparison.Ordinal),
            $"Exception message did not include the divisor '{divisor}'. Actual message: {ex.Message}");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionTooSmall" /> embeds the required minimum count in
    /// the thrown <see cref="ArgumentException" />'s message. The message comes from
    /// <c>Arg_Invalid_CollectionTooSmall</c> which formats the minimum count as <c>{0}</c>.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionTooSmall_WhenCollectionShorterThanRequired_ShouldIncludeMinimumCountInMessage()
    {
        const int requiredMin = 5;
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfCollectionTooSmall(new List<int> { 1, 2 }, requiredMin, "collection");
        });

        Assert.IsTrue(
            ex.Message.Contains(requiredMin.ToString(System.Globalization.CultureInfo.CurrentCulture), StringComparison.Ordinal),
            $"Exception message did not include the required minimum '{requiredMin}'. Actual message: {ex.Message}");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined{TEnum}" /> embeds both the rejected
    /// value and the enum type name in the thrown <see cref="ArgumentOutOfRangeException" />'s message. The
    /// message comes from <c>Arg_OutOfRangeException_EnumValue</c> which has both <c>{0}</c> (type name) and
    /// <c>{1}</c> (rejected value) placeholders.
    /// </summary>
    [TestMethod]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsUndefined_ShouldIncludeValueAndTypeNameInMessage()
    {
        const TestEnum value = (TestEnum)99;
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(value, "value");
        });

        Assert.IsTrue(
            ex.Message.Contains("99", StringComparison.Ordinal),
            $"Exception message did not include the rejected value '99'. Actual message: {ex.Message}");
        Assert.IsTrue(
            ex.Message.Contains(nameof(TestEnum), StringComparison.Ordinal),
            $"Exception message did not include the enum type name '{nameof(TestEnum)}'. Actual message: {ex.Message}");
    }
}
