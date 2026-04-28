// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultNotableDateCollisionResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the default <see cref="INotableDateCollisionResolver" /> implementation. Sorts overlapping dates by category, falls back to
/// alphabetic name, and removes exact duplicates.
/// </summary>
/// <remarks>
/// <para>
/// This resolver removes structurally equal <see cref="NotableDate" /> entries first, then orders the remaining dates ascending by
/// <see cref="NotableDateCategory" /> integer value (lower values indicate higher significance) and then alphabetically by
/// <see cref="NotableDate.Name" />. It is used automatically when no custom <see cref="INotableDateCollisionResolver" /> is supplied
/// to <see cref="NotableDateService" />.
/// </para>
/// </remarks>
public sealed class DefaultNotableDateCollisionResolver : INotableDateCollisionResolver
{
	/// <inheritdoc />
	public IReadOnlyList<NotableDate> Resolve(DateTime date, IReadOnlyList<NotableDate> overlapping)
	{
		if (overlapping is null || overlapping.Count == 0)
			return Array.Empty<NotableDate>();

		return overlapping
			.Distinct()
			.OrderBy(d => (int)d.Category)
			.ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}
}
