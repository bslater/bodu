// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyMetadata{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.Currencies;

/// <summary>
/// Once-per-closed-generic validated metadata for an <see cref="ICurrency" /> tag. Lazily computed and cached in a
/// static field so the validation runs at most once per <typeparamref name="TCurrency" />.
/// </summary>
/// <typeparam name="TCurrency">The currency tag type.</typeparam>
/// <remarks>
/// Every <see cref="Money{TCurrency}" /> code path that needs <see cref="ICurrency.MinorUnits" />,
/// <see cref="ICurrency.IsoCode" />, or <see cref="ICurrency.CashRoundingIncrement" /> reads it from
/// <see cref="Value" /> rather than the raw interface members. That keeps validation centralized - a single
/// failing-fast boundary - and prevents the type from half-existing with broken metadata.
/// </remarks>
internal static class CurrencyMetadata<TCurrency>
    where TCurrency : ICurrency
{
    /// <summary>Lazy-initialized descriptor. Using <see cref="Lazy{T}" /> with the default <see cref="System.Threading.LazyThreadSafetyMode.ExecutionAndPublication" /> mode means a validation failure surfaces as the original <see cref="InvalidOperationException" /> rather than being wrapped in a <see cref="TypeInitializationException" /> by the runtime's static-field initializer, and the cached exception is rethrown directly on every subsequent access.</summary>
    private static readonly Lazy<CurrencyMetadataDescriptor> s_value = new(Validate);

    /// <summary>
    /// Gets the validated descriptor for <typeparamref name="TCurrency" />.
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
        string isoCode = TCurrency.IsoCode;
        ValidateIsoCode(isoCode, nameof(ICurrency.IsoCode));

        int minorUnits = TCurrency.MinorUnits;
        if ((uint)minorUnits > 28u)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyMinorUnitsOutOfRange,
                    typeof(TCurrency).FullName,
                    nameof(ICurrency.MinorUnits),
                    minorUnits));
        }

        decimal minorUnitFactor = ComputeMinorUnitFactor(minorUnits);

        decimal cashIncrement = TCurrency.CashRoundingIncrement;
        if (cashIncrement < 0m)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyCashRoundingNegative,
                    typeof(TCurrency).FullName,
                    nameof(ICurrency.CashRoundingIncrement),
                    cashIncrement));
        }

        if (cashIncrement != 0m)
        {
            // Compare with the increment rounded to the minor units rather than scaling it by 10^minorUnits, which
            // overflows for a whole increment at high precision (8 at 28 minor units).
            if (!MoneyMath.IsWholeNumberOfMinorUnits(cashIncrement, minorUnits))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        FinancialResourceStrings.Op_Invalid_CurrencyCashRoundingTooFine,
                        typeof(TCurrency).FullName,
                        nameof(ICurrency.CashRoundingIncrement),
                        cashIncrement,
                        minorUnits));
            }
        }

        string? successorIsoCode = TCurrency.SuccessorIsoCode;
        if (successorIsoCode is not null)
            ValidateIsoCode(successorIsoCode, nameof(ICurrency.SuccessorIsoCode));

        // Resolve the stored CurrencyCode once. A tag whose ISO code is outside the shipped catalogue resolves to
        // CurrencyCode.None; the runtime-tagged bridge paths surface that as a mismatch when they need the enum.
        CurrencyCode code = CurrencyInfo.TryGetCurrencyCode(isoCode, out CurrencyCode resolved) ? resolved : CurrencyCode.None;

        return new CurrencyMetadataDescriptor(
            isoCode,
            code,
            minorUnits,
            minorUnitFactor,
            cashIncrement,
            TCurrency.IsHistoric,
            TCurrency.DemonetizedOn,
            successorIsoCode,
            TCurrency.EnglishName ?? string.Empty);
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
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyIsoCodeNull,
                    typeof(TCurrency).FullName,
                    memberName));
        }

        if (value.Length != 3)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_Invalid_CurrencyIsoCodeLength,
                    typeof(TCurrency).FullName,
                    memberName,
                    value,
                    value.Length));
        }

        for (int i = 0; i < 3; i++)
        {
            char c = value[i];
            if (c is < 'A' or > 'Z')
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
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
        decimal factor = 1m;
        for (int i = 0; i < minorUnits; i++)
            factor *= 10m;
        return factor;
    }
}
