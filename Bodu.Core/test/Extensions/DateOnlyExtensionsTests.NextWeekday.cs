// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NextWeekday.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    // =========================================================================
    // NextWeekday(this DateOnly, WorkingDaysOfWeek)
    // =========================================================================

    /// <summary>
    /// Provides cases for NextWeekday under the Saturday/Sunday weekend model.
    /// </summary>
    public static IEnumerable<object[]> NextWeekdaySaturdaySundayTestData()
    {
        // Fri 19 Apr 2024 → next weekday skips Sat/Sun → Mon 22 Apr.
        yield return new object[] { new DateOnly(2024, 4, 19), new DateOnly(2024, 4, 22) };
        // Sat 20 Apr 2024 → Mon 22 Apr.
        yield return new object[] { new DateOnly(2024, 4, 20), new DateOnly(2024, 4, 22) };
        // Sun 21 Apr 2024 → Mon 22 Apr.
        yield return new object[] { new DateOnly(2024, 4, 21), new DateOnly(2024, 4, 22) };
        // Mon 22 Apr 2024 → Tue 23 Apr.
        yield return new object[] { new DateOnly(2024, 4, 22), new DateOnly(2024, 4, 23) };
        // Wed 17 Apr 2024 → Thu 18 Apr.
        yield return new object[] { new DateOnly(2024, 4, 17), new DateOnly(2024, 4, 18) };
    }

    // =========================================================================
    // NextWeekday(this DateOnly, WorkingDaysOfWeek, IWeekendDefinitionProvider?)
    // =========================================================================

    /// <summary>
    /// Verifies that the provider overload falls back to the <see cref="WorkingDaysOfWeek" /> enum value when the provider is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenProviderIsNull_ShouldUseWeekendEnum()
    {
        var input = new DateOnly(2024, 4, 19); // Friday
        DateOnly actual = input.NextWeekday(WorkingDaysOfWeek.MondayToFriday, provider: null);
        Assert.AreEqual(new DateOnly(2024, 4, 22), actual);
    }

    /// <summary>
    /// Verifies that the provider overload still throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="WorkingDaysOfWeek" /> even when a provider is supplied.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenProviderOverload_WeekendIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.NextWeekday((WorkingDaysOfWeek)999, provider: null);
        });
    }

    /// <summary>
    /// Verifies that with a Friday/Saturday weekend, <see cref="DateOnlyExtensions.NextWeekday(DateOnly, WorkingDaysOfWeek)" /> skips both weekend days and returns the following Sunday.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekendIsFridaySaturday_ShouldSkipFridayAndSaturday()
    {
        // Thu 18 Apr 2024 → skip Fri/Sat → Sun 21 Apr.
        var input = new DateOnly(2024, 4, 18);
        DateOnly actual = input.NextWeekday(WorkingDaysOfWeek.SundayToThursday);
        Assert.AreEqual(new DateOnly(2024, 4, 21), actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="WorkingDaysOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekendIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.NextWeekday((WorkingDaysOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextWeekday(DateOnly, WorkingDaysOfWeek)" />, when called with
    /// <see cref="WorkingDaysOfWeek.AllDays" />, advances by exactly one day. Because <see cref="DateTimeExtensions.IsWeekend(DayOfWeek, WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// classifies every day as a weekday under <c>None</c>, the search loop's first iteration moves the cursor forward one day and exits.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekendIsNone_ShouldAdvanceOneDay()
    {
        var input = new DateOnly(2024, 4, 20);

        DateOnly actual = input.NextWeekday(WorkingDaysOfWeek.AllDays);

        Assert.AreEqual(input.AddDays(1), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextWeekday(DateOnly, WorkingDaysOfWeek)" /> returns the next non-weekend date when Saturday and Sunday are defined as the weekend.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NextWeekdaySaturdaySundayTestData))]
    public void NextWeekday_WhenWeekendIsSaturdaySunday_ShouldReturnExpectedDate(DateOnly date, DateOnly expected)
    {
        DateOnly actual = date.NextWeekday(WorkingDaysOfWeek.MondayToFriday);
        Assert.AreEqual(expected, actual);
    }

}
