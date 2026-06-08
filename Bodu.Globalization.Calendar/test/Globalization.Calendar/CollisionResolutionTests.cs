// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollisionResolutionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the same-day collision policy: two concepts emitted on the same date are kept, reduced to the
/// highest-priority occurrence, reduced by category then priority, or resolved by a custom resolver.
/// </summary>
[TestClass]
public sealed class CollisionResolutionTests
{
    /// <summary>
    /// Builds a resource document whose two 1 January concepts collide, parameterized by collision policy.
    /// </summary>
    /// <param name="policy">The same-day collision policy token.</param>
    /// <returns>The resource XML.</returns>
    private static string Document(string policy) => $"""
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.collide">
      <ResolutionPolicy duplicatePolicy="Error" sameDayCollisionPolicy="{policy}" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="big-holiday" displayName="Big Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x" priority="200"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="minor-observance" displayName="Minor Observance" category="Observance" defaultNonWorkingDay="false">
          <Rules><Rule id="x" priority="50"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="other-day" displayName="Other Day" category="Observance" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="March" day="3" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Resolves 1 January 2025 for the supplied policy, returning the surviving identifiers.
    /// </summary>
    /// <param name="policy">The collision policy token.</param>
    /// <param name="resolver">An optional custom collision resolver.</param>
    /// <returns>The surviving notable-date identifiers.</returns>
    private static List<string> ResolveNewYear(string policy, INotableDateCollisionResolver? resolver = null)
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(Document(policy)), new NotableDateServiceOptions { CollisionResolver = resolver });
        return service.Resolve(new DateOnly(2025, 1, 1), "XX")
            .Select(r => r.NotableDateId).OrderBy(s => s, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Verifies that the default keep-all policy returns both colliding occurrences.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenKeepAll_ReturnsBoth()
    {
        CollectionAssert.AreEqual(new[] { "big-holiday", "minor-observance" }, ResolveNewYear("KeepAll"));
    }

    /// <summary>
    /// Verifies that the highest-priority policy keeps only the higher-priority occurrence.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenHighestPriorityOnly_KeepsTheHigherPriority()
    {
        CollectionAssert.AreEqual(new[] { "big-holiday" }, ResolveNewYear("HighestPriorityOnly"));
    }

    /// <summary>
    /// Verifies that the category-priority policy keeps the higher-ranked category.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCategoryPriority_KeepsTheHigherCategory()
    {
        CollectionAssert.AreEqual(new[] { "big-holiday" }, ResolveNewYear("CategoryPriority"));
    }

    /// <summary>
    /// Verifies that the custom policy defers to the supplied resolver.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenCustom_DefersToResolver()
    {
        INotableDateCollisionResolver resolver = new KeepObservanceResolver();
        CollectionAssert.AreEqual(new[] { "minor-observance" }, ResolveNewYear("Custom", resolver));
    }

    /// <summary>
    /// Verifies that a non-colliding date is unaffected by a reducing policy.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleOccurrence_IsUnaffectedByPolicy()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(Document("HighestPriorityOnly")));
        IReadOnlyList<NotableDate> march = service.Resolve(new DateOnly(2025, 3, 3), "XX");

        CollectionAssert.AreEqual(new[] { "other-day" }, march.Select(r => r.NotableDateId).ToList());
    }

    /// <summary>
    /// A custom resolver that keeps only the observance occurrence.
    /// </summary>
    private sealed class KeepObservanceResolver
        : INotableDateCollisionResolver
    {
        /// <inheritdoc />
        public IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding) =>
            colliding.Where(n => n.Category == NotableDateCategory.Observance).ToList();
    }
}
