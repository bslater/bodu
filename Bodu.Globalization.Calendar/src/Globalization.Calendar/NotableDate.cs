// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a single resolved occurrence of a notable date — the output produced by applying a
/// <see cref="NotableDateRule" /> to a specific year, territory, and calendar context.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NotableDate" /> is immutable and self-describing: it carries the resolved <see cref="Date" />, the
/// originating rule metadata (<see cref="Name" />, <see cref="Category" />, <see cref="Tags" />), the contextual
/// scoping (<see cref="TerritoryCode" />, <see cref="CalendarType" />), and — when applicable — the
/// <see cref="AdjustmentReason" /> explaining why the date was shifted from its originally calculated position.
/// </para>
/// <para>
/// Multi-day events such as Hanukkah or Ramadan are modeled by setting <see cref="DurationDays" /> to a value greater
/// than one. The <see cref="Date" /> property always points at the anchor day; consumers iterating dates within the
/// span should walk <see cref="EndDate" /> inclusively.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Iterate notable dates for a territory and display their details:
/// </para>
/// <code>
///<![CDATA[
/// INotableDateService service = new NotableDateService();
/// IReadOnlyList<NotableDate> dates = service.GetNotableDates(2026, territoryCode: "AU");
///
/// foreach (NotableDate date in dates)
/// {
///     Console.WriteLine($"{date.Date:d}  {date.DisplayName}");
///
///     if (date.DurationDays > 1)
///         Console.WriteLine($"  Spans through {date.EndDate:d} ({date.DurationDays} days)");
///
///     if (date.WasAdjusted)
///         Console.WriteLine($"  Adjusted from {date.AdjustmentReason!.OriginalDate:d}");
///
///     if (date.IsNonWorkingDay)
///         Console.WriteLine("  Non-working day");
/// }
///]]>
/// </code>
/// </example>
public sealed record NotableDate
{
#if NET7_0_OR_GREATER

    /// <summary>
    /// Gets the resolved anchor date for the notable date occurrence.
    /// </summary>
    /// <returns>The anchor <see cref="DateTime" />. Required at construction.</returns>
    public required DateTime Date { get; init; }

    /// <summary>
    /// Gets the canonical name of the notable date as authored on the originating <see cref="NotableDateRule" />.
    /// </summary>
    /// <returns>The notable date title. Required at construction; never <see langword="null" />.</returns>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the primary category of the notable date.
    /// </summary>
    /// <returns>One of the defined <see cref="NotableDateCategory" /> values. Required at construction.</returns>
    public required NotableDateCategory Category { get; init; }
#else

	/// <summary>
	/// Gets the resolved anchor date for the notable date occurrence.
	/// </summary>
	/// <returns>The anchor <see cref="DateTime" />.</returns>
	public DateTime Date { get; init; }

	/// <summary>
	/// Gets the canonical name of the notable date.
	/// </summary>
	/// <returns>The notable date title. Defaults to <see cref="string.Empty" /> when not assigned.</returns>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// Gets the primary category of the notable date.
	/// </summary>
	/// <returns>One of the defined <see cref="NotableDateCategory" /> values.</returns>
	public NotableDateCategory Category { get; init; }
#endif

    /// <summary>
    /// Gets the duration of the notable date in days. Defaults to one. Multi-day spans use values greater than one and
    /// may be enumerated from <see cref="Date" /> through <see cref="EndDate" /> inclusively.
    /// </summary>
    /// <returns>A positive day count. The default is <c>1</c>.</returns>
    public int DurationDays { get; init; } = 1;

    /// <summary>
    /// Gets the inclusive last day of the span, derived from <see cref="Date" /> and <see cref="DurationDays" />.
    /// </summary>
    /// <returns>
    /// The final <see cref="DateTime" /> of the span. Equal to <see cref="Date" /> for single-day occurrences.
    /// </returns>
    public DateTime EndDate => Date.AddDays(Math.Max(0, DurationDays - 1));

    /// <summary>
    /// Gets the optional <see cref="AdjustmentReason" /> describing why the date was shifted from its original
    /// calculated position by a triggered <see cref="ObservanceAdjustment" />, or <see langword="null" /> when the date
    /// was not adjusted.
    /// </summary>
    /// <returns>
    /// An <see cref="AdjustmentReason" /> describing the shift, or <see langword="null" /> when the occurrence was not
    /// adjusted.
    /// </returns>
    public AdjustmentReason? AdjustmentReason { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Date" /> was shifted from its originally calculated position.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when an adjustment fired and produced this occurrence; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool WasAdjusted => AdjustmentReason is not null;

    /// <summary>
    /// Gets a value indicating whether the notable date is treated as a non-working day for the active territory.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the resolved occurrence is non-working; otherwise <see langword="false" />.
    /// </returns>
    public bool IsNonWorkingDay { get; init; }

    /// <summary>
    /// Gets the calendar system associated with the notable date (e.g. <c>System.Globalization.GregorianCalendar</c>),
    /// or <see langword="null" /> when the date is calendar-agnostic.
    /// </summary>
    /// <returns>
    /// The CLR <see cref="Type" /> of the associated calendar, or <see langword="null" /> when the occurrence is
    /// calendar-agnostic.
    /// </returns>
    public Type? CalendarType { get; init; }

    /// <summary>
    /// Gets the territory the notable date applies to (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>), or <see langword="null" />
    /// when the date is global.
    /// </summary>
    /// <returns>
    /// The ISO 3166-style territory code, or <see langword="null" /> when the occurrence applies globally.
    /// </returns>
    public string? TerritoryCode { get; init; }

    /// <summary>
    /// Gets the optional non-exclusive tags inherited from the originating <see cref="NotableDateRule" />.
    /// </summary>
    /// <returns>
    /// An immutable hash set of tag values. Empty when the originating rule carries none; never <see langword="null" />
    /// .
    /// </returns>
    public ImmutableHashSet<string> Tags { get; init; } = [];

    /// <summary>
    /// Gets an optional comment inherited from the originating <see cref="NotableDateRule" />.
    /// </summary>
    /// <returns>
    /// The free-form comment text, or <see langword="null" /> when the originating rule did not author one.
    /// </returns>
    public string? Comment { get; init; }

    /// <summary>
    /// Gets the display name for the notable date, qualified by territory and calendar suffixes when those are scoped.
    /// </summary>
    /// <remarks>
    /// This property does not perform translation. To localize a notable date's name into the active culture, supply an
    /// <see cref="INotableDateNameLocalizer" /> when constructing the <see cref="NotableDateService" />.
    /// </remarks>
    /// <returns>
    /// The display name, with parenthesized territory and calendar suffixes appended when applicable. Never
    /// <see langword="null" />.
    /// </returns>
    public string DisplayName
    {
        get
        {
            if (TerritoryCode is null && CalendarType is null)
                return Name;

            var calendarSuffix = CalendarType?.Name.Replace("Calendar", string.Empty, StringComparison.OrdinalIgnoreCase);
            IEnumerable<string?> parts = new[] { TerritoryCode, calendarSuffix }.Where(s => !string.IsNullOrEmpty(s));
            return $"{Name} ({string.Join(", ", parts)})";
        }
    }
}
