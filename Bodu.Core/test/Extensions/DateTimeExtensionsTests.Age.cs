// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.Age.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that the <see cref="DateTimeExtensions.Age(DateTime, DateTime)" /> method returns the expected age in full calendar
    /// years for various date combinations.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.AgeTestData))]
    public void Age_WhenCalculatedAgainstDate_ShouldReturnExpected(DateTime input, DateTime atDate, int expected)
    {
        var actual = input.Age(atDate);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Age(DateTime)" /> returns the same actual as
    /// <see cref="DateTimeExtensions.Age(DateTime, DateTime)" /> when called with <see cref="DateTime.Today" />.
    /// </summary>
    [TestMethod]
    public void Age_WhenUsingDefaultToday_ShouldMatchExplicitCall()
    {
        DateTime birth = DateTime.Today.AddYears(-1);
        var expected = birth.Age(DateTime.Today);
        var actual = birth.Age();

        Assert.AreEqual(expected, actual, "Default overload did not match Age(input, DateTime.Today)");
    }

    /// <summary>
    /// Verifies that minimum and maximum supported dates do not throw.
    /// </summary>
    [TestMethod]
    public void Age_WhenUsingMinAndMaxDateOnly_ShouldNotThrow()
    {
        var age = DateOnly.MinValue.Age(DateOnly.MaxValue);
        Assert.IsGreaterThan(0, age);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeKind" /> differences are ignored in age calculations.
    /// </summary>
    [TestMethod]
    public void Age_WhenUsingMixedDateTimeKinds_ShouldIgnoreKind()
    {
        var birth = new DateTime(2000, 4, 18, 0, 0, 0, DateTimeKind.Local);
        var atDate = new DateTime(2024, 4, 18, 0, 0, 0, DateTimeKind.Utc);

        Assert.AreEqual(24, birth.Age(atDate));
    }

}
