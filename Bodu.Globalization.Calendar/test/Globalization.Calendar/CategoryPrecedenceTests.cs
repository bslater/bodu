// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CategoryPrecedenceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the resolution policy's category precedence is data-driven: the built-in precedence ranks every
/// category, and a resource may author its own precedence to reorder how <see cref="CollisionPolicy.CategoryPriority" />
/// resolves a same-day collision.
/// </summary>
[TestClass]
public sealed class CategoryPrecedenceTests
{
    /// <summary>
    /// Builds a document whose 1 January public holiday and observance collide under the category-priority policy, with
    /// an optional authored category precedence.
    /// </summary>
    /// <param name="categoryPrecedenceXml">The authored <c>CategoryPrecedence</c> element, or the empty string.</param>
    /// <returns>The resource XML.</returns>
    private static string Document(string categoryPrecedenceXml) => $"""
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.precedence">
      <ResolutionPolicy sameDayCollisionPolicy="CategoryPriority" priorityDirection="HigherWins">{categoryPrecedenceXml}</ResolutionPolicy>
      <NotableDates>
        <NotableDate id="big-holiday" displayName="Big Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x" priority="200"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="minor-observance" displayName="Minor Observance" category="Observance" defaultNonWorkingDay="false">
          <Rules><Rule id="x" priority="50"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Resolves 1 January 2025 from the supplied resource and returns the surviving identifiers.
    /// </summary>
    /// <param name="resource">The loaded resource.</param>
    /// <returns>The surviving notable-date identifiers.</returns>
    private static List<string> ResolveNewYear(NotableDateResource resource) =>
        new NotableDateService(resource)
            .Resolve(new DateOnly(2025, 1, 1), "XX")
            .Select(r => r.NotableDateId)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Verifies that the built-in category precedence ranks every <see cref="NotableDateCategory" /> except
    /// <see cref="NotableDateCategory.None" />, so a category added in future cannot silently rank below every other.
    /// </summary>
    [TestMethod]
    public void DefaultPrecedence_RanksEveryCategoryExceptNone()
    {
        IReadOnlyList<NotableDateCategory> precedence = ResolutionPolicy.Default.CategoryPrecedence;

        foreach (NotableDateCategory category in Enum.GetValues<NotableDateCategory>())
        {
            if (category == NotableDateCategory.None)
                Assert.DoesNotContain(category, precedence, "None must remain unranked.");
            else
                Assert.Contains(category, precedence, $"{category} must be ranked by the default precedence.");
        }
    }

    /// <summary>
    /// Verifies that without an authored precedence the built-in order keeps the public holiday over the observance.
    /// </summary>
    [TestMethod]
    public void CategoryPriority_WhenDefaultPrecedence_KeepsPublicHoliday()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(Document(string.Empty));

        CollectionAssert.AreEqual(new[] { "big-holiday" }, ResolveNewYear(resource));
    }

    /// <summary>
    /// Verifies that an authored XML category precedence that ranks observances above public holidays inverts which
    /// occurrence survives the category-priority collision.
    /// </summary>
    [TestMethod]
    public void CategoryPriority_WhenAuthoredPrecedenceInverts_KeepsObservance()
    {
        const string precedence = "<CategoryPrecedence><Category value=\"Observance\" /><Category value=\"PublicHoliday\" /></CategoryPrecedence>";
        NotableDateResource resource = NotableDateResourceLoader.Load(Document(precedence));

        CollectionAssert.AreEqual(new[] { "minor-observance" }, ResolveNewYear(resource));
    }

    /// <summary>
    /// Verifies that the JSON projection honors an authored category precedence identically to the XML form.
    /// </summary>
    [TestMethod]
    public void CategoryPriority_WhenAuthoredPrecedenceInvertsInJson_KeepsObservance()
    {
        const string json = """
        {
          "schemaVersion": "1.0", "resourceId": "data.precedence",
          "resolutionPolicy": { "sameDayCollisionPolicy": "CategoryPriority", "categoryPrecedence": [ "Observance", "PublicHoliday" ] },
          "notableDates": [
            { "id": "big-holiday", "displayName": "Big Holiday", "category": "PublicHoliday", "defaultNonWorkingDay": true,
              "rules": [ { "id": "x", "priority": 200, "strategy": { "fixed": { "month": 1, "day": 1 } } } ] },
            { "id": "minor-observance", "displayName": "Minor Observance", "category": "Observance",
              "rules": [ { "id": "x", "priority": 50, "strategy": { "fixed": { "month": 1, "day": 1 } } } ] }
          ]
        }
        """;

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json);

        CollectionAssert.AreEqual(new[] { "minor-observance" }, ResolveNewYear(resource));
    }
}
