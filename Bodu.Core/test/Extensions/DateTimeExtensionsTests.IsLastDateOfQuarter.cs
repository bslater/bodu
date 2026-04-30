// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsLastDateOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsLastDateOfQuarter(DateTime, CalendarQuarterDefinition)" /> returns <c>false</c>
    /// when the input date is not the first day of the quarter.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsNotLastDateOfQuarterTestData), DynamicDataSourceType.Method)]
    public void IsLastDateOfQuarter_WhenDateIsNotStartOfQuarterDefinition_ShouldReturnFalse(DateTime input, CalendarQuarterDefinition definition)
    {
        bool actual = input.IsLastDateOfQuarter(definition);
        Assert.IsFalse(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfQuarter(DateTime)" /> returns <c>true</c> only when the date is the
    /// first day of a quarter based on the <see cref="CalendarQuarterDefinition.JanuaryToDecember" /> structure.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsLastDateOfQuarterJanuaryDecemberTestData), DynamicDataSourceType.Method)]
    public void IsLastDateOfQuarter_WhenDateIsQuarterStartAndDefaultDefinition_ShouldReturnTrue(DateTime input, bool expected)
    {
        bool actual = input.IsLastDateOfQuarter();

        Assert.AreEqual(actual, expected);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsLastDateOfQuarter(DateTime, CalendarQuarterDefinition)" /> returns <c>true</c> only
    /// when the input date equals the computed start of the quarter.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsLastDateOfQuarterTestData), DynamicDataSourceType.Method)]
    public void IsLastDateOfQuarter_WhenDateMatchesStartOfQuarterDefinition_ShouldReturnTrue(DateTime input, CalendarQuarterDefinition definition)
    {
        bool actual = input.IsLastDateOfQuarter(definition);

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsLastDateOfQuarter" />, when DefinitionIsCustom, throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void IsLastDateOfQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 20);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = input.IsLastDateOfQuarter(CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsLastDateOfQuarter" />, when DefinitionIsInvalid, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsLastDateOfQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 20);
        var definition = (CalendarQuarterDefinition)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.IsLastDateOfQuarter(definition);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsLastDateOfQuarter" />, when UsingValidQuarterProvider, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.ValidQuarterProvider.IsLastDateOfQuarterTestData), typeof(DateTimeExtensionsTests.ValidQuarterProvider), DynamicDataSourceType.Method)]
    public void IsLastDateOfQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateTime input, bool expected)
    {
        var provider = new ValidQuarterProvider();
        var actual = input.IsLastDateOfQuarter(provider);

        Assert.AreEqual(expected, actual);
    }
}
