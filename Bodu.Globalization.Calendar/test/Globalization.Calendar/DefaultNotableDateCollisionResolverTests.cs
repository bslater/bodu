// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultNotableDateCollisionResolverTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the deterministic ordering and de-duplication contract of
/// <see cref="DefaultNotableDateCollisionResolver" />.
/// </summary>
[TestClass]
public sealed class DefaultNotableDateCollisionResolverTests
{
	private readonly DefaultNotableDateCollisionResolver _resolver = new();
	private static readonly DateTime Anchor = new DateTime(2026, 1, 1);

	/// <summary>
	/// Verifies that a <see langword="null" /> overlapping list returns an empty result without
	/// throwing.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOverlappingIsNull_ShouldReturnEmpty()
	{
		IReadOnlyList<NotableDate> result = _resolver.Resolve(Anchor, null!);

		Assert.AreEqual(0, result.Count);
	}

	/// <summary>
	/// Verifies that an empty overlapping list returns an empty result without throwing.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOverlappingIsEmpty_ShouldReturnEmpty()
	{
		IReadOnlyList<NotableDate> result = _resolver.Resolve(Anchor, Array.Empty<NotableDate>());

		Assert.AreEqual(0, result.Count);
	}

	/// <summary>
	/// Verifies that entries are ordered by <see cref="NotableDateCategory" /> numeric value
	/// regardless of input order.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenMixedCategories_ShouldOrderByCategoryEnumOrdinal()
	{
		NotableDate cultural = Create("C", NotableDateCategory.Cultural);
		NotableDate holiday = Create("H", NotableDateCategory.Holiday);
		NotableDate other = Create("O", NotableDateCategory.Other);
		NotableDate none = Create("N", NotableDateCategory.None);

		IReadOnlyList<NotableDate> result = _resolver.Resolve(Anchor, new[] { cultural, other, holiday, none });

		CollectionAssert.AreEqual(
			new[] { none, holiday, cultural, other },
			result.ToArray());
	}

	/// <summary>
	/// Verifies that entries sharing a category are then ordered by
	/// <see cref="NotableDate.Name" /> using ordinal-case-insensitive comparison.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenSameCategory_ShouldOrderByNameOrdinalIgnoreCase()
	{
		NotableDate bravo = Create("bravo", NotableDateCategory.Holiday);
		NotableDate alpha = Create("ALPHA", NotableDateCategory.Holiday);
		NotableDate charlie = Create("Charlie", NotableDateCategory.Holiday);

		IReadOnlyList<NotableDate> result = _resolver.Resolve(Anchor, new[] { bravo, alpha, charlie });

		CollectionAssert.AreEqual(
			new[] { alpha, bravo, charlie },
			result.ToArray());
	}

	/// <summary>
	/// Verifies that exact duplicate <see cref="NotableDate" /> records (record equality) are
	/// collapsed into a single entry before ordering.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenDuplicateRecordsSupplied_ShouldCollapseToSingleEntry()
	{
		NotableDate first = Create("Same", NotableDateCategory.Cultural);
		NotableDate duplicate = Create("Same", NotableDateCategory.Cultural);

		IReadOnlyList<NotableDate> result = _resolver.Resolve(Anchor, new[] { first, duplicate });

		Assert.AreEqual(1, result.Count);
	}

	/// <summary>
	/// Verifies that two entries differing only in category are treated as distinct records.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenRecordsDifferByCategoryAlone_ShouldKeepBoth()
	{
		NotableDate holiday = Create("Same", NotableDateCategory.Holiday);
		NotableDate cultural = Create("Same", NotableDateCategory.Cultural);

		IReadOnlyList<NotableDate> result = _resolver.Resolve(Anchor, new[] { cultural, holiday });

		Assert.AreEqual(2, result.Count);
		Assert.AreSame(holiday, result[0]);
		Assert.AreSame(cultural, result[1]);
	}

	private static NotableDate Create(string name, NotableDateCategory category) =>
		new NotableDate
		{
			Date = Anchor,
			Name = name,
			Category = category,
		};
}
