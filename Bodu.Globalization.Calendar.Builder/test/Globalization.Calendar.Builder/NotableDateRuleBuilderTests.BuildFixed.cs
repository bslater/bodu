// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleBuilderTests.BuildFixed.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Verifies the Fixed-strategy month-token resolution behaviour of <see cref="NotableDateRuleBuilder" />.
/// </summary>
public partial class NotableDateRuleBuilderTests
{
    /// <summary>
    /// Verifies that a numeric string month token (e.g. <c>"5"</c>) is resolved through the integer parse branch
    /// and produces a rule with <see cref="NotableDateRule.Month" /> set and
    /// <see cref="NotableDateRule.CalendarMonthAlias" /> cleared.
    /// </summary>
    [TestMethod]
    public void Build_WhenFixedWithNumericStringMonthToken_ShouldResolveMonthNumber()
    {
        NotableDateRuleBuilder builder = new();
        builder
            .Category(NotableDateCategory.Holiday)
            .Fixed("5", 1);

        NotableDateRule rule = builder.Build("Numeric Month");

        Assert.AreEqual(5, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a numeric string month token of <c>"13"</c> — the inclusive upper bound for non-Gregorian
    /// calendars supporting a thirteenth month — is resolved through the integer parse branch.
    /// </summary>
    [TestMethod]
    public void Build_WhenFixedWithNumericStringMonthThirteen_ShouldResolveMonthNumber()
    {
        NotableDateRuleBuilder builder = new();
        builder
            .Category(NotableDateCategory.Holiday)
            .Fixed("13", 1);

        NotableDateRule rule = builder.Build("Thirteenth Month");

        Assert.AreEqual(13, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that each fixed Hebrew month alias resolves through the alias switch to its numeric month value
    /// with <see cref="NotableDateRule.CalendarMonthAlias" /> cleared.
    /// </summary>
    [TestMethod]
    [DataRow("Tishri", 1)]
    [DataRow("Heshvan", 2)]
    [DataRow("Kislev", 3)]
    [DataRow("Tevet", 4)]
    [DataRow("Shevat", 5)]
    [DataRow("AdarI", 6)]
    [DataRow("AdarII", 7)]
    public void Build_WhenFixedWithHebrewFixedMonthAlias_ShouldResolveToNumericMonth(string token, int expectedMonth)
    {
        NotableDateRuleBuilder builder = new();
        builder
            .Category(NotableDateCategory.Religious)
            .Fixed(token, 15);

        NotableDateRule rule = builder.Build("Hebrew Fixed Test");

        Assert.AreEqual(expectedMonth, rule.Month);
        Assert.IsNull(rule.CalendarMonthAlias);
    }

    /// <summary>
    /// Verifies that a Hebrew month alias that is leap-year dependent (e.g. <c>"Nisan"</c>) falls through both the
    /// integer-parse and fixed-Hebrew arms to the trailing alias-store branch, leaving
    /// <see cref="NotableDateRule.Month" /> <see langword="null" /> and
    /// <see cref="NotableDateRule.CalendarMonthAlias" /> populated.
    /// </summary>
    [TestMethod]
    public void Build_WhenFixedWithLeapDependentHebrewAlias_ShouldStoreCalendarMonthAlias()
    {
        NotableDateRuleBuilder builder = new();
        builder
            .Category(NotableDateCategory.Religious)
            .Fixed("Nisan", 15);

        NotableDateRule rule = builder.Build("Passover");

        Assert.IsNull(rule.Month);
        Assert.AreEqual("Nisan", rule.CalendarMonthAlias);
    }
}
