
namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves the canonical ordering, deduplication, or suppression of multiple <see cref="NotableDate" /> instances that fall on the
/// same day.
/// </summary>
/// <remarks>
/// <para>
/// When two or more rules resolve to the same date for a given territory (for example a fixed bank holiday and a substitute observance,
/// or Easter Monday landing on Anzac Day), the service consults its registered <see cref="INotableDateCollisionResolver" /> to decide
/// how the overlapping dates should be presented. The default implementation simply orders by
/// <see cref="NotableDate.Category" /> and <see cref="NotableDate.Name" />.
/// </para>
/// </remarks>
public interface INotableDateCollisionResolver
{
	/// <summary>
	/// Reorders, deduplicates, or filters a set of notable dates that share the same calendar day.
	/// </summary>
	/// <param name="date">The shared day. May be useful for date-aware tie-breaking.</param>
	/// <param name="overlapping">The notable dates resolved for this day. The supplied collection is never empty.</param>
	/// <returns>The ordered notable dates to expose to consumers.</returns>
	IReadOnlyList<NotableDate> Resolve(DateTime date, IReadOnlyList<NotableDate> overlapping);
}

/// <summary>
/// Provides the default <see cref="INotableDateCollisionResolver" /> implementation. Sorts overlapping dates by category, falls back to
/// alphabetic name, and removes exact duplicates.
/// </summary>
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
