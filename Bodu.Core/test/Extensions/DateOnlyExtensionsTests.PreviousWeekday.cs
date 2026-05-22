// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.PreviousWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    // =========================================================================
    // PreviousWeekday(this DateOnly, WorkingDaysOfWeek)
    // =========================================================================

    /// <summary>
    /// Provides cases for PreviousWeekday under the Saturday/Sunday weekend model.
    /// </summary>
    public static IEnumerable<object[]> PreviousWeekdaySaturdaySundayTestData()
    {
        // Mon 22 Apr 2024 → previous weekday skips Sun/Sat → Fri 19 Apr.
        yield return new object[] { new DateOnly(2024, 4, 22), new DateOnly(2024, 4, 19) };
        // Sun 21 Apr 2024 → Fri 19 Apr.
        yield return new object[] { new DateOnly(2024, 4, 21), new DateOnly(2024, 4, 19) };
        // Sat 20 Apr 2024 → Fri 19 Apr.
        yield return new object[] { new DateOnly(2024, 4, 20), new DateOnly(2024, 4, 19) };
        // Wed 17 Apr 2024 → Tue 16 Apr.
        yield return new object[] { new DateOnly(2024, 4, 17), new DateOnly(2024, 4, 16) };
    }

    // =========================================================================
    // PreviousWeekday(this DateOnly, WorkingDaysOfWeek, IWeekendDefinitionProvider?)
    // =========================================================================

    /// <summary>
    /// Verifies that the provider overload falls back to the <see cref="WorkingDaysOfWeek" /> enum value when the provider is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenProviderIsNull_ShouldUseWeekendEnum()
    {
        var input = new DateOnly(2024, 4, 22); // Monday
        DateOnly actual = input.PreviousWeekday(WorkingDaysOfWeek.MondayToFriday, provider: null);
        Assert.AreEqual(new DateOnly(2024, 4, 19), actual);
    }

    /// <summary>
    /// Verifies that the provider overload still throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="WorkingDaysOfWeek" /> even when a provider is supplied.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenProviderOverload_WeekendIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 22);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousWeekday((WorkingDaysOfWeek)999, provider: null);
        });
    }

    /// <summary>
    /// Verifies that with a Friday/Saturday weekend, <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, WorkingDaysOfWeek)" /> skips both weekend days and returns the preceding Thursday.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekendIsFridaySaturday_ShouldSkipSaturdayAndFriday()
    {
        // Sun 21 Apr 2024 → skip Sat 20, Fri 19 → Thu 18 Apr.
        var input = new DateOnly(2024, 4, 21);
        DateOnly actual = input.PreviousWeekday(WorkingDaysOfWeek.SundayToThursday);
        Assert.AreEqual(new DateOnly(2024, 4, 18), actual);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="WorkingDaysOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekendIsInvalid_ShouldThrowExactly()
    {
        var input = new DateOnly(2024, 4, 22);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.PreviousWeekday((WorkingDaysOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, WorkingDaysOfWeek)" />, when called with
    /// <see cref="WorkingDaysOfWeek.AllDays" />, retreats by exactly one day. Because <see cref="DateTimeExtensions.IsWeekend(DayOfWeek, WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// classifies every day as a weekday under <c>None</c>, the reverse search loop's first iteration moves the cursor back one day and exits.
    /// </summary>
    [TestMethod]
    public void PreviousWeekday_WhenWeekendIsNone_ShouldRetreatOneDay()
    {
        var input = new DateOnly(2024, 4, 21);

        DateOnly actual = input.PreviousWeekday(WorkingDaysOfWeek.AllDays);

        Assert.AreEqual(input.AddDays(-1), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.PreviousWeekday(DateOnly, WorkingDaysOfWeek)" /> returns the prior non-weekend date when Saturday and Sunday are defined as the weekend.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(PreviousWeekdaySaturdaySundayTestData))]
    public void PreviousWeekday_WhenWeekendIsSaturdaySunday_ShouldReturnExpectedDate(DateOnly date, DateOnly expected)
    {
        DateOnly actual = date.PreviousWeekday(WorkingDaysOfWeek.MondayToFriday);
        Assert.AreEqual(expected, actual);
    }

}
