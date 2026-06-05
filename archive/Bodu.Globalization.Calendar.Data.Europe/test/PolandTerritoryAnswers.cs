// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PolandTerritoryAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data sources for the
/// Poland rule catalogue shipped in the Europe data pack. Covers dates inherited from <c>europe-common.xml</c>
/// (Easter Monday, Corpus Christi, Christmas Day), Poland-specific national holidays (Constitution Day, Independence
/// Day), and the absence of dates Poland deliberately did not cherry-pick (Good Friday) or renamed (Boxing Day).
/// </summary>
public static class PolandTerritoryAnswers
{
    private static Func<INotableDateRuleProvider> PlProvider =>
        EuropeCalendarData.CreatePolandProvider;

    /// <summary>
    /// Provides the cherry-pick expectations for the Poland flattened rule catalogue.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="RuleCatalogueExpectation" />.
    /// </returns>
    public static IEnumerable<object[]> RuleCatalogueExpectations()
    {
        yield return new object[]
        {
            new RuleCatalogueExpectation
            {
                ProviderFactory = PlProvider,
                Territory = "PL",
                Includes =
                [
                    "Easter Monday",
                    "Corpus Christi",
                    "Assumption of Mary",
                    "Christmas Day",
                    "Second Day of Christmas",
                    "Constitution Day",
                    "Independence Day",
                ],
                Excludes =
                [
                    "Good Friday",
                    "Boxing Day",
                ],
            },
        };
    }
}
