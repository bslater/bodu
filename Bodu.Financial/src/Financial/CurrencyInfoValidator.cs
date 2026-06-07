// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Validates <see cref="CurrencyInfo" /> metadata supplied to <see cref="CurrencyRegistry" /> with the same rule set
/// that <see cref="CurrencyMetadata{TCurrency}" /> applies to currency tag types, surfacing argument-style exceptions
/// because the metadata arrives as a method argument rather than as type metadata.
/// </summary>
internal static class CurrencyInfoValidator
{
    /// <summary>
    /// The maximum number of fractional digits a currency's minor unit may declare.
    /// </summary>
    private const int MaxMinorUnits = 28;

    /// <summary>
    /// The exclusive upper bound for an ISO 4217 three-digit numeric code.
    /// </summary>
    private const int NumericCodeUpperBound = 1000;

    /// <summary>
    /// Validates the structural and semantic integrity of <paramref name="info" />.
    /// </summary>
    /// <param name="info">The currency metadata to validate.</param>
    /// <param name="paramName">The parameter name reported in argument exceptions.</param>
    /// <exception cref="ArgumentNullException"><paramref name="info" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// The ISO code is not three uppercase ASCII letters; the successor ISO code, when present, is malformed; the
    /// cash-rounding increment is finer than the declared minor units; or the historic / successor fields are
    /// inconsistent.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The minor-unit precision is outside 0 to 28, the cash-rounding increment is negative, or the numeric code is
    /// outside 0 to 999.
    /// </exception>
    public static void Validate(CurrencyInfo info, string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(info, paramName);
        FinancialThrowHelper.ThrowIfNotValidIsoCode(info.IsoCode, paramName);

        if ((uint)info.MinorUnits > MaxMinorUnits)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                info.MinorUnits,
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CurrencyInfoMinorUnits, info.IsoCode, info.MinorUnits));
        }

        if (info.CashRoundingIncrement < 0m)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                info.CashRoundingIncrement,
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CurrencyInfoCashRoundingNegative, info.IsoCode, info.CashRoundingIncrement));
        }

        if (info.CashRoundingIncrement != 0m)
        {
            var scaled = info.CashRoundingIncrement * MinorUnitFactor(info.MinorUnits);
            if (scaled != decimal.Truncate(scaled))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_Invalid_CurrencyInfoCashRoundingTooFine, info.IsoCode, info.CashRoundingIncrement, info.MinorUnits),
                    paramName);
            }
        }

        if ((uint)info.NumericCode >= NumericCodeUpperBound)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                info.NumericCode,
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CurrencyInfoNumericCode, info.IsoCode, info.NumericCode));
        }

        if (info.SuccessorIsoCode is not null)
            FinancialThrowHelper.ThrowIfNotValidIsoCode(info.SuccessorIsoCode, paramName);

        // A non-historic currency must not carry demonetization metadata; historicity and its supporting fields must
        // agree so downstream consumers can trust the IsHistoric flag.
        if (!info.IsHistoric && (info.DemonetizedOn is not null || info.SuccessorIsoCode is not null))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_Invalid_CurrencyInfoHistoricInconsistent, info.IsoCode),
                paramName);
        }
    }

    /// <summary>
    /// Computes <c>10 ^ minorUnits</c> as a <see cref="decimal" />.
    /// </summary>
    /// <param name="minorUnits">The validated minor-unit precision.</param>
    /// <returns>The scale factor.</returns>
    private static decimal MinorUnitFactor(int minorUnits)
    {
        var factor = 1m;
        for (var i = 0; i < minorUnits; i++)
            factor *= 10m;
        return factor;
    }
}
