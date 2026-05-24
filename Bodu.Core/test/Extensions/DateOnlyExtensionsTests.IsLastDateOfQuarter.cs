// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsLastDateOfQuarter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsLastDateOfQuarter(DateOnly, CalendarQuarterDefinition)" /> returns <c>true</c> only
    /// when the input date equals the computed start of the quarter.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsLastDateOfQuarterTestData), typeof(DateTimeExtensionsTests))]
    public void IsLastDateOfQuarter_WhenComparedToExpectedStart_ShouldReturnExpectedResult(DateTime inputDateTime, CalendarQuarterDefinition definition)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var actual = input.IsLastDateOfQuarter(definition);

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsLastDateOfQuarter(DateOnly, CalendarQuarterDefinition)" /> returns <c>false</c>
    /// when the input date is not the first day of the quarter.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsNotLastDateOfQuarterTestData), typeof(DateTimeExtensionsTests))]
    public void IsLastDateOfQuarter_WhenDateIsNotStartOfQuarter_ShouldReturnFalse(DateTime inputDateTime, CalendarQuarterDefinition definition)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var actual = input.IsLastDateOfQuarter(definition);
        Assert.IsFalse(actual);
    }
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsFirstDateOfQuarter(DateOnly)" /> returns <c>true</c> only when the date is the
    /// first day of a quarter based on the <see cref="CalendarQuarterDefinition.JanuaryToDecember" /> structure.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsLastDateOfQuarterJanuaryDecemberTestData), typeof(DateTimeExtensionsTests))]
    public void IsLastDateOfQuarter_WhenDateIsQuarterStartAndDefaultDefinition_ShouldReturnTrue(DateTime inputDateTime, bool expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var actual = input.IsLastDateOfQuarter();

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsLastDateOfQuarter" />, when DefinitionIsCustom, throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void IsLastDateOfQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = input.IsLastDateOfQuarter(CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsLastDateOfQuarter" />, when DefinitionIsInvalid, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsLastDateOfQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);
        var definition = (CalendarQuarterDefinition)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.IsLastDateOfQuarter(definition);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsLastDateOfQuarter" />, when UsingValidQuarterProvider, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.ValidQuarterProvider.IsLastDateOfQuarterTestData), typeof(DateTimeExtensionsTests.ValidQuarterProvider))]
    public void IsLastDateOfQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateTime inputDateTime, bool expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var provider = new DateTimeExtensionsTests.ValidQuarterProvider();

        var actual = input.IsLastDateOfQuarter(provider);

        Assert.AreEqual(expected, actual);
    }

}
