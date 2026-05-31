// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeExchangeRateProviderKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Financial.Kat;

/// <summary>
/// Represents a known-answer test row exercising <see cref="CompositeDatedExchangeRateProvider" /> against a set of
/// independent inner <see cref="FixedDatedExchangeRateTable" />s.
/// </summary>
/// <param name="Name">The short label identifying the scenario.</param>
/// <param name="FromIsoCode">The source-currency code passed to the composite provider.</param>
/// <param name="ToIsoCode">The destination-currency code passed to the composite provider.</param>
/// <param name="RequestedDate">The date the caller asks about.</param>
/// <param name="Options">The lookup options supplied to the composite provider.</param>
/// <param name="ProviderRates">A jagged array, one entry per inner provider, supplying that provider's observations.</param>
/// <param name="ExpectedSuccess"><see langword="true" /> when the lookup should succeed; otherwise <see langword="false" />.</param>
/// <param name="ExpectedRate">The expected returned rate value when successful.</param>
/// <param name="ExpectedProvider">The expected provider identifier carried by the result rate.</param>
/// <param name="ExpectedResolvedDate">The expected resolved date when successful.</param>
/// <param name="ExpectedInverted">The expected <see cref="ExchangeRate.IsInverted" /> flag when successful.</param>
public sealed record CompositeExchangeRateProviderKat(
    string Name,
    string FromIsoCode,
    string ToIsoCode,
    DateOnly RequestedDate,
    ExchangeRateLookupOptions Options,
    ExchangeRate[][] ProviderRates,
    bool ExpectedSuccess,
    decimal? ExpectedRate,
    string? ExpectedProvider,
    DateOnly? ExpectedResolvedDate,
    bool ExpectedInverted) : IKat;
