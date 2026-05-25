// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.ReversedRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Pins the strict reversed-range policy: every range-accepting public API on <see cref="NotableDateService" /> must
/// throw <see cref="ArgumentException" /> (or a derived <see cref="ArgumentOutOfRangeException" />) when the supplied
/// end date precedes the start date. The legacy <c>GetNotableDates(start, end, …)</c> overloads previously swapped
/// reversed arguments; the consolidation makes refusal the uniform contract so authoring errors are surfaced rather
/// than masked.
/// </summary>
public partial class NotableDateServiceTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateService.GetNotableDates(DateTime, DateTime, string?, Type?)" /> throws
    /// when <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRangeIsReversed_ShouldThrow()
    {
        NotableDateService service = BuildService(Fixed("Midsummer", 6, 21));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = service.GetNotableDates(new DateTime(2026, 6, 30), new DateTime(2026, 6, 1));
        });
    }

    /// <summary>
    /// Verifies that the filtered range overload also throws when the range is reversed.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenRangeIsReversed_WithFilter_ShouldThrow()
    {
        NotableDateService service = BuildService(Fixed("Midsummer", 6, 21));
        NotableDateFilter holidayFilter = NotableDateFilter.ForAnyCategory(NotableDateCategory.Holiday);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = service.GetNotableDates(
                new DateTime(2026, 6, 30), new DateTime(2026, 6, 1), holidayFilter);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.ResolveNotableDatesInRange" /> throws when the range is reversed
    /// (the underlying <see cref="RangeResolution.NotableDateRangeRequest" /> validates and throws).
    /// </summary>
    [TestMethod]
    public void ResolveNotableDatesInRange_WhenRangeIsReversed_ShouldThrow()
    {
        NotableDateService service = BuildService(Fixed("Midsummer", 6, 21));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = service.ResolveNotableDatesInRange(
                new DateTime(2026, 6, 30), new DateTime(2026, 6, 1));
        });
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.IsRangeResolved" /> throws when the range is reversed rather than
    /// silently returning <see langword="false" /> for an inverted <see cref="DateRange" />.
    /// </summary>
    [TestMethod]
    public void IsRangeResolved_WhenRangeIsReversed_ShouldThrow()
    {
        NotableDateService service = BuildService(Fixed("Midsummer", 6, 21));

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = service.IsRangeResolved(new DateTime(2026, 6, 30), new DateTime(2026, 6, 1));
        });
    }
}
