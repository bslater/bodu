// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.NextWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    public static IEnumerable<object[]> NextWeekdaySaturdaySundayDateTimeTestData()
    {
        yield return new object[] { new DateTime(2024, 4, 19), new DateTime(2024, 4, 22) }; // Fri → Mon
        yield return new object[] { new DateTime(2024, 4, 20), new DateTime(2024, 4, 22) }; // Sat → Mon
        yield return new object[] { new DateTime(2024, 4, 21), new DateTime(2024, 4, 22) }; // Sun → Mon
        yield return new object[] { new DateTime(2024, 4, 22), new DateTime(2024, 4, 23) }; // Mon → Tue
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, WorkingDaysOfWeek)" /> preserves the input's <see cref="DateTime.Kind" />.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenCalled_ShouldPreserveInputKind()
    {
        var input = new DateTime(2024, 4, 19, 12, 34, 56, DateTimeKind.Utc);
        DateTime actual = input.NextWeekday(WorkingDaysOfWeek.MondayToFriday);
        Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, WorkingDaysOfWeek)" /> preserves the input's <see cref="DateTime.TimeOfDay" />.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenCalled_ShouldPreserveTimeOfDay()
    {
        var input = new DateTime(2024, 4, 19, 13, 45, 15);
        DateTime actual = input.NextWeekday(WorkingDaysOfWeek.MondayToFriday);
        Assert.AreEqual(new TimeSpan(13, 45, 15), actual.TimeOfDay);
    }

    // =========================================================================
    // Provider overload
    // =========================================================================

    /// <summary>
    /// Verifies that the provider overload falls back to the <see cref="WorkingDaysOfWeek" /> enum value when the provider is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenProviderIsNull_ShouldUseWeekendEnum()
    {
        var input = new DateTime(2024, 4, 19);
        DateTime actual = input.NextWeekday(WorkingDaysOfWeek.MondayToFriday, provider: null);
        Assert.AreEqual(new DateTime(2024, 4, 22), actual);
    }

    /// <summary>
    /// Verifies that the provider overload still throws <see cref="ArgumentOutOfRangeException" /> for an undefined <see cref="WorkingDaysOfWeek" /> even when a provider is supplied.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenProviderOverload_WeekendIsInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.NextWeekday((WorkingDaysOfWeek)999, provider: null);
        });
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeek.Custom" /> with a user-supplied <c>IWeekendDefinitionProvider</c> applies the provider's rule when determining the next weekday.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenUsingCustomProvider_ShouldApplyProviderRule()
    {
        var input = new DateTime(2024, 4, 18); // Thursday
        IWeekendDefinitionProvider provider = new FridayOnlyWeekendProvider();

        // Using the Custom weekend with a FridayOnly provider; Friday 19 Apr is the weekend, so next is Sat 20.
        DateTime actual = input.NextWeekday(WorkingDaysOfWeek.Custom, provider);
        Assert.AreEqual(new DateTime(2024, 4, 20), actual);
    }

    /// <summary>
    /// Verifies that with a Friday/Saturday weekend, <see cref="DateTimeExtensions.NextWeekday(DateTime, WorkingDaysOfWeek)" /> skips both weekend days and returns the following Sunday.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekendIsFridaySaturday_ShouldSkipFridayAndSaturday()
    {
        var input = new DateTime(2024, 4, 18); // Thursday
        DateTime actual = input.NextWeekday(WorkingDaysOfWeek.SundayToThursday);
        Assert.AreEqual(new DateTime(2024, 4, 21), actual); // Sunday
    }

    /// <summary>
    /// Verifies that an undefined <see cref="WorkingDaysOfWeek" /> value throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekendIsInvalid_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.NextWeekday((WorkingDaysOfWeek)999);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, WorkingDaysOfWeek)" />, when called with
    /// <see cref="WorkingDaysOfWeek.AllDays" />, advances by exactly one day. Because <see cref="DateTimeExtensions.IsWeekend(DayOfWeek, WorkingDaysOfWeek, IWeekendDefinitionProvider?)" />
    /// classifies every day as a weekday under <c>None</c>, the search loop's first iteration moves the cursor forward one day and exits.
    /// </summary>
    [TestMethod]
    public void NextWeekday_WhenWeekendIsNone_ShouldAdvanceOneDay()
    {
        var input = new DateTime(2024, 4, 20);

        DateTime actual = input.NextWeekday(WorkingDaysOfWeek.AllDays);

        Assert.AreEqual(input.AddDays(1), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.NextWeekday(DateTime, WorkingDaysOfWeek)" /> returns the next non-weekend date when Saturday and Sunday are defined as the weekend.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NextWeekdaySaturdaySundayDateTimeTestData))]
    public void NextWeekday_WhenWeekendIsSaturdaySunday_ShouldReturnExpectedDate(DateTime input, DateTime expected)
    {
        DateTime actual = input.NextWeekday(WorkingDaysOfWeek.MondayToFriday);
        Assert.AreEqual(expected, actual);
    }

}
