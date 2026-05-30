// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyConversionKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Numerics.Kat;

/// <summary>
/// Represents a known-answer test row exercising an end-to-end money conversion through
/// <see cref="MoneyExchangeRateExtensions" />.
/// </summary>
/// <param name="Name">The short label identifying the scenario.</param>
/// <param name="SourceAmount">The source-currency amount to convert.</param>
/// <param name="Rate">The exchange rate to apply.</param>
/// <param name="TargetDecimalPlaces">The destination currency's natural decimal-place count.</param>
/// <param name="Rounding">The rounding mode used to round the converted amount.</param>
/// <param name="ExpectedAmount">The expected destination-currency amount.</param>
public sealed record MoneyConversionKat(
    string Name,
    decimal SourceAmount,
    decimal Rate,
    int TargetDecimalPlaces,
    MidpointRounding Rounding,
    decimal ExpectedAmount) : IKat;
