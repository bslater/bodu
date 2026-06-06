// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoValidator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
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
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_OutOfRange_CurrencyInfoMinorUnits, info.IsoCode, info.MinorUnits));
        }

        if (info.CashRoundingIncrement < 0m)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                info.CashRoundingIncrement,
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_OutOfRange_CurrencyInfoCashRoundingNegative, info.IsoCode, info.CashRoundingIncrement));
        }

        if (info.CashRoundingIncrement != 0m)
        {
            var scaled = info.CashRoundingIncrement * MinorUnitFactor(info.MinorUnits);
            if (scaled != decimal.Truncate(scaled))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_Invalid_CurrencyInfoCashRoundingTooFine, info.IsoCode, info.CashRoundingIncrement, info.MinorUnits),
                    paramName);
            }
        }

        if ((uint)info.NumericCode >= NumericCodeUpperBound)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                info.NumericCode,
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_OutOfRange_CurrencyInfoNumericCode, info.IsoCode, info.NumericCode));
        }

        if (info.SuccessorIsoCode is not null)
            FinancialThrowHelper.ThrowIfNotValidIsoCode(info.SuccessorIsoCode, paramName);

        // A non-historic currency must not carry demonetization metadata; historicity and its supporting fields must
        // agree so downstream consumers can trust the IsHistoric flag.
        if (!info.IsHistoric && (info.DemonetizedOn is not null || info.SuccessorIsoCode is not null))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, FinancialResourceStrings.Arg_Invalid_CurrencyInfoHistoricInconsistent, info.IsoCode),
                paramName);
        }
    }

    /// <summary>
    /// Determines whether <paramref name="info" /> satisfies every currency-metadata rule, without throwing.
    /// </summary>
    /// <param name="info">The currency metadata to test.</param>
    /// <returns><see langword="true" /> when the metadata is valid; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// Mirrors the rules enforced by <see cref="Validate" /> so that <see cref="CurrencyRegistry" /> can report an
    /// invalid candidate as a result on its non-throwing registration paths rather than by catching an exception.
    /// </remarks>
    public static bool TryValidate([NotNullWhen(true)] CurrencyInfo? info)
    {
        if (info is null || !IsValidIsoCode(info.IsoCode))
            return false;

        if ((uint)info.MinorUnits > MaxMinorUnits)
            return false;

        if (info.CashRoundingIncrement < 0m)
            return false;

        if (info.CashRoundingIncrement != 0m)
        {
            var scaled = info.CashRoundingIncrement * MinorUnitFactor(info.MinorUnits);
            if (scaled != decimal.Truncate(scaled))
                return false;
        }

        if ((uint)info.NumericCode >= NumericCodeUpperBound)
            return false;

        if (info.SuccessorIsoCode is not null && !IsValidIsoCode(info.SuccessorIsoCode))
            return false;

        // A non-historic currency must not carry demonetization metadata.
        if (!info.IsHistoric && (info.DemonetizedOn is not null || info.SuccessorIsoCode is not null))
            return false;

        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="value" /> is a non-null, three-character uppercase ASCII ISO 4217-style code.
    /// </summary>
    /// <param name="value">The candidate code.</param>
    /// <returns><see langword="true" /> when the code is well formed.</returns>
    private static bool IsValidIsoCode([NotNullWhen(true)] string? value) =>
        value is { Length: 3 }
            && char.IsAsciiLetterUpper(value[0])
            && char.IsAsciiLetterUpper(value[1])
            && char.IsAsciiLetterUpper(value[2]);

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
