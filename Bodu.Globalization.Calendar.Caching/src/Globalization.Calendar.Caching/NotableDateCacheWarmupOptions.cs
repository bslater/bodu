// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheWarmupOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Configures the startup cache warm-up registered through <c>AddNotableDateCacheWarmup</c>: the territories to warm
/// and the span of civil years they are warmed over.
/// </summary>
/// <remarks>
/// The warmed span defaults to a rolling window around the current UTC civil year — <c>[year −
/// <see cref="YearsBehind" />, year + <see cref="YearsAhead" />]</c>, evaluated when the warm-up runs — so a long-lived
/// deployment always warms the years users actually query. Either bound can be pinned with <see cref="FirstYear" /> or
/// <see cref="LastYear" />; a pinned bound replaces its rolling default independently of the other.
/// </remarks>
public sealed class NotableDateCacheWarmupOptions
{
    /// <summary>
    /// Gets the territory codes to warm.
    /// </summary>
    /// <value>
    /// The territory list; empty by default, which fails validation so an unconfigured warm-up cannot register.
    /// </value>
    public IList<string> Territories { get; } = new List<string>();

    /// <summary>
    /// Gets or sets the number of civil years before the current UTC year the warmed span reaches back, used when
    /// <see cref="FirstYear" /> is not set.
    /// </summary>
    /// <value>The rolling years behind; defaults to 0. Must not be negative.</value>
    public int YearsBehind { get; set; }

    /// <summary>
    /// Gets or sets the number of civil years after the current UTC year the warmed span reaches forward, used when
    /// <see cref="LastYear" /> is not set.
    /// </summary>
    /// <value>The rolling years ahead; defaults to 1. Must not be negative.</value>
    public int YearsAhead { get; set; } = 1;

    /// <summary>
    /// Gets or sets the fixed inclusive first civil year of the warmed span, replacing the rolling
    /// <see cref="YearsBehind" /> start.
    /// </summary>
    /// <value>The fixed first year, or <see langword="null" /> to derive it from <see cref="YearsBehind" />.</value>
    public int? FirstYear { get; set; }

    /// <summary>
    /// Gets or sets the fixed inclusive last civil year of the warmed span, replacing the rolling
    /// <see cref="YearsAhead" /> end.
    /// </summary>
    /// <value>The fixed last year, or <see langword="null" /> to derive it from <see cref="YearsAhead" />.</value>
    public int? LastYear { get; set; }

    /// <summary>
    /// Validates the option values, throwing when a rule is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="Territories" /> is empty or contains a blank code, when <see cref="YearsBehind" /> or
    /// <see cref="YearsAhead" /> is negative, when a fixed year is outside the representable calendar range, or when
    /// <see cref="FirstYear" /> and <see cref="LastYear" /> are both set and inverted.
    /// </exception>
    /// <remarks>
    /// This throwing form preserves the <c>ParamName</c> of the offending option. The dependency-injection registration
    /// instead wires <see cref="TryValidate(out string?)" /> into <c>ValidateOnStart</c>.
    /// </remarks>
    public void Validate()
    {
        if (!TryValidate(out string? error, out string? paramName))
            throw new ArgumentException(error, paramName);
    }

    /// <summary>
    /// Attempts to validate the options without throwing, reporting the first invariant that is violated.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the first violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    public bool TryValidate(out string? error) =>
        TryValidate(out error, out _);

    /// <summary>
    /// Validates the invariants, reporting the first violation and the name of the offending option.
    /// </summary>
    /// <param name="error">The first violation's message, or <see langword="null" /> when everything holds.</param>
    /// <param name="paramName">The offending option's name, or <see langword="null" /> when everything holds.</param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    private bool TryValidate(out string? error, out string? paramName)
    {
        if (Territories is null || Territories.Count == 0)
        {
            error = CalendarCachingResourceStrings.Arg_Invalid_WarmupTerritoriesEmpty;
            paramName = nameof(Territories);
            return false;
        }

        foreach (string territory in Territories)
        {
            if (string.IsNullOrWhiteSpace(territory))
            {
                error = CalendarCachingResourceStrings.Arg_Invalid_WarmupTerritoryBlank;
                paramName = nameof(Territories);
                return false;
            }
        }

        if (YearsBehind < 0)
        {
            error = string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_WarmupYearsNegative, YearsBehind);
            paramName = nameof(YearsBehind);
            return false;
        }

        if (YearsAhead < 0)
        {
            error = string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_WarmupYearsNegative, YearsAhead);
            paramName = nameof(YearsAhead);
            return false;
        }

        if (FirstYear is { } first && (first < DateOnly.MinValue.Year || first > DateOnly.MaxValue.Year))
        {
            error = string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_WarmupYearOutOfRange, first);
            paramName = nameof(FirstYear);
            return false;
        }

        if (LastYear is { } last && (last < DateOnly.MinValue.Year || last > DateOnly.MaxValue.Year))
        {
            error = string.Format(CultureInfo.CurrentCulture, CalendarCachingResourceStrings.Arg_Invalid_WarmupYearOutOfRange, last);
            paramName = nameof(LastYear);
            return false;
        }

        if (FirstYear is { } firstYear && LastYear is { } lastYear && lastYear < firstYear)
        {
            error = CalendarCachingResourceStrings.Arg_Invalid_WarmupWindowInverted;
            paramName = nameof(LastYear);
            return false;
        }

        error = null;
        paramName = null;
        return true;
    }
}
