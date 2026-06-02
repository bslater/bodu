// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GreeceTerritoryAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data sources for the
/// Greece rule catalogue shipped in the Europe data pack. Covers the combination of fixed Gregorian dates inherited
/// from <c>europe-common.xml</c> (Epiphany, the renamed Dormition of the Mother of God) with the movable Eastern
/// Orthodox Easter cycle cherry-picked from the main library, and the deliberate absence of the Western Easter dates.
/// </summary>
public static class GreeceTerritoryAnswers
{
    private static Func<INotableDateRuleProvider> GrProvider =>
        EuropeCalendarData.CreateGreeceProvider;

    /// <summary>
    /// Provides the cherry-pick expectations for the Greece flattened rule catalogue.
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
                ProviderFactory = GrProvider,
                Territory = "GR",
                Includes =
                [
                    "Epiphany",
                    "Dormition of the Mother of God",
                    "Orthodox Good Friday",
                    "Orthodox Easter Monday",
                    "Independence Day",
                    "Ochi Day",
                ],
                Excludes =
                [
                    "Good Friday",
                    "Easter Monday",
                    "Assumption of Mary",
                ],
            },
        };
    }
}
