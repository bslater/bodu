// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyMetadata.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Once-per-closed-generic validated metadata for an <see cref="ICurrency" /> tag. Lazily computed and cached in a
/// static field so the validation runs at most once per <typeparamref name="TCurrency" />.
/// </summary>
/// <typeparam name="TCurrency">The currency tag type.</typeparam>
/// <remarks>
/// Every <see cref="Money{TCurrency}" /> code path that needs <see cref="ICurrency.MinorUnits" />,
/// <see cref="ICurrency.IsoCode" />, or <see cref="ICurrency.CashRoundingIncrement" /> reads it from
/// <see cref="Value" /> rather than the raw interface members. That keeps validation centralised — a single
/// failing-fast boundary — and prevents the type from half-existing with broken metadata.
/// </remarks>
internal static class CurrencyMetadata<TCurrency>
    where TCurrency : ICurrency
{
    /// <summary>
    /// Lazy-initialised descriptor. Using <see cref="Lazy{T}" /> with the default
    /// <see cref="System.Threading.LazyThreadSafetyMode.ExecutionAndPublication" /> mode means a validation failure
    /// surfaces as the original <see cref="InvalidOperationException" /> rather than being wrapped in a
    /// <see cref="TypeInitializationException" /> by the runtime's static-field initialiser, and the cached exception
    /// is rethrown directly on every subsequent access.
    /// </summary>
    private static readonly Lazy<CurrencyMetadataDescriptor> s_value = new(Validate);

    /// <summary>
    /// The validated descriptor for <typeparamref name="TCurrency" />.
    /// </summary>
    public static CurrencyMetadataDescriptor Value =>
        s_value.Value;

    /// <summary>
    /// Validates the metadata exposed by <typeparamref name="TCurrency" /> and caches the result.
    /// </summary>
    /// <returns>The validated descriptor.</returns>
    /// <exception cref="InvalidOperationException">
    /// The currency reports invalid metadata. The exception message names the offending field and the
    /// <typeparamref name="TCurrency" /> type.
    /// </exception>
    private static CurrencyMetadataDescriptor Validate()
    {
        var isoCode = TCurrency.IsoCode;
        ValidateIsoCode(isoCode, nameof(ICurrency.IsoCode));

        var minorUnits = TCurrency.MinorUnits;
        if ((uint)minorUnits > 28u)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyMinorUnitsOutOfRange,
                    typeof(TCurrency).FullName,
                    nameof(ICurrency.MinorUnits),
                    minorUnits));
        }

        var minorUnitFactor = ComputeMinorUnitFactor(minorUnits);

        var cashIncrement = TCurrency.CashRoundingIncrement;
        if (cashIncrement < 0m)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyCashRoundingNegative,
                    typeof(TCurrency).FullName,
                    nameof(ICurrency.CashRoundingIncrement),
                    cashIncrement));
        }

        if (cashIncrement != 0m)
        {
            var scaled = cashIncrement * minorUnitFactor;
            if (scaled != decimal.Truncate(scaled))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        FinancialResourceStrings.Op_Invalid_CurrencyCashRoundingTooFine,
                        typeof(TCurrency).FullName,
                        nameof(ICurrency.CashRoundingIncrement),
                        cashIncrement,
                        minorUnits));
            }
        }

        var successorIsoCode = TCurrency.SuccessorIsoCode;
        if (successorIsoCode is not null)
            ValidateIsoCode(successorIsoCode, nameof(ICurrency.SuccessorIsoCode));

        return new CurrencyMetadataDescriptor(
            isoCode,
            minorUnits,
            minorUnitFactor,
            cashIncrement,
            TCurrency.IsHistoric,
            TCurrency.DemonetizedOn,
            successorIsoCode);
    }

    /// <summary>
    /// Validates that <paramref name="value" /> is a three-letter uppercase-ASCII ISO 4217 alphabetic code.
    /// </summary>
    /// <param name="value">The candidate ISO code.</param>
    /// <param name="memberName">The member name to include in the exception message.</param>
    /// <exception cref="InvalidOperationException">
    /// The code is null, the wrong length, or contains non-uppercase-letter characters.
    /// </exception>
    private static void ValidateIsoCode(string value, string memberName)
    {
        if (value is null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyIsoCodeNull,
                    typeof(TCurrency).FullName,
                    memberName));
        }

        if (value.Length != 3)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyIsoCodeLength,
                    typeof(TCurrency).FullName,
                    memberName,
                    value,
                    value.Length));
        }

        for (var i = 0; i < 3; i++)
        {
            var c = value[i];
            if (c is < 'A' or > 'Z')
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        FinancialResourceStrings.Op_Invalid_CurrencyIsoCodeNonAscii,
                        typeof(TCurrency).FullName,
                        memberName,
                        value));
            }
        }
    }

    /// <summary>
    /// Computes <c>10 ^ minorUnits</c> as a <see cref="decimal" />.
    /// </summary>
    /// <param name="minorUnits">The currency's minor-unit precision (already validated).</param>
    /// <returns>The scale factor.</returns>
    private static decimal ComputeMinorUnitFactor(int minorUnits)
    {
        var factor = 1m;
        for (var i = 0; i < minorUnits; i++)
            factor *= 10m;
        return factor;
    }
}

/// <summary>
/// Snapshotted validated metadata for a currency tag type.
/// </summary>
/// <param name="IsoCode">The validated ISO 4217 alphabetic code.</param>
/// <param name="MinorUnits">The validated minor-unit precision.</param>
/// <param name="MinorUnitFactor">Pre-computed <c>10 ^ MinorUnits</c> for use in allocation and conversion.</param>
/// <param name="CashRoundingIncrement">The validated cash-rounding increment, or <c>0m</c> when none.</param>
/// <param name="IsHistoric">Whether the currency has been demonetized.</param>
/// <param name="DemonetizedOn">The demonetization date, when known.</param>
/// <param name="SuccessorIsoCode">The replacement currency's ISO code, when applicable.</param>
internal readonly record struct CurrencyMetadataDescriptor(
    string IsoCode,
    int MinorUnits,
    decimal MinorUnitFactor,
    decimal CashRoundingIncrement,
    bool IsHistoric,
    DateOnly? DemonetizedOn,
    string? SuccessorIsoCode);
