// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Financial.Kat;

/// <summary>
/// Represents a known-answer test row exercising a full <see cref="FixedDatedExchangeRateTable.TryGetRate" /> call,
/// including the full resulting metadata.
/// </summary>
/// <param name="Name">The short label identifying the scenario.</param>
/// <param name="FromIsoCode">The source-currency code passed to the table.</param>
/// <param name="ToIsoCode">The destination-currency code passed to the table.</param>
/// <param name="RequestedDate">The date the caller asks about.</param>
/// <param name="Options">The lookup options supplied to the table.</param>
/// <param name="Rates">The observations used to populate the table under test.</param>
/// <param name="ExpectedSuccess">
/// <see langword="true" /> when the lookup should succeed; otherwise <see langword="false" />.
/// </param>
/// <param name="ExpectedRate">The expected returned rate value when successful.</param>
/// <param name="ExpectedResolvedDate">The expected resolved date when successful.</param>
/// <param name="ExpectedProvider">The expected provider identifier when successful.</param>
/// <param name="ExpectedInverted">The expected <see cref="ExchangeRate.IsInverted" /> flag when successful.</param>
public sealed record ExchangeRateLookupKat(
    string Name,
    string FromIsoCode,
    string ToIsoCode,
    DateOnly RequestedDate,
    ExchangeRateLookupOptions Options,
    ExchangeRate[] Rates,
    bool ExpectedSuccess,
    decimal? ExpectedRate,
    DateOnly? ExpectedResolvedDate,
    string? ExpectedProvider,
    bool ExpectedInverted) : IKat;
