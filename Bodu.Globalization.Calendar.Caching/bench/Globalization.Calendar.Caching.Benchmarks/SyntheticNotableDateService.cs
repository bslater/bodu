// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntheticNotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching.Benchmarks;

/// <summary>
/// An <see cref="INotableDateService" /> that deterministically emits a configurable number of occurrences per civil
/// year, spread across the year and alternating between two categories, so cache benchmarks control the payload size
/// without depending on real calendar data.
/// </summary>
internal sealed class SyntheticNotableDateService
    : INotableDateService
{
    /// <summary>The number of occurrences emitted per civil year.</summary>
    private readonly int _occurrencesPerYear;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntheticNotableDateService" /> class.
    /// </summary>
    /// <param name="occurrencesPerYear">The number of occurrences emitted per civil year.</param>
    public SyntheticNotableDateService(int occurrencesPerYear) =>
        _occurrencesPerYear = occurrencesPerYear;

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory) =>
        Resolve(new DateRange(date, date), territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory)
    {
        List<NotableDate> occurrences = new();
        for (int year = range.StartDate.Year; year <= range.EndDate.Year; year++)
        {
            for (int i = 0; i < _occurrencesPerYear; i++)
            {
                NotableDate occurrence = BuildOccurrence(year, i);
                if (occurrence.Date >= range.StartDate && occurrence.Date <= range.EndDate)
                    occurrences.Add(occurrence);
            }
        }

        return occurrences;
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory, NotableDateFilter filter) =>
        [.. Resolve(date, territory).Where(filter.Matches)];

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory, NotableDateFilter filter) =>
        [.. Resolve(range, territory).Where(filter.Matches)];

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedTerritories() => new[] { "US" };

    /// <inheritdoc />
    public IReadOnlyList<CalendarSystem> GetSupportedCalendars() => new[] { CalendarSystem.Gregorian };

    /// <summary>
    /// Builds one deterministic occurrence, spread across the year by index and alternating between the public-holiday
    /// and observance categories so filtered benchmarks can select half the payload.
    /// </summary>
    /// <param name="year">The civil year.</param>
    /// <param name="index">The occurrence's index within the year.</param>
    /// <returns>The occurrence.</returns>
    private NotableDate BuildOccurrence(int year, int index)
    {
        int dayOfYear = 1 + (index * 364 / Math.Max(1, _occurrencesPerYear));
        return new NotableDate(
            new DateOnly(year, 1, 1).AddDays(dayOfYear - 1),
            ActualDate: null,
            IsObserved: false,
            new NotableDateRuleIdentity("bench.resource", $"rule-{index:D4}", "default"),
            $"Synthetic day {index}",
            "US",
            index % 2 == 0 ? NotableDateCategory.PublicHoliday : NotableDateCategory.Observance,
            Priority: 100,
            DurationDays: 1,
            IsNonWorkingDay: index % 2 == 0,
            Tags: Array.Empty<string>(),
            AdjustmentPolicyId: null,
            AdjustmentReason: null);
    }
}
