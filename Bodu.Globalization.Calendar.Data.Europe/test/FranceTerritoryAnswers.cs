// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FranceTerritoryAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data sources for the
/// France rule catalogue shipped in the Europe data pack. Covers cherry-picked rule presence (Fête du Travail, Bastille
/// Day) and the absence of un-cherry-picked observances such as International Workers' Day.
/// </summary>
public static class FranceTerritoryAnswers
{
    private static Func<INotableDateRuleProvider> FrProvider =>
        EuropeCalendarData.CreateFranceProvider;

    /// <summary>
    /// Provides the cherry-pick expectations for the France flattened rule catalogue.
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
                ProviderFactory = FrProvider,
                Territory = "FR",
                Includes =
                [
                    "Fête du Travail",
                    "Bastille Day",
                ],
                Excludes =
                [
                    "International Workers' Day",
                ],
            },
        };
    }
}
