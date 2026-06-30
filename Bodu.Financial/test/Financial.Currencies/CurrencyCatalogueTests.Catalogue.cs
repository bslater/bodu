// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogueTests.Catalogue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

public partial class CurrencyCatalogueTests
{

    /// <summary>
    /// Verifies that the catalogue contains the full set of shipped tag types so the source generator is in
    /// sync with <c>currencies.json</c>.
    /// </summary>
    [TestMethod]
    public void Catalogue_WhenEnumeratedViaReflection_ShouldContainAtLeastOneHundredEightyTypes()
    {
        int count = typeof(USD).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "Bodu.Financial.Currencies"
                && typeof(ICurrency).IsAssignableFrom(t))
            .Count();

        Assert.IsGreaterThanOrEqualTo(180, count, $"Expected at least 180 currency tag types, found {count}.");
    }
}
