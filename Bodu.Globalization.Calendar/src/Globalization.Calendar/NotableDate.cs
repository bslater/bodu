using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Represents a single resolved occurrence of a notable date — the output produced by applying a <see cref="NotableDateRule" /> to a
	/// specific year, territory, and calendar context.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A <see cref="NotableDate" /> is immutable and self-describing: it carries the resolved <see cref="Date" />, the originating rule
	/// metadata (<see cref="Name" />, <see cref="Category" />, <see cref="Tags" />), the contextual scoping (<see cref="TerritoryCode" />,
	/// <see cref="CalendarType" />), and — when applicable — the <see cref="AdjustmentReason" /> explaining why the date was shifted from
	/// its originally calculated position.
	/// </para>
	/// <para>
	/// Multi-day events such as Hanukkah or Ramadan are modelled by setting <see cref="DurationDays" /> to a value greater than one. The
	/// <see cref="Date" /> property always points at the anchor day; consumers iterating dates within the span should walk
	/// <see cref="EndDate" /> inclusively.
	/// </para>
	/// </remarks>
	public sealed record NotableDate
	{
#if NET7_0_OR_GREATER

		/// <summary>
		/// Gets the resolved anchor date for the notable date occurrence.
		/// </summary>
		public required DateTime Date { get; init; }

		/// <summary>
		/// Gets the canonical name of the notable date as authored on the originating <see cref="NotableDateRule" />.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// Gets the primary category of the notable date.
		/// </summary>
		public required NotableDateCategory Category { get; init; }
#else

		/// <summary>
		/// Gets the resolved anchor date for the notable date occurrence.
		/// </summary>
		public DateTime Date { get; init; }

		/// <summary>
		/// Gets the canonical name of the notable date.
		/// </summary>
		public string Name { get; init; } = string.Empty;

		/// <summary>
		/// Gets the primary category of the notable date.
		/// </summary>
		public NotableDateCategory Category { get; init; }
#endif

		/// <summary>
		/// Gets the duration of the notable date in days. Defaults to one. Multi-day spans use values greater than one and may be enumerated
		/// from <see cref="Date" /> through <see cref="EndDate" /> inclusively.
		/// </summary>
		public int DurationDays { get; init; } = 1;

		/// <summary>
		/// Gets the inclusive last day of the span, derived from <see cref="Date" /> and <see cref="DurationDays" />.
		/// </summary>
		public DateTime EndDate => Date.AddDays(Math.Max(0, DurationDays - 1));

		/// <summary>
		/// Gets the optional <see cref="AdjustmentReason" /> describing why the date was shifted from its original calculated position by a
		/// triggered <see cref="ObservanceAdjustment" />, or <see langword="null" /> when the date was not adjusted.
		/// </summary>
		public AdjustmentReason? AdjustmentReason { get; init; }

		/// <summary>
		/// Gets a value indicating whether <see cref="Date" /> was shifted from its originally calculated position.
		/// </summary>
		public bool WasAdjusted => AdjustmentReason is not null;

		/// <summary>
		/// Gets a value indicating whether the notable date is treated as a non-working day for the active territory.
		/// </summary>
		public bool IsNonWorkingDay { get; init; }

		/// <summary>
		/// Gets the calendar system associated with the notable date (e.g. <c>System.Globalization.GregorianCalendar</c>), or
		/// <see langword="null" /> when the date is calendar-agnostic.
		/// </summary>
		public Type? CalendarType { get; init; }

		/// <summary>
		/// Gets the territory the notable date applies to (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>), or <see langword="null" /> when the date is
		/// global.
		/// </summary>
		public string? TerritoryCode { get; init; }

		/// <summary>
		/// Gets the optional non-exclusive tags inherited from the originating <see cref="NotableDateRule" />.
		/// </summary>
		public ImmutableHashSet<string> Tags { get; init; } = ImmutableHashSet<string>.Empty;

		/// <summary>
		/// Gets an optional comment inherited from the originating <see cref="NotableDateRule" />.
		/// </summary>
		public string? Comment { get; init; }

		/// <summary>
		/// Gets the display name for the notable date, qualified by territory and calendar suffixes when those are scoped.
		/// </summary>
		/// <remarks>
		/// This property does not perform translation. To localise a notable date's name into the active culture, supply an
		/// <see cref="INotableDateNameLocalizer" /> when constructing the <see cref="NotableDateService" />.
		/// </remarks>
		public string DisplayName
		{
			get
			{
				if (TerritoryCode is null && CalendarType is null)
					return Name;

				string? calendarSuffix = CalendarType?.Name.Replace("Calendar", string.Empty, StringComparison.OrdinalIgnoreCase);
				var parts = new[] { TerritoryCode, calendarSuffix }.Where(s => !string.IsNullOrEmpty(s));
				return $"{Name} ({string.Join(", ", parts)})";
			}
		}
	}
}
