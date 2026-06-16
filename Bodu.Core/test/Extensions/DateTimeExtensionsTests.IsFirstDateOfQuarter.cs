// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.IsFirstDateOfQuarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfQuarter" />, when DateIsNotStartOfQuarterDefinition, returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsNotFirstDateOfQuarterTestData))]
    public void IsFirstDateOfQuarter_WhenDateIsNotStartOfQuarterDefinition_ShouldReturnFalse(DateTime inputDate, CalendarQuarterDefinition definition)
    {
        bool actual = inputDate.IsFirstDateOfQuarter(definition);
        Assert.IsFalse(actual);
    }
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfQuarter" />, when DateIsQuarterStartAndDefaultDefinition, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsFirstDateOfQuarterJanuaryDecemberTestData))]
    public void IsFirstDateOfQuarter_WhenDateIsQuarterStartAndDefaultDefinition_ShouldReturnTrue(DateTime input)
    {
        bool actual = input.IsFirstDateOfQuarter();

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfQuarter" />, when DateMatchesStartOfQuarterDefinition, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(IsFirstDateOfQuarterTestData))]
    public void IsFirstDateOfQuarter_WhenDateMatchesStartOfQuarterDefinition_ShouldReturnTrue(DateTime inputDate, CalendarQuarterDefinition definition)
    {
        bool actual = inputDate.IsFirstDateOfQuarter(definition);

        Assert.IsTrue(actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfQuarter" />, when DefinitionIsCustom, throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void IsFirstDateOfQuarter_WhenDefinitionIsCustom_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 20);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = input.IsFirstDateOfQuarter(CalendarQuarterDefinition.Custom);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfQuarter" />, when DefinitionIsInvalid, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void IsFirstDateOfQuarter_WhenDefinitionIsInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 20);
        var definition = (CalendarQuarterDefinition)999;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.IsFirstDateOfQuarter(definition);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfQuarter" />, when UsingValidQuarterProvider, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.ValidQuarterProvider.IsFirstDateOfQuarterTestData), typeof(DateTimeExtensionsTests.ValidQuarterProvider))]
    public void IsFirstDateOfQuarter_WhenUsingValidQuarterProvider_ShouldReturnExpectedDate(DateTime input, bool expected)
    {
        var provider = new ValidQuarterProvider();

        bool actual = input.IsFirstDateOfQuarter(provider);

        Assert.AreEqual(expected, actual);
    }

}
