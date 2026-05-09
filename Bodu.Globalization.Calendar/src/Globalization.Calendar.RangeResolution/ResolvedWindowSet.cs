// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolvedWindowSet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Maintains the union of chronological windows that have been resolved by the prototype range-resolution pipeline so a consumer
/// can introspect what is known to the service without re-querying.
/// </summary>
/// <remarks>
/// <para>
/// The set keeps a sorted list of disjoint <see cref="DateRange" /> intervals. Adding a new range merges any existing intervals
/// that overlap or are immediately adjacent so the resulting list always contains the minimum number of disjoint intervals
/// describing the same coverage.
/// </para>
/// <para>
/// This type is not thread-safe. The <see cref="NotableDateService" /> coordinates concurrent access externally.
/// </para>
/// </remarks>
internal sealed class ResolvedWindowSet
{
	private readonly List<DateRange> _ranges = new();

	/// <summary>
	/// Gets the disjoint chronological windows currently tracked, in ascending order of <see cref="DateRange.StartDate" />.
	/// </summary>
	public IReadOnlyList<DateRange> Ranges => _ranges;

	/// <summary>
	/// Adds the supplied range to the set, merging it with any overlapping or adjacent existing intervals.
	/// </summary>
	/// <param name="range">The range to add. Inverted ranges are silently ignored.</param>
	public void Add(DateRange range)
	{
		if (!range.IsValid) return;

		DateTime start = range.StartDate.Date;
		DateTime end = range.EndDate.Date;

		List<DateRange> retained = new(_ranges.Count + 1);
		var merged = false;

		foreach (DateRange existing in _ranges)
		{
			DateTime existingStart = existing.StartDate.Date;
			DateTime existingEnd = existing.EndDate.Date;

			// Disjoint and earlier — keep as-is.
			if (existingEnd.AddDays(1) < start)
			{
				retained.Add(existing);
				continue;
			}

			// Disjoint and later — keep as-is (we may still be merging earlier ones).
			if (existingStart > end.AddDays(1))
			{
				retained.Add(existing);
				continue;
			}

			// Overlap or adjacency: extend the candidate window to cover the existing range and drop it from retained.
			if (existingStart < start) start = existingStart;
			if (existingEnd > end) end = existingEnd;
			merged = true;
		}

		retained.Add(new DateRange(start, end));
		retained.Sort(static (a, b) => a.StartDate.Date.CompareTo(b.StartDate.Date));

		_ranges.Clear();
		_ranges.AddRange(retained);

		_ = merged;
	}

	/// <summary>
	/// Determines whether the supplied date is covered by any tracked range.
	/// </summary>
	/// <param name="date">The date to test.</param>
	/// <returns><see langword="true" /> when at least one tracked range contains the date.</returns>
	public bool Contains(DateTime date)
	{
		foreach (DateRange range in _ranges)
		{
			if (range.Contains(date)) return true;
		}

		return false;
	}

	/// <summary>
	/// Determines whether <paramref name="range" /> is fully covered by the union of tracked intervals.
	/// </summary>
	/// <param name="range">The range whose coverage is being tested.</param>
	/// <returns><see langword="true" /> when every day in <paramref name="range" /> lies inside a single tracked interval.</returns>
	public bool Covers(DateRange range)
	{
		if (!range.IsValid) return false;

		foreach (DateRange existing in _ranges)
		{
			if (existing.Contains(range)) return true;
		}

		return false;
	}

	/// <summary>
	/// Removes every tracked range, restoring the empty state.
	/// </summary>
	public void Clear() => _ranges.Clear();
}
