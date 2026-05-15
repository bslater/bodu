// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class FiscalWeekQuarterProviderTests
{

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>dayOfWeek</c> is not a defined <see cref="DayOfWeek" /> value.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDayOfWeekIsUndefined_ShouldThrowException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new FiscalWeekQuarterProvider(1, (DayOfWeek)99));
    }

    /// <summary>
    /// Verifies that a provider constructed with <c>isFiscalYearEnd: true</c> successfully initialises
    /// without throwing, confirming that the end-month path of the constructor is reachable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenIsFiscalYearEndIsTrue_ShouldInitialiseWithoutThrowing()
    {
        // Fiscal year ends in January; provider derives the start from the following month.
        var provider = new FiscalWeekQuarterProvider(1, DayOfWeek.Saturday, isFiscalYearEnd: true);
        Assert.IsNotNull(provider);
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>month</c> is above the valid range (1–12).
    /// </summary>
    [TestMethod]
    [DataRow(13)]
    [DataRow(100)]
    public void Ctor_WhenMonthIsAboveValidRange_ShouldThrowException(int month)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new FiscalWeekQuarterProvider(month));
    }
    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>month</c> is below the valid range (1–12).
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Ctor_WhenMonthIsBelowValidRange_ShouldThrowException(int month)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new FiscalWeekQuarterProvider(month));
    }

    /// <summary>
    /// Verifies that two providers constructed with the same parameters but different
    /// <see cref="FiscalWeekPattern" /> values produce identical quarter boundaries and 53-week
    /// detection results, confirming that the pattern does not affect quarter boundary arithmetic.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPatternDiffers_ShouldNotAffectQuarterBoundaries()
    {
        var p544 = new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks544);
        var p445 = new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false, pattern: FiscalWeekPattern.Weeks445);

        Assert.AreEqual(p544.GetQuarterStart(1, 2023), p445.GetQuarterStart(1, 2023));
        Assert.AreEqual(p544.GetQuarterEnd(4, 2023), p445.GetQuarterEnd(4, 2023));
        Assert.AreEqual(p544.Is53WeekFiscalYear(2023), p445.Is53WeekFiscalYear(2023));
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>pattern</c> is not a defined <see cref="FiscalWeekPattern" /> value.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPatternIsUndefined_ShouldThrowException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false, pattern: (FiscalWeekPattern)99));
    }

    /// <summary>
    /// Verifies that the <see cref="FiscalWeekQuarterProvider.Pattern" /> property returns the value
    /// supplied to the constructor for each defined <see cref="FiscalWeekPattern" />.
    /// </summary>
    [TestMethod]
    [DataRow(FiscalWeekPattern.Weeks544)]
    [DataRow(FiscalWeekPattern.Weeks454)]
    [DataRow(FiscalWeekPattern.Weeks445)]
    public void Pattern_WhenConstructed_ShouldReturnSuppliedPattern(FiscalWeekPattern pattern)
    {
        var provider = new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false, pattern: pattern);
        Assert.AreEqual(pattern, provider.Pattern);
    }

    /// <summary>
    /// Verifies that the default <see cref="FiscalWeekQuarterProvider.Pattern" /> is
    /// <see cref="FiscalWeekPattern.Weeks445" /> when no pattern is specified.
    /// </summary>
    [TestMethod]
    public void Pattern_WhenConstructedWithDefaultPattern_ShouldReturnWeeks445()
    {
        var provider = new FiscalWeekQuarterProvider(1, DayOfWeek.Sunday, isFiscalYearEnd: false);
        Assert.AreEqual(FiscalWeekPattern.Weeks445, provider.Pattern);
    }

}
