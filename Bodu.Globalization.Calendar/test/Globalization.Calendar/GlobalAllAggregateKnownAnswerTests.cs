// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalAllAggregateKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that the aggregate <c>global-all</c> catalogue imports and resolves a representative concept from every
/// new and updated catalogue, confirming the import wiring added in the latest pass resolves end to end.
/// </summary>
[TestClass]
public sealed class GlobalAllAggregateKnownAnswerTests
{
    /// <summary>
    /// Verifies that one representative concept from each contributing catalogue resolves through <c>global-all</c> for a
    /// representative Gregorian year.
    /// </summary>
    /// <param name="notableDateId">The notable-date id expected to resolve through the aggregate.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("reformation-day")]                    // christian-protestant
    [DataRow("transfiguration-sunday")]             // christian-protestant (imported Western Easter offset)
    [DataRow("andrew-the-apostle")]                 // christian-anglican
    [DataRow("oriental-orthodox-easter-sunday")]    // christian-oriental-orthodox
    [DataRow("vaisakhi")]                           // global-sikh
    [DataRow("mahavir-jayanti")]                    // global-jain
    [DataRow("naw-ruz")]                            // global-bahai
    [DataRow("zoroastrian-nowruz")]                 // global-zoroastrian
    [DataRow("rosh-hashanah")]                      // global-jewish
    [DataRow("eid-al-fitr")]                        // global-islamic
    [DataRow("diwali")]                             // global-hindu
    [DataRow("vesak")]                              // global-buddhist
    public void Resolve_GlobalAll_IncludesConceptFromEveryContributingCatalogue(string notableDateId)
    {
        NotableDateService service = CommonCatalogues.Service("global-all");

        List<NotableDate> matches = CommonCatalogues.ResolveForYear(service, notableDateId, 2025);

        Assert.IsGreaterThanOrEqualTo(1, matches.Count, $"'{notableDateId}' did not resolve through global-all");
    }
}
