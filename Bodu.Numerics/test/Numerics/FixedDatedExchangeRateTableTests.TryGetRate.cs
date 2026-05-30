// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedExchangeRateTableTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics.Kat;
using Bodu.Test.Kat;

namespace Bodu.Numerics;

public partial class FixedDatedExchangeRateTableTests
{
    /// <summary>
    /// Verifies that <see cref="FixedDatedExchangeRateTable.TryGetRate" /> returns the rate, provider, resolved date,
    /// inversion flag, and offset days expected for the supplied lookup scenario.
    /// </summary>
    [TestMethod]
    [DynamicData(
        nameof(ProviderLookupCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryGetRate_WhenLookupRequested_ShouldReturnExpectedRateMetadata(ExchangeRateLookupKat kat)
    {
        FixedDatedExchangeRateTable table = new(kat.Rates);

        bool actual = table.TryGetRate(
            kat.FromIsoCode,
            kat.ToIsoCode,
            kat.RequestedDate,
            kat.Options,
            out ExchangeRateLookupResult result);

        Assert.AreEqual(kat.ExpectedSuccess, actual);

        if (!kat.ExpectedSuccess)
            return;

        Assert.AreEqual(kat.ExpectedRate, result.Rate.Rate);
        Assert.AreEqual(kat.ExpectedResolvedDate, result.Rate.Date);
        Assert.AreEqual(kat.ExpectedProvider, result.Rate.Provider);
        Assert.AreEqual(kat.ExpectedInverted, result.Rate.IsInverted);
        Assert.AreEqual(kat.FromIsoCode, result.Rate.FromIsoCode);
        Assert.AreEqual(kat.ToIsoCode, result.Rate.ToIsoCode);
        Assert.AreEqual(kat.RequestedDate, result.RequestedDate);
        Assert.AreEqual(
            Math.Abs(result.Rate.Date.DayNumber - kat.RequestedDate.DayNumber),
            result.OffsetDays);
    }

    public static IEnumerable<object[]> ProviderLookupCases()
    {
        ExchangeRate[] direct = [new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 3), 1.50m, "RBA")];
        ExchangeRate[] inverseOnly = [new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 3), 1.50m, "RBA")];
        ExchangeRate[] bothDirections =
        [
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 3), 1.50m, "RBA"),
            new ExchangeRate("AUD", "USD", new DateOnly(2024, 1, 3), 0.67m, "RBA"),
        ];
        ExchangeRate[] series =
        [
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 2), 1.49m, "RBA"),
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 3), 1.50m, "RBA"),
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 8), 1.55m, "RBA"),
        ];

        yield return [new ExchangeRateLookupKat(
            "Direct exact",
            "USD", "AUD",
            new DateOnly(2024, 1, 3),
            ExchangeRateLookupOptions.Exact,
            direct,
            true,
            1.50m,
            new DateOnly(2024, 1, 3),
            "RBA",
            false)];

        yield return [new ExchangeRateLookupKat(
            "Direct exact miss",
            "USD", "AUD",
            new DateOnly(2024, 1, 4),
            ExchangeRateLookupOptions.Exact,
            direct,
            false,
            null, null, null, false)];

        yield return [new ExchangeRateLookupKat(
            "Direct previous within tolerance",
            "USD", "AUD",
            new DateOnly(2024, 1, 4),
            ExchangeRateLookupOptions.PreviousWithin(2),
            series,
            true,
            1.50m,
            new DateOnly(2024, 1, 3),
            "RBA",
            false)];

        yield return [new ExchangeRateLookupKat(
            "Direct next within tolerance",
            "USD", "AUD",
            new DateOnly(2024, 1, 7),
            ExchangeRateLookupOptions.NextWithin(2),
            series,
            true,
            1.55m,
            new DateOnly(2024, 1, 8),
            "RBA",
            false)];

        yield return [new ExchangeRateLookupKat(
            "Direct nearest within tolerance",
            "USD", "AUD",
            new DateOnly(2024, 1, 4),
            ExchangeRateLookupOptions.NearestWithin(3),
            series,
            true,
            1.50m,
            new DateOnly(2024, 1, 3),
            "RBA",
            false)];

        yield return [new ExchangeRateLookupKat(
            "Inverse exact when only direct exists",
            "AUD", "USD",
            new DateOnly(2024, 1, 3),
            ExchangeRateLookupOptions.Exact,
            inverseOnly,
            true,
            1m / 1.50m,
            new DateOnly(2024, 1, 3),
            "RBA",
            true)];

        yield return [new ExchangeRateLookupKat(
            "Inverse disabled blocks reverse pair",
            "AUD", "USD",
            new DateOnly(2024, 1, 3),
            new ExchangeRateLookupOptions(ExchangeRateDateResolution.Exact, AllowInverse: false),
            inverseOnly,
            false,
            null, null, null, false)];

        yield return [new ExchangeRateLookupKat(
            "Direct wins over inverse when both exist",
            "AUD", "USD",
            new DateOnly(2024, 1, 3),
            ExchangeRateLookupOptions.Exact,
            bothDirections,
            true,
            0.67m,
            new DateOnly(2024, 1, 3),
            "RBA",
            false)];

        yield return [new ExchangeRateLookupKat(
            "Missing pair returns false",
            "JPY", "EUR",
            new DateOnly(2024, 1, 3),
            ExchangeRateLookupOptions.Exact,
            direct,
            false,
            null, null, null, false)];
    }
}
