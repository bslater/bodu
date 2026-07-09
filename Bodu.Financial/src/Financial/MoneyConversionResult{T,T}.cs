// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyConversionResult{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates;

namespace Bodu.Financial;

/// <summary>
/// Bundles the result of a single end-to-end strongly-typed money conversion together with the exchange-rate metadata
/// that produced it, so consumers can audit which observed rate was used.
/// </summary>
/// <typeparam name="TSource">The source currency.</typeparam>
/// <typeparam name="TTarget">The destination currency.</typeparam>
/// <param name="SourceAmount">The original source amount.</param>
/// <param name="TargetAmount">The converted, rounded target amount.</param>
/// <param name="ExchangeRate">
/// The full lookup result, including provider, dates, and inversion flag, used to compute
/// <paramref name="TargetAmount" />.
/// </param>
public readonly record struct MoneyConversionResult<TSource, TTarget>(
    Money<TSource> SourceAmount,
    Money<TTarget> TargetAmount,
    RateLookupResult ExchangeRate)
    where TSource : ICurrency
    where TTarget : ICurrency;
