// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDatedRateProviderTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Kat;
using Bodu.Test.Kat;

namespace Bodu.Financial.ExchangeRates;

public partial class FixedDatedRateProviderTests
{
    /// <summary>
    /// Verifies that <see cref="FixedDatedRateProvider.TryGetRate" /> returns the rate, provider, resolved date,
    /// inversion flag, and offset days expected for a lookup scenario that is expected to succeed.
    /// </summary>
    [TestMethod]
    [DynamicData(
        nameof(ProviderLookupSuccessCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryGetRate_WhenLookupSucceeds_ShouldReturnExpectedRateMetadata(RateLookupKat kat)
    {
        FixedDatedRateProvider table = new(kat.Rates);

        bool actual = table.TryGetRate(
            kat.FromIsoCode,
            kat.ToIsoCode,
            kat.RequestedDate,
            kat.Options,
            out RateLookupResult result);

        Assert.IsTrue(actual);

        Assert.AreEqual(kat.ExpectedRate, result.Rate.Rate);
        Assert.AreEqual(kat.ExpectedResolvedDate, result.Rate.Date);
        Assert.AreEqual(kat.ExpectedProvider, result.Rate.Provider);
        Assert.AreEqual(kat.ExpectedInverted, result.Rate.IsInverted);
        Assert.AreEqual(kat.FromIsoCode, result.Rate.From.ToString());
        Assert.AreEqual(kat.ToIsoCode, result.Rate.To.ToString());
        Assert.AreEqual(kat.RequestedDate, result.RequestedDate);
        Assert.AreEqual(
            Math.Abs(result.Rate.Date.DayNumber - kat.RequestedDate.DayNumber),
            result.OffsetDays);
    }

    /// <summary>
    /// Verifies that <see cref="FixedDatedRateProvider.TryGetRate" /> returns <see langword="false" /> for a lookup
    /// scenario that is expected to fail.
    /// </summary>
    [TestMethod]
    [DynamicData(
        nameof(ProviderLookupFailureCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryGetRate_WhenLookupFails_ShouldReturnFalse(RateLookupKat kat)
    {
        FixedDatedRateProvider table = new(kat.Rates);

        bool actual = table.TryGetRate(
            kat.FromIsoCode,
            kat.ToIsoCode,
            kat.RequestedDate,
            kat.Options,
            out _);

        Assert.IsFalse(actual);
    }

    public static IEnumerable<object[]> ProviderLookupSuccessCases() =>
        ProviderLookupCases().Where(row => ((RateLookupKat)row[0]).ExpectedSuccess);

    public static IEnumerable<object[]> ProviderLookupFailureCases() =>
        ProviderLookupCases().Where(row => !((RateLookupKat)row[0]).ExpectedSuccess);

    public static IEnumerable<object[]> ProviderLookupCases()
    {
        ExchangeRate[] direct = [new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA")];
        ExchangeRate[] inverseOnly = [new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA")];
        ExchangeRate[] bothDirections =
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.AUD, CurrencyCode.USD, new DateOnly(2024, 1, 3), 0.67m, "RBA"),
        ];
        ExchangeRate[] series =
        [
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 2), 1.49m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 3), 1.50m, "RBA"),
            new ExchangeRate(CurrencyCode.USD, CurrencyCode.AUD, new DateOnly(2024, 1, 8), 1.55m, "RBA"),
        ];

        yield return [new RateLookupKat(
            "Direct exact",
            "USD", "AUD",
            new DateOnly(2024, 1, 3),
            RateLookupOptions.Exact,
            direct,
            true,
            1.50m,
            new DateOnly(2024, 1, 3),
            "RBA",
            false)];

        yield return [new RateLookupKat(
            "Direct exact miss",
            "USD", "AUD",
            new DateOnly(2024, 1, 4),
            RateLookupOptions.Exact,
            direct,
            false,
            null, null, null, false)];

        yield return [new RateLookupKat(
            "Direct previous within tolerance",
            "USD", "AUD",
            new DateOnly(2024, 1, 4),
            RateLookupOptions.PreviousWithin(2),
            series,
            true,
            1.50m,
            new DateOnly(2024, 1, 3),
            "RBA",
            false)];

        yield return [new RateLookupKat(
            "Direct next within tolerance",
            "USD", "AUD",
            new DateOnly(2024, 1, 7),
            RateLookupOptions.NextWithin(2),
            series,
            true,
            1.55m,
            new DateOnly(2024, 1, 8),
            "RBA",
            false)];

        yield return [new RateLookupKat(
            "Direct nearest within tolerance",
            "USD", "AUD",
            new DateOnly(2024, 1, 4),
            RateLookupOptions.NearestWithin(3),
            series,
            true,
            1.50m,
            new DateOnly(2024, 1, 3),
            "RBA",
            false)];

        yield return [new RateLookupKat(
            "Inverse exact when only direct exists",
            "AUD", "USD",
            new DateOnly(2024, 1, 3),
            RateLookupOptions.Exact,
            inverseOnly,
            true,
            1m / 1.50m,
            new DateOnly(2024, 1, 3),
            "RBA",
            true)];

        yield return [new RateLookupKat(
            "Inverse disabled blocks reverse pair",
            "AUD", "USD",
            new DateOnly(2024, 1, 3),
            new RateLookupOptions(RateDateResolution.Exact, allowInverse: false),
            inverseOnly,
            false,
            null, null, null, false)];

        yield return [new RateLookupKat(
            "Direct wins over inverse when both exist",
            "AUD", "USD",
            new DateOnly(2024, 1, 3),
            RateLookupOptions.Exact,
            bothDirections,
            true,
            0.67m,
            new DateOnly(2024, 1, 3),
            "RBA",
            false)];

        yield return [new RateLookupKat(
            "Missing pair returns false",
            "JPY", "EUR",
            new DateOnly(2024, 1, 3),
            RateLookupOptions.Exact,
            direct,
            false,
            null, null, null, false)];
    }
}
