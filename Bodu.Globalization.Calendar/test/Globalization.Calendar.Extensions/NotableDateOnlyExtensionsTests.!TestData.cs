// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.!TestData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Provides day-of-week samples spanning a single calendar week in 2026 paired with their working-day classification under the
    /// default Saturday/Sunday weekend definition.
    /// </summary>
    /// <returns>A sequence of <c>(DateOnly input, bool isWorkingDay)</c> tuples.</returns>
    public static IEnumerable<object[]> WorkingDayClassificationTestData()
    {
        yield return new object[] { new DateOnly(2026, 1, 5), true };   // Monday
        yield return new object[] { new DateOnly(2026, 1, 6), true };   // Tuesday
        yield return new object[] { new DateOnly(2026, 1, 7), true };   // Wednesday
        yield return new object[] { new DateOnly(2026, 1, 8), true };   // Thursday
        yield return new object[] { new DateOnly(2026, 1, 9), true };   // Friday
        yield return new object[] { new DateOnly(2026, 1, 10), false }; // Saturday
        yield return new object[] { new DateOnly(2026, 1, 11), false }; // Sunday
    }

    /// <summary>
    /// Provides start-of-week, count and expected next-working-day samples for an empty rule set.
    /// </summary>
    /// <returns>A sequence of <c>(DateOnly start, int count, DateOnly expected)</c> tuples.</returns>
    public static IEnumerable<object[]> NextWorkingDayCountTestData()
    {
        yield return new object[] { new DateOnly(2026, 1, 5), 1, new DateOnly(2026, 1, 6) };
        yield return new object[] { new DateOnly(2026, 1, 5), 2, new DateOnly(2026, 1, 7) };
        yield return new object[] { new DateOnly(2026, 1, 5), 5, new DateOnly(2026, 1, 12) };
        yield return new object[] { new DateOnly(2026, 1, 9), 1, new DateOnly(2026, 1, 12) };
        yield return new object[] { new DateOnly(2026, 1, 10), 1, new DateOnly(2026, 1, 12) };
    }

    /// <summary>
    /// Provides start-of-week, count and expected previous-working-day samples for an empty rule set.
    /// </summary>
    /// <returns>A sequence of <c>(DateOnly start, int count, DateOnly expected)</c> tuples.</returns>
    public static IEnumerable<object[]> PreviousWorkingDayCountTestData()
    {
        yield return new object[] { new DateOnly(2026, 1, 9), 1, new DateOnly(2026, 1, 8) };
        yield return new object[] { new DateOnly(2026, 1, 9), 5, new DateOnly(2026, 1, 2) };
        yield return new object[] { new DateOnly(2026, 1, 12), 1, new DateOnly(2026, 1, 9) };
        yield return new object[] { new DateOnly(2026, 1, 11), 1, new DateOnly(2026, 1, 9) };
    }

    /// <summary>
    /// Provides signed-days samples for <c>AddWorkingDays</c> against an empty rule set.
    /// </summary>
    /// <returns>A sequence of <c>(DateOnly input, int days, DateOnly expected)</c> tuples.</returns>
    public static IEnumerable<object[]> AddWorkingDaysSignedTestData()
    {
        yield return new object[] { new DateOnly(2026, 1, 5), 0, new DateOnly(2026, 1, 5) };
        yield return new object[] { new DateOnly(2026, 1, 5), 1, new DateOnly(2026, 1, 6) };
        yield return new object[] { new DateOnly(2026, 1, 5), -1, new DateOnly(2026, 1, 2) };
        yield return new object[] { new DateOnly(2026, 1, 5), 5, new DateOnly(2026, 1, 12) };
        yield return new object[] { new DateOnly(2026, 1, 12), -5, new DateOnly(2026, 1, 5) };
    }

    /// <summary>
    /// Provides inclusive ranges and their expected working-day counts under the default Saturday/Sunday weekend definition.
    /// </summary>
    /// <returns>A sequence of <c>(DateOnly start, DateOnly end, int expected)</c> tuples.</returns>
    public static IEnumerable<object[]> WorkingDaysBetweenRangeTestData()
    {
        yield return new object[] { new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), 1 };
        yield return new object[] { new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 10), 0 };
        yield return new object[] { new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 11), 5 };
        yield return new object[] { new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 18), 10 };
        yield return new object[] { new DateOnly(2026, 1, 11), new DateOnly(2026, 1, 5), 5 };
    }

    /// <summary>
    /// Provides territory-scoping samples expressing bidirectional containment: a query matches a rule when either side's
    /// territory contains the other, and a <see langword="null" /> query matches any rule.
    /// </summary>
    /// <returns>A sequence of <c>(string ruleTerritory, string? queryTerritory, bool expectedNonWorking)</c> tuples.</returns>
    public static IEnumerable<object[]> TerritoryForwardingTestData()
    {
        yield return new object[] { "AU", "AU", true };
        yield return new object[] { "AU", "AU-NSW", true };
        yield return new object[] { "AU-NSW", "AU", true };
        yield return new object[] { "AU-NSW", "AU-NSW", true };
        yield return new object[] { "AU-NSW", "AU-VIC", false };
        yield return new object[] { "AU", "NZ", false };
        yield return new object[] { "AU", null!, true };
    }

    /// <summary>
    /// Builds a service whose only rule is a single-day non-working rule on 7 April 2026 (a Tuesday) and returns it.
    /// </summary>
    /// <param name="territory">An optional territory to scope the rule to.</param>
    /// <returns>A configured <see cref="NotableDateService" /> instance.</returns>
    private static NotableDateService BuildHolidayService(string? territory = null) =>
        BuildService(Fixed("Holiday", 4, 7, nonWorking: true, territory: territory));

    /// <summary>
    /// Provides boundary inputs that should throw <see cref="ArgumentOutOfRangeException" /> when adding working days would overrun
    /// the <see cref="DateOnly" /> range.
    /// </summary>
    /// <returns>A sequence of <c>(DateOnly input, int days)</c> tuples, each one expected to throw.</returns>
    public static IEnumerable<object[]> AddWorkingDaysOverflowTestData()
    {
        yield return new object[] { DateOnly.MaxValue, 1 };
        yield return new object[] { DateOnly.MinValue, -1 };
        yield return new object[] { DateOnly.MaxValue.AddDays(-1), 5 };
        yield return new object[] { DateOnly.MinValue.AddDays(1), -5 };
    }

    /// <summary>
    /// Provides single-day range inputs paired with their expected working-day yield count.
    /// </summary>
    /// <returns>A sequence of <c>(DateOnly day, int expectedWorkingYieldCount)</c> tuples.</returns>
    public static IEnumerable<object[]> SingleDayWorkingYieldTestData()
    {
        yield return new object[] { new DateOnly(2026, 1, 5), 1 };
        yield return new object[] { new DateOnly(2026, 1, 6), 1 };
        yield return new object[] { new DateOnly(2026, 1, 9), 1 };
        yield return new object[] { new DateOnly(2026, 1, 10), 0 };
        yield return new object[] { new DateOnly(2026, 1, 11), 0 };
    }

    /// <summary>
    /// Provides single-day range inputs paired with their expected non-working-day yield count.
    /// </summary>
    /// <returns>A sequence of <c>(DateOnly day, int expectedNonWorkingYieldCount)</c> tuples.</returns>
    public static IEnumerable<object[]> SingleDayNonWorkingYieldTestData()
    {
        yield return new object[] { new DateOnly(2026, 1, 5), 0 };
        yield return new object[] { new DateOnly(2026, 1, 9), 0 };
        yield return new object[] { new DateOnly(2026, 1, 10), 1 };
        yield return new object[] { new DateOnly(2026, 1, 11), 1 };
    }
}
