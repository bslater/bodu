// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingNotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateService" /> test double that returns a configurable, deterministic occurrence per requested
/// year and counts how many times each range resolution is invoked, so a decorator's cache hits and misses can be
/// asserted.
/// </summary>
internal sealed class CountingNotableDateService : INotableDateService
{
    /// <summary>The occurrence factory invoked once per requested range, keyed on the range's start year.</summary>
    private readonly Func<int, IReadOnlyList<NotableDate>> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingNotableDateService" /> class.
    /// </summary>
    /// <param name="factory">
    /// A factory producing the occurrences for a requested year, or <see langword="null" /> to return a single New
    /// Year's Day occurrence for the year.
    /// </param>
    public CountingNotableDateService(Func<int, IReadOnlyList<NotableDate>>? factory = null)
    {
        _factory = factory ?? DefaultFactory;
    }

    /// <summary>
    /// Gets the number of times <see cref="Resolve(DateRange, string)" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int ResolveCount { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory) =>
        Resolve(new DateRange(date, date), territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory)
    {
        ResolveCount++;

        List<NotableDate> occurrences = new();
        for (int year = range.StartDate.Year; year <= range.EndDate.Year; year++)
        {
            foreach (NotableDate occurrence in _factory(year))
            {
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
    /// Produces the default single New Year's Day occurrence for a year.
    /// </summary>
    /// <param name="year">The civil year.</param>
    /// <returns>A single occurrence dated 1 January of the year.</returns>
    private static IReadOnlyList<NotableDate> DefaultFactory(int year) =>
        new[] { NotableDateCacheTestFactory.NewYearsDay(year) };
}
