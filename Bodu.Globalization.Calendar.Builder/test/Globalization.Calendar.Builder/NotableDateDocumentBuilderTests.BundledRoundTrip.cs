// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.BundledRoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// The bundled common notable-date catalogues shipped by <c>Bodu.Globalization.Calendar</c>. Round-tripping every
    /// one through the builder proves it can express every notable-date definition and resource feature in production.
    /// </summary>
    /// <returns>One single-element object array per catalogue name, for <c>[DynamicData]</c>.</returns>
    public static IEnumerable<object[]> BundledCatalogues()
    {
        string[] names =
        [
            "default-minimal", "global-core", "global-all", "global-anchors",
            "christian-western", "christian-orthodox",
            "global-islamic", "global-islamic-umm-al-qura", "global-hindu", "global-jewish",
            "global-persian", "global-buddhist", "global-lunar",
            "global-cultural", "global-family", "global-family-social", "global-social",
            "global-remembrance", "global-un", "global-health", "global-science",
            "global-education", "global-environment", "global-food", "global-animals",
            "global-multiday-normalization",
        ];
        return names.Select(n => new object[] { n });
    }

    /// <summary>
    /// Resolves every occurrence for a set of representative territories across 2024–2026 and returns them as a
    /// comparable, ordered list — the observable fingerprint of a resource.
    /// </summary>
    /// <param name="resource">The resource to resolve over.</param>
    /// <returns>An ordered list of <c>(territory, id, date, observed)</c> tuples.</returns>
    private static List<(string Territory, string Id, DateOnly Date, bool Observed)> Fingerprint(NotableDateResource resource)
    {
        string[] territories = ["US", "CA-ON", "AU-NSW", "GB-ENG", "FR", "DE", "IN", "IL", "SA", "JP", "CN", "NZ"];
        NotableDateService service = new(resource);
        var rows = new List<(string, string, DateOnly, bool)>();
        foreach (string territory in territories)
        {
            for (int year = 2024; year <= 2026; year++)
            {
                foreach (NotableDate n in service.Resolve(year, territory))
                    rows.Add((territory, n.NotableDateId, n.Date, n.IsObserved));
            }
        }

        return rows.OrderBy(r => r.Item1).ThenBy(r => r.Item3).ThenBy(r => r.Item2).ToList();
    }

    /// <summary>
    /// Verifies that every bundled common catalogue round-trips through the builder — parsing it with
    /// <see cref="NotableDateDocumentBuilder.FromXml(string)" /> and rebuilding it reproduces a resource with the same
    /// identity, definition count, rule count, adjustment-policy count, and resolved occurrences as a direct load.
    /// </summary>
    /// <param name="catalogue">The bundled catalogue name under test.</param>
    [TestMethod]
    [DynamicData(nameof(BundledCatalogues))]
    public void FromXml_WhenBundledCatalogue_ShouldRoundTripToEquivalentResource(string catalogue)
    {
        string xml = CommonNotableDateResources.Resolve(catalogue)
            ?? throw new InvalidOperationException($"Bundled catalogue '{catalogue}' was not found.");

        NotableDateResource direct = NotableDateResourceLoader.Load(xml, CommonNotableDateResources.Resolver);
        NotableDateResource viaBuilder = NotableDateDocumentBuilder.FromXml(xml).Build(CommonNotableDateResources.Resolver);

        Assert.AreEqual(direct.ResourceId, viaBuilder.ResourceId, $"{catalogue}: resourceId");
        Assert.HasCount(direct.NotableDates.Count, viaBuilder.NotableDates, $"{catalogue}: definition count");
        Assert.AreEqual(direct.RuleCount, viaBuilder.RuleCount, $"{catalogue}: rule count");
        Assert.HasCount(direct.AdjustmentPolicies.Count, viaBuilder.AdjustmentPolicies, $"{catalogue}: adjustment-policy count");

        CollectionAssert.AreEqual(Fingerprint(direct), Fingerprint(viaBuilder), $"{catalogue}: resolved occurrences differ");
    }
}
