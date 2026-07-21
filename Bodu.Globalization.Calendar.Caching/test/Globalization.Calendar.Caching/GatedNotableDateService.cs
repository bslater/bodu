// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GatedNotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateService" /> test double whose range resolution blocks on a gate and counts invocations, so
/// a test can pile up concurrent callers and assert how many computations actually ran. Optionally throws on the first
/// invocation to exercise failure paths.
/// </summary>
internal sealed class GatedNotableDateService
    : INotableDateService
{
    /// <summary>The gate every resolution waits on before returning.</summary>
    private readonly ManualResetEventSlim _gate = new(initialState: false);

    /// <summary>Whether the first invocation throws instead of returning.</summary>
    private readonly bool _throwOnFirstCall;

    /// <summary>The number of range resolutions started.</summary>
    private int _resolveCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatedNotableDateService" /> class.
    /// </summary>
    /// <param name="throwOnFirstCall">
    /// <see langword="true" /> to throw <see cref="InvalidOperationException" /> from the first resolution.
    /// </param>
    public GatedNotableDateService(bool throwOnFirstCall = false)
    {
        _throwOnFirstCall = throwOnFirstCall;
    }

    /// <summary>
    /// Gets the number of range resolutions started.
    /// </summary>
    /// <value>The invocation count.</value>
    public int ResolveCount => Volatile.Read(ref _resolveCount);

    /// <summary>
    /// Releases every caller waiting on the gate.
    /// </summary>
    public void Open() => _gate.Set();

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory) =>
        Resolve(new DateRange(date, date), territory);

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory)
    {
        int call = Interlocked.Increment(ref _resolveCount);
        _gate.Wait(TimeSpan.FromSeconds(30));

        if (_throwOnFirstCall && call == 1)
            throw new InvalidOperationException("Simulated computation failure.");

        return new[] { NotableDateCacheTestFactory.NewYearsDay(range.StartDate.Year) };
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
}
