// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.Age.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that the age is correctly adjusted when the evaluation date is a non-leap year.
    /// </summary>
    [TestMethod]
    public void Age_WhenBornOnLeapDay_EvaluatedInNonLeapYear_ShouldAdjustToFeb28()
    {
        var birth = new DateOnly(2000, 2, 29);
        var reference = new DateOnly(2023, 2, 28);

        Assert.AreEqual(23, birth.Age(reference));
    }
    /// <summary>
    /// Verifies that the <see cref="DateOnlyExtensions.Age(DateOnly, DateOnly)" /> method returns the expected age in full calendar
    /// years for various date combinations.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.AgeTestData), typeof(DateTimeExtensionsTests))]
    public void Age_WhenCalculatedAgainstDate_ShouldReturnExpected(DateTime inputDateTime, DateTime atDateTime, int expected)
    {
        var birth = DateOnly.FromDateTime(inputDateTime);
        var atDate = DateOnly.FromDateTime(atDateTime);

        var actual = birth.Age(atDate);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that calculating age from MaxValue to MinValue clamps to 0.
    /// </summary>
    [TestMethod]
    public void Age_WhenMaxEvaluatedBeforeMin_ShouldReturnZero()
    {
        var age = DateOnly.MaxValue.Age(DateOnly.MinValue);

        Assert.AreEqual(0, age);
    }

    /// <summary>
    /// Verifies that dates before the minimum supported year do not throw but clamp to 0.
    /// </summary>
    [TestMethod]
    public void Age_WhenReferenceDateBeforeBirth_ShouldReturnZero()
    {
        var birth = new DateOnly(2000, 1, 1);
        var earlier = new DateOnly(1999, 12, 31);

        Assert.AreEqual(0, birth.Age(earlier));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Age(DateOnly)" /> returns the same actual as
    /// <see cref="DateOnlyExtensions.Age(DateOnly, DateOnly)" /> when called with <see cref="DateOnly.FromDateTime(DateTime.Today)" />.
    /// </summary>
    [TestMethod]
    public void Age_WhenUsingDefaultToday_ShouldMatchExplicitCall()
    {
        var birth = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
        var expected = birth.Age(DateOnly.FromDateTime(DateTime.Today));

        var actual = birth.Age();

        Assert.AreEqual(expected, actual);
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

}
