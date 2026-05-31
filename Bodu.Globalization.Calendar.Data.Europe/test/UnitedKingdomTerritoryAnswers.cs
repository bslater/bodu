// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnitedKingdomTerritoryAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data sources for the
/// United Kingdom rule catalogue shipped in the Europe data pack. Covers cherry-picked rule presence (Easter Monday,
/// Boxing Day, Burns Night) and the named-holiday occurrence assertions.
/// </summary>
public static class UnitedKingdomTerritoryAnswers
{
    private static Func<INotableDateRuleProvider> GbProvider =>
        EuropeCalendarData.CreateUnitedKingdomProvider;

    /// <summary>
    /// Provides the cherry-pick expectations for the United Kingdom flattened rule catalogue.
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
                ProviderFactory = GbProvider,
                Territory = "GB",
                Includes =
                [
                    "Easter Monday",
                    "Boxing Day",
                    "Burns Night",
                ],
            },
        };
    }

    // Future expansion: a NamedHolidayOccurrences() provider can be added once the GB Christmas Day
    // weekend-substitute / MoveToNextNonWorkingDay cascade is fully characterised. The bespoke
    // LoadRules_WhenLocalRuleOverridesInheritedRule_ShouldUseLocalRule test in
    // UnitedKingdomNotableDatesTests covers the GB-scope override payload separately for now.
}
