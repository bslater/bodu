// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultNotableDateCollisionResolverTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the deterministic ordering and de-duplication contract of
/// <see cref="DefaultNotableDateCollisionResolver" />.
/// </summary>
[TestClass]
public sealed class DefaultNotableDateCollisionResolverTests
{
    private readonly DefaultNotableDateCollisionResolver _resolver = new();
    private static readonly DateTime Anchor = new(2026, 1, 1);

    /// <summary>
    /// Verifies that a <see langword="null" /> context returns an empty result without throwing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenContextIsNull_ShouldReturnEmpty()
    {
        IReadOnlyList<NotableDate> result = _resolver.Resolve(null!);

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that an empty overlapping list returns an empty result without throwing.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenOverlappingIsEmpty_ShouldReturnEmpty()
    {
        IReadOnlyList<NotableDate> result = _resolver.Resolve(Context());

        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that entries are ordered by <see cref="NotableDateCategory" /> numeric value when priority and
    /// provenance tie, regardless of input order.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMixedCategories_ShouldOrderByCategoryEnumOrdinal()
    {
        NotableDate cultural = Create("C", NotableDateCategory.Cultural);
        NotableDate holiday = Create("H", NotableDateCategory.Holiday);
        NotableDate other = Create("O", NotableDateCategory.Other);
        NotableDate none = Create("N", NotableDateCategory.None);

        IReadOnlyList<NotableDate> result = _resolver.Resolve(Context(cultural, other, holiday, none));

        CollectionAssert.AreEqual(new[] { none, holiday, cultural, other }, result.ToArray());
    }

    /// <summary>
    /// Verifies that entries sharing a category are then ordered by <see cref="NotableDate.Name" /> using
    /// ordinal-case-insensitive comparison.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSameCategory_ShouldOrderByNameOrdinalIgnoreCase()
    {
        NotableDate bravo = Create("bravo", NotableDateCategory.Holiday);
        NotableDate alpha = Create("ALPHA", NotableDateCategory.Holiday);
        NotableDate charlie = Create("Charlie", NotableDateCategory.Holiday);

        IReadOnlyList<NotableDate> result = _resolver.Resolve(Context(bravo, alpha, charlie));

        CollectionAssert.AreEqual(new[] { alpha, bravo, charlie }, result.ToArray());
    }

    /// <summary>
    /// Verifies that priority outranks category: a lower priority value sorts first even when its category ordinal is
    /// higher.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenPrioritiesDiffer_ShouldOrderByAscendingPriorityBeforeCategory()
    {
        NotableDate strong = Create("Strong", NotableDateCategory.Other, priority: 10);
        NotableDate weak = Create("Weak", NotableDateCategory.Holiday, priority: 50);

        IReadOnlyList<NotableDate> result = _resolver.Resolve(Context(weak, strong));

        Assert.AreSame(strong, result[0], "The lower priority value (10) outranks the lower category ordinal.");
        Assert.AreSame(weak, result[1]);
    }

    /// <summary>
    /// Verifies that provenance is the strongest tie-breaker: a runtime-override occurrence outranks a local one with
    /// the same priority and category.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenProvenanceDiffers_ShouldOrderRuntimeOverrideFirst()
    {
        NotableDate local = Create("Aaa", NotableDateCategory.Holiday);
        NotableDate overridden = Create("Zzz", NotableDateCategory.Holiday);

        NotableDateCollisionContext context = new(
            Anchor,
            [local, overridden],
            [NotableDateProvenance.Local, NotableDateProvenance.RuntimeOverride]);

        IReadOnlyList<NotableDate> result = _resolver.Resolve(context);

        Assert.AreSame(overridden, result[0], "Runtime-override provenance outranks local even against alphabetical order.");
        Assert.AreSame(local, result[1]);
    }

    /// <summary>
    /// Verifies that exact duplicate <see cref="NotableDate" /> records (record equality) are collapsed into a single
    /// entry before ordering.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenDuplicateRecordsSupplied_ShouldCollapseToSingleEntry()
    {
        NotableDate first = Create("Same", NotableDateCategory.Cultural);
        NotableDate duplicate = Create("Same", NotableDateCategory.Cultural);

        IReadOnlyList<NotableDate> result = _resolver.Resolve(Context(first, duplicate));

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

        IReadOnlyList<NotableDate> result = _resolver.Resolve(Context(cultural, holiday));

        Assert.AreEqual(2, result.Count);
        Assert.AreSame(holiday, result[0]);
        Assert.AreSame(cultural, result[1]);
    }

    /// <summary>
    /// Builds a collision context over the supplied occurrences, all tagged with <see cref="NotableDateProvenance.Local" />.
    /// </summary>
    /// <param name="overlapping">The overlapping occurrences.</param>
    /// <returns>The collision context.</returns>
    private static NotableDateCollisionContext Context(params NotableDate[] overlapping) =>
        new(Anchor, overlapping, [.. overlapping.Select(_ => NotableDateProvenance.Local)]);

    /// <summary>
    /// Creates a single-day <see cref="NotableDate" /> with the supplied name, category, and priority.
    /// </summary>
    /// <param name="name">The notable-date name.</param>
    /// <param name="category">The category.</param>
    /// <param name="priority">The priority; defaults to 100.</param>
    /// <returns>The constructed notable date.</returns>
    private static NotableDate Create(string name, NotableDateCategory category, int priority = 100) =>
        new()
        {
            Date = Anchor,
            Name = name,
            Category = category,
            Priority = priority,
        };
}
