// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CountingNotableDateService.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Samples.Caching;

/// <summary>
/// A pass-through <see cref="INotableDateService" /> decorator that counts how many range resolutions reach the
/// inner service, so the scenarios can prove — deterministically, without timing — when the cache absorbed a query.
/// </summary>
public sealed class CountingNotableDateService : INotableDateService
{
    private readonly INotableDateService _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountingNotableDateService" /> class.
    /// </summary>
    /// <param name="inner">The service whose resolutions are counted.</param>
    public CountingNotableDateService(INotableDateService inner) =>
        _inner = inner;

    /// <summary>
    /// Gets the number of range resolutions that reached the inner service.
    /// </summary>
    public int RangeResolutions { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory) =>
        _inner.Resolve(date, territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory)
    {
        RangeResolutions++;
        return _inner.Resolve(range, territory);
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory, NotableDateFilter filter) =>
        _inner.Resolve(date, territory, filter);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory, NotableDateFilter filter)
    {
        RangeResolutions++;
        return _inner.Resolve(range, territory, filter);
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedTerritories() =>
        _inner.GetSupportedTerritories();

    /// <inheritdoc />
    public IReadOnlyList<CalendarSystem> GetSupportedCalendars() =>
        _inner.GetSupportedCalendars();
}
