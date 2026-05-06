// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.IsFirstDateOfQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Extensions.DateTimeExtensionsTests;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsFirstDateOfQuarter" />, when DateIsQuarterStartAndDefaultDefinition, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsFirstDateOfQuarterJanuaryDecemberTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
    public void IsFirstDateOfQuarter_WhenDateIsQuarterStartAndDefaultDefinition_ShouldReturnTrue(DateTime inputDateTime)
    {
        var input = DateOnly.FromDateTime(inputDateTime);

        bool actual = input.IsFirstDateOfQuarter();

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsFirstDateOfQuarter" />, when DateMatchesStartOfQuarterDefinition, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsFirstDateOfQuarterTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
    public void IsFirstDateOfQuarter_WhenDateMatchesStartOfQuarterDefinition_ShouldReturnTrue(DateTime inputDateTime, CalendarQuarterDefinition definition)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        bool actual = input.IsFirstDateOfQuarter(definition);

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsFirstDateOfQuarter" />, when DateIsNotStartOfQuarterDefinition, returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.IsNotFirstDateOfQuarterTestData), typeof(DateTimeExtensionsTests), DynamicDataSourceType.Method)]
    public void IsFirstDateOfQuarter_WhenDateIsNotStartOfQuarterDefinition_ShouldReturnFalse(DateTime inputDateTime, CalendarQuarterDefinition definition)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        bool actual = input.IsFirstDateOfQuarter(definition);

        Assert.IsFalse(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsFirstDateOfQuarter" />, when DefinitionIsInvalid, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsFirstDateOfQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);
        var definition = (CalendarQuarterDefinition)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.IsFirstDateOfQuarter(definition);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsFirstDateOfQuarter" />, when DefinitionIsCustom, throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void IsFirstDateOfQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = input.IsFirstDateOfQuarter(CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsFirstDateOfQuarter" />, when UsingValidQuarterProvider, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.ValidQuarterProvider.IsFirstDateOfQuarterTestData), typeof(DateTimeExtensionsTests.ValidQuarterProvider), DynamicDataSourceType.Method)]
    public void IsFirstDateOfQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateTime inputDateTime, bool expected)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var provider = new ValidQuarterProvider();

        var actual = input.IsFirstDateOfQuarter(provider);

        Assert.AreEqual(expected, actual);
    }
}
