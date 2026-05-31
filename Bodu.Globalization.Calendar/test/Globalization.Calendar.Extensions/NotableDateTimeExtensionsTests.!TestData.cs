// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensionsTests.!TestData.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public partial class NotableDateTimeExtensionsTests
{
    /// <summary>
    /// Provides day-of-week samples spanning a single calendar week in 2026 paired with their working-day classification under the
    /// default Saturday/Sunday weekend definition.
    /// </summary>
    /// <returns>A sequence of <c>(DateTime input, bool isWorkingDay)</c> tuples.</returns>
    public static IEnumerable<object[]> WorkingDayClassificationTestData()
    {
        yield return new object[] { new DateTime(2026, 1, 5), true };   // Monday
        yield return new object[] { new DateTime(2026, 1, 6), true };   // Tuesday
        yield return new object[] { new DateTime(2026, 1, 7), true };   // Wednesday
        yield return new object[] { new DateTime(2026, 1, 8), true };   // Thursday
        yield return new object[] { new DateTime(2026, 1, 9), true };   // Friday
        yield return new object[] { new DateTime(2026, 1, 10), false }; // Saturday
        yield return new object[] { new DateTime(2026, 1, 11), false }; // Sunday
    }

    /// <summary>
    /// Provides start-of-week, count and expected next-working-day samples for an empty rule set.
    /// </summary>
    /// <returns>A sequence of <c>(DateTime start, int count, DateTime expected)</c> tuples.</returns>
    public static IEnumerable<object[]> NextWorkingDayCountTestData()
    {
        yield return new object[] { new DateTime(2026, 1, 5), 1, new DateTime(2026, 1, 6) };  // Mon → Tue
        yield return new object[] { new DateTime(2026, 1, 5), 2, new DateTime(2026, 1, 7) };  // Mon → Wed
        yield return new object[] { new DateTime(2026, 1, 5), 5, new DateTime(2026, 1, 12) }; // Mon → next Mon (skipping weekend)
        yield return new object[] { new DateTime(2026, 1, 9), 1, new DateTime(2026, 1, 12) }; // Fri → Mon
        yield return new object[] { new DateTime(2026, 1, 10), 1, new DateTime(2026, 1, 12) };// Sat → Mon
    }

    /// <summary>
    /// Provides start-of-week, count and expected previous-working-day samples for an empty rule set.
    /// </summary>
    /// <returns>A sequence of <c>(DateTime start, int count, DateTime expected)</c> tuples.</returns>
    public static IEnumerable<object[]> PreviousWorkingDayCountTestData()
    {
        yield return new object[] { new DateTime(2026, 1, 9), 1, new DateTime(2026, 1, 8) };  // Fri → Thu
        yield return new object[] { new DateTime(2026, 1, 9), 5, new DateTime(2026, 1, 2) };  // Fri → previous Fri
        yield return new object[] { new DateTime(2026, 1, 12), 1, new DateTime(2026, 1, 9) }; // Mon → previous Fri
        yield return new object[] { new DateTime(2026, 1, 11), 1, new DateTime(2026, 1, 9) }; // Sun → Fri
    }

    /// <summary>
    /// Provides signed-days samples for <c>AddWorkingDays</c> against an empty rule set.
    /// </summary>
    /// <returns>A sequence of <c>(DateTime input, int days, DateTime expected)</c> tuples.</returns>
    public static IEnumerable<object[]> AddWorkingDaysSignedTestData()
    {
        yield return new object[] { new DateTime(2026, 1, 5), 0, new DateTime(2026, 1, 5) };  // Zero
        yield return new object[] { new DateTime(2026, 1, 5), 1, new DateTime(2026, 1, 6) };  // +1
        yield return new object[] { new DateTime(2026, 1, 5), -1, new DateTime(2026, 1, 2) }; // -1 across weekend
        yield return new object[] { new DateTime(2026, 1, 5), 5, new DateTime(2026, 1, 12) }; // +5 spans weekend
        yield return new object[] { new DateTime(2026, 1, 12), -5, new DateTime(2026, 1, 5) };// -5 spans weekend
    }

    /// <summary>
    /// Provides inclusive ranges and their expected working-day counts under the default Saturday/Sunday weekend definition.
    /// </summary>
    /// <returns>A sequence of <c>(DateTime start, DateTime end, int expected)</c> tuples.</returns>
    public static IEnumerable<object[]> WorkingDaysBetweenRangeTestData()
    {
        yield return new object[] { new DateTime(2026, 1, 5), new DateTime(2026, 1, 5), 1 };   // Single working day
        yield return new object[] { new DateTime(2026, 1, 10), new DateTime(2026, 1, 10), 0 }; // Single non-working day
        yield return new object[] { new DateTime(2026, 1, 5), new DateTime(2026, 1, 11), 5 };  // Full week
        yield return new object[] { new DateTime(2026, 1, 5), new DateTime(2026, 1, 18), 10 }; // Two weeks
        yield return new object[] { new DateTime(2026, 1, 11), new DateTime(2026, 1, 5), 5 };  // Reversed boundaries
    }

    /// <summary>
    /// Provides <see cref="DateTimeKind" /> values for tests that assert kind preservation across walk and snap operations.
    /// </summary>
    /// <returns>A sequence of single-element <c>(DateTimeKind)</c> tuples.</returns>
    public static IEnumerable<object[]> DateTimeKindPreservationTestData()
    {
        yield return new object[] { DateTimeKind.Unspecified };
        yield return new object[] { DateTimeKind.Utc };
        yield return new object[] { DateTimeKind.Local };
    }

    /// <summary>
    /// Provides territory-scoping samples expressing bidirectional containment: a query matches a rule when either side's
    /// territory contains the other, and a <see langword="null" /> query matches any rule.
    /// </summary>
    /// <returns>A sequence of <c>(string ruleTerritory, string? queryTerritory, bool expectedNonWorking)</c> tuples.</returns>
    public static IEnumerable<object[]> TerritoryForwardingTestData()
    {
        yield return new object[] { "AU", "AU", true };           // Same scope
        yield return new object[] { "AU", "AU-NSW", true };       // Rule scope contains query scope
        yield return new object[] { "AU-NSW", "AU", true };       // Query scope contains rule scope
        yield return new object[] { "AU-NSW", "AU-NSW", true };   // Same subdivision
        yield return new object[] { "AU-NSW", "AU-VIC", false };  // Sibling subdivisions
        yield return new object[] { "AU", "NZ", false };          // Unrelated countries
        yield return new object[] { "AU", null!, true };          // Null query bypasses scope filter
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
    /// the <see cref="DateTime" /> range.
    /// </summary>
    /// <returns>A sequence of <c>(DateTime input, int days)</c> tuples, each one expected to throw.</returns>
    public static IEnumerable<object[]> AddWorkingDaysOverflowTestData()
    {
        yield return new object[] { DateTime.MaxValue, 1 };   // positive overflow past MaxValue
        yield return new object[] { DateTime.MinValue, -1 };  // negative overflow past MinValue
        yield return new object[] { DateTime.MaxValue.AddDays(-1), 5 };  // multi-step positive overflow
        yield return new object[] { DateTime.MinValue.AddDays(1), -5 };  // multi-step negative overflow
    }

    /// <summary>
    /// Provides single-day range inputs paired with their expected working-day yield count.
    /// </summary>
    /// <returns>A sequence of <c>(DateTime day, int expectedWorkingYieldCount)</c> tuples.</returns>
    public static IEnumerable<object[]> SingleDayWorkingYieldTestData()
    {
        yield return new object[] { new DateTime(2026, 1, 5), 1 };  // Monday
        yield return new object[] { new DateTime(2026, 1, 6), 1 };  // Tuesday
        yield return new object[] { new DateTime(2026, 1, 9), 1 };  // Friday
        yield return new object[] { new DateTime(2026, 1, 10), 0 }; // Saturday
        yield return new object[] { new DateTime(2026, 1, 11), 0 }; // Sunday
    }

    /// <summary>
    /// Provides single-day range inputs paired with their expected non-working-day yield count.
    /// </summary>
    /// <returns>A sequence of <c>(DateTime day, int expectedNonWorkingYieldCount)</c> tuples.</returns>
    public static IEnumerable<object[]> SingleDayNonWorkingYieldTestData()
    {
        yield return new object[] { new DateTime(2026, 1, 5), 0 };  // Monday
        yield return new object[] { new DateTime(2026, 1, 9), 0 };  // Friday
        yield return new object[] { new DateTime(2026, 1, 10), 1 }; // Saturday
        yield return new object[] { new DateTime(2026, 1, 11), 1 }; // Sunday
    }
}
