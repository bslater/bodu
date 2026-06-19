// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleApplicabilityTests.AppliesTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class RuleApplicabilityTests
{
    /// <summary>
    /// Verifies that <see cref="RuleApplicability.AppliesTo" /> enforces the inclusive <c>fromYear</c>/<c>toYear</c>
    /// window, returning the expected result for each year relative to the bounds.
    /// </summary>
    /// <param name="year">The Gregorian year being resolved.</param>
    /// <param name="fromYear">The lower bound, or <c>-1</c> for none.</param>
    /// <param name="toYear">The upper bound, or <c>-1</c> for none.</param>
    /// <param name="expected">The expected applicability outcome.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(2025, -1, -1, true)]    // unbounded
    [DataRow(2025, 2020, 2030, true)] // interior
    [DataRow(2020, 2020, 2030, true)] // lower boundary inclusive
    [DataRow(2030, 2020, 2030, true)] // upper boundary inclusive
    [DataRow(2019, 2020, 2030, false)] // below fromYear
    [DataRow(2031, 2020, 2030, false)] // above toYear
    [DataRow(2025, 2025, -1, true)]   // lower bound only, on boundary
    [DataRow(2024, 2025, -1, false)]  // lower bound only, below
    [DataRow(2025, -1, 2025, true)]   // upper bound only, on boundary
    [DataRow(2026, -1, 2025, false)]  // upper bound only, above
    public void AppliesTo_WhenYearWindow_ShouldMatchExpected(int year, int fromYear, int toYear, bool expected)
    {
        RuleApplicability applicability = Years(
            fromYear < 0 ? null : fromYear,
            toYear < 0 ? null : toYear);

        Assert.AreEqual(expected, applicability.AppliesTo("XX", year));
    }

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.OnlyYears" /> restricts applicability to the listed years and excludes
    /// every other year.
    /// </summary>
    /// <param name="year">The Gregorian year being resolved.</param>
    /// <param name="expected">The expected applicability outcome.</param>
    [TestMethod]
    [DataRow(2024, true)]
    [DataRow(2026, true)]
    [DataRow(2025, false)]
    [DataRow(2030, false)]
    public void AppliesTo_WhenOnlyYears_ShouldMatchOnlyListedYears(int year, bool expected)
    {
        RuleApplicability applicability = Years(null, null, onlyYears: [2024, 2026]);

        Assert.AreEqual(expected, applicability.AppliesTo("XX", year));
    }

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.ExceptYears" /> suppresses applicability for the listed years and
    /// applies for every other year.
    /// </summary>
    /// <param name="year">The Gregorian year being resolved.</param>
    /// <param name="expected">The expected applicability outcome.</param>
    [TestMethod]
    [DataRow(2025, false)]
    [DataRow(2024, true)]
    [DataRow(2026, true)]
    public void AppliesTo_WhenExceptYears_ShouldSuppressListedYears(int year, bool expected)
    {
        RuleApplicability applicability = Years(null, null, exceptYears: [2025]);

        Assert.AreEqual(expected, applicability.AppliesTo("XX", year));
    }

    /// <summary>
    /// Verifies that an <c>ExceptYear</c> inside a <c>fromYear</c>/<c>toYear</c> window is suppressed while the rest of
    /// the window remains applicable.
    /// </summary>
    /// <param name="year">The Gregorian year being resolved.</param>
    /// <param name="expected">The expected applicability outcome for the 2020-2030 window excepting 2025.</param>
    [TestMethod]
    [DataRow(2024, true)]   // inside window
    [DataRow(2025, false)]  // excepted year
    [DataRow(2026, true)]   // inside window
    [DataRow(2019, false)]  // below window
    public void AppliesTo_WhenWindowWithExceptYear_ShouldSuppressOnlyThatYear(int year, bool expected)
    {
        RuleApplicability applicability = Years(2020, 2030, exceptYears: [2025]);

        Assert.AreEqual(expected, applicability.AppliesTo("XX", year));
    }

    /// <summary>
    /// Verifies that <see cref="RuleApplicability.AppliesTo" /> throws <see cref="ArgumentNullException" /> when the
    /// territory is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AppliesTo_WhenTerritoryIsNull_ShouldThrowExactly()
    {
        RuleApplicability applicability = Years(null, null);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = applicability.AppliesTo(null!, 2025);
        });
    }
}
