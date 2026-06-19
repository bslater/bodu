// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

internal static partial class FinancialThrowHelper
{
    /// <summary>
    /// Throws when <paramref name="value" /> is not a non-null, three-character uppercase ASCII ISO 4217-style code.
    /// </summary>
    /// <param name="value">The candidate currency code to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is not exactly three uppercase ASCII letters.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNotValidIsoCode(
        string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(value, paramName);

        if (value.Length != 3 ||
            !char.IsAsciiLetterUpper(value[0]) ||
            !char.IsAsciiLetterUpper(value[1]) ||
            !char.IsAsciiLetterUpper(value[2]))
        {
            throw new ArgumentException(
                FinancialResourceStrings.Arg_Invalid_IsoCodeShape,
                paramName);
        }
    }

    /// <summary>
    /// Throws when <paramref name="value" /> is the <see cref="CurrencyCode.None" /> sentinel or is not a defined
    /// <see cref="CurrencyCode" /> member.
    /// </summary>
    /// <param name="value">The candidate currency code to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is <see cref="CurrencyCode.None" /> or is not a defined
    /// <see cref="CurrencyCode" /> value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNotDefinedCurrencyCode(
        CurrencyCode value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value == CurrencyCode.None || !Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Arg_Invalid_CurrencyCodeNotMapped,
                    value,
                    (int)value));
        }
    }

    /// <summary>
    /// Throws when <paramref name="value" /> is <see langword="null" />, empty, or contains only white-space.
    /// </summary>
    /// <param name="value">The candidate provider identifier to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is empty or contains only white-space characters.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNullOrWhiteSpaceProvider(
        string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(value, paramName);

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                FinancialResourceStrings.Arg_Invalid_ProviderNullOrWhiteSpace,
                paramName);
    }

    /// <summary>
    /// Throws when <paramref name="value" /> is not a fully constructed <see cref="ExchangeRatePair" />.
    /// </summary>
    /// <param name="value">The candidate pair to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is <see langword="default" />, i.e. its
    /// <see cref="ExchangeRatePair.IsValid" /> property reports <see langword="false" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfInvalidExchangeRatePair(
        ExchangeRatePair value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (!value.IsValid)
            throw new ArgumentException(
                FinancialResourceStrings.Arg_Invalid_ExchangeRatePairDefault,
                paramName);
    }

    /// <summary>
    /// Throws when <paramref name="rate" /> is zero or negative.
    /// </summary>
    /// <param name="rate">The exchange-rate value to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="rate" /> is less than or equal to zero.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfExchangeRateNotPositive(
        decimal rate,
        [CallerArgumentExpression(nameof(rate))] string? paramName = null)
    {
        if (rate <= 0m)
            throw new ArgumentOutOfRangeException(
                paramName,
                rate,
                FinancialResourceStrings.Arg_OutOfRange_ExchangeRateNotPositive);
    }

    /// <summary>
    /// Throws when <paramref name="ratios" /> is empty, contains a negative element, or sums to zero.
    /// </summary>
    /// <param name="ratios">The allocation ratios to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="ratios" /> is empty, contains a negative value, or sums to zero.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfAllocationRatiosInvalid(
        ReadOnlySpan<decimal> ratios,
        [CallerArgumentExpression(nameof(ratios))] string? paramName = null)
    {
        if (ratios.IsEmpty)
            throw new ArgumentException(
                FinancialResourceStrings.Arg_Invalid_AllocationRatiosEmpty,
                paramName);

        decimal totalWeight = 0m;
        for (int i = 0; i < ratios.Length; i++)
        {
            if (ratios[i] < 0m)
                throw new ArgumentException(
                    FinancialResourceStrings.Arg_Invalid_AllocationRatiosNegative,
                    paramName);
            totalWeight += ratios[i];
        }

        if (totalWeight == 0m)
            throw new ArgumentException(
                FinancialResourceStrings.Arg_Invalid_AllocationRatiosAllZero,
                paramName);
    }

    /// <summary>
    /// Throws when <paramref name="mode" /> is not a defined <see cref="MoneyParseMode" /> member.
    /// </summary>
    /// <param name="mode">The parse mode to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="mode" /> is not a defined value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfMoneyParseModeUndefined(
        MoneyParseMode mode,
        [CallerArgumentExpression(nameof(mode))] string? paramName = null)
    {
        if (mode is not MoneyParseMode.StrictIso and not MoneyParseMode.CultureAware
            and not MoneyParseMode.LenientImport and not MoneyParseMode.RoundTripOnly)
        {
            throw new ArgumentOutOfRangeException(paramName, mode, string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_MoneyParseModeUndefined, mode));
        }
    }

    /// <summary>
    /// Throws when <paramref name="policy" /> is not a defined <see cref="ScalePolicy" /> member.
    /// </summary>
    /// <param name="policy">The scale policy to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="policy" /> is not a defined value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfScalePolicyUndefined(
        ScalePolicy policy,
        [CallerArgumentExpression(nameof(policy))] string? paramName = null)
    {
        if (policy is not ScalePolicy.CurrencyMinorUnits and not ScalePolicy.Unrounded and not ScalePolicy.Custom)
            throw new ArgumentOutOfRangeException(paramName, policy, string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_ScalePolicyUndefined, policy));
    }

    /// <summary>
    /// Throws when <paramref name="policy" /> is not a defined <see cref="CashRoundingPolicy" /> member.
    /// </summary>
    /// <param name="policy">The cash-rounding policy to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="policy" /> is not a defined value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfCashRoundingPolicyUndefined(
        CashRoundingPolicy policy,
        [CallerArgumentExpression(nameof(policy))] string? paramName = null)
    {
        if (policy is not CashRoundingPolicy.None and not CashRoundingPolicy.CurrencyCashIncrement)
            throw new ArgumentOutOfRangeException(paramName, policy, string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_CashRoundingPolicyUndefined, policy));
    }

    /// <summary>
    /// Throws when <paramref name="policy" /> is not a defined <see cref="AllocationPolicy" /> member.
    /// </summary>
    /// <param name="policy">The allocation policy to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="policy" /> is not a defined value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfAllocationPolicyUndefined(
        AllocationPolicy policy,
        [CallerArgumentExpression(nameof(policy))] string? paramName = null)
    {
        if (policy is not AllocationPolicy.LargestRemainder)
            throw new ArgumentOutOfRangeException(paramName, policy, string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_AllocationPolicyUndefined, policy));
    }

    /// <summary>
    /// Throws when <paramref name="policy" /> is not a defined <see cref="ConversionRoundingPolicy" /> member.
    /// </summary>
    /// <param name="policy">The conversion-rounding policy to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="policy" /> is not a defined value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfConversionRoundingPolicyUndefined(
        ConversionRoundingPolicy policy,
        [CallerArgumentExpression(nameof(policy))] string? paramName = null)
    {
        if (policy is not ConversionRoundingPolicy.RoundAtTarget and not ConversionRoundingPolicy.Defer)
            throw new ArgumentOutOfRangeException(paramName, policy, string.Format(CultureInfo.CurrentCulture, FinancialResourceStrings.Arg_OutOfRange_ConversionRoundingPolicyUndefined, policy));
    }

    /// <summary>
    /// Throws when <paramref name="minorUnits" /> is outside the supported range 0 to 28.
    /// </summary>
    /// <param name="minorUnits">The candidate minor-unit scale to validate.</param>
    /// <param name="isoCode">The currency code reported in the exception message for context.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="minorUnits" /> is less than zero or greater than 28.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfMinorUnitsOutOfRange(
        int minorUnits,
        string isoCode,
        [CallerArgumentExpression(nameof(minorUnits))] string? paramName = null)
    {
        if ((uint)minorUnits > 28u)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                minorUnits,
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Arg_OutOfRange_UnknownCurrencyMinorUnits,
                    minorUnits,
                    isoCode));
        }
    }

    /// <summary>
    /// Throws when <paramref name="policy" /> is not a defined <see cref="MoneyBagConversionRoundingPolicy" /> member.
    /// </summary>
    /// <param name="policy">The rounding policy to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="policy" /> is not a defined value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfMoneyBagRoundingPolicyUndefined(
        MoneyBagConversionRoundingPolicy policy,
        [CallerArgumentExpression(nameof(policy))] string? paramName = null)
    {
        if (policy is not MoneyBagConversionRoundingPolicy.SumRawThenRound
            and not MoneyBagConversionRoundingPolicy.RoundEachCurrencyThenSum)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                policy,
                FinancialResourceStrings.Arg_OutOfRange_UnsupportedMoneyBagRoundingPolicy);
        }
    }

    /// <summary>
    /// Throws when <paramref name="policy" /> is not a defined <see cref="Serialization.FinancialJsonPolicy" /> member.
    /// </summary>
    /// <param name="policy">The policy value to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="policy" /> is not a defined value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfFinancialJsonPolicyUndefined(
        Serialization.FinancialJsonPolicy policy,
        [CallerArgumentExpression(nameof(policy))] string? paramName = null)
    {
        if (policy is not Serialization.FinancialJsonPolicy.Strict
            and not Serialization.FinancialJsonPolicy.Lenient
            and not Serialization.FinancialJsonPolicy.Compact)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                policy,
                string.Format(
                    CultureInfo.CurrentCulture,
                    FinancialResourceStrings.Op_Invalid_FinancialJsonPolicyUndefined,
                    policy));
        }
    }

    /// <summary>
    /// Throws a <see cref="FormatException" /> reporting that <paramref name="format" /> is not a supported specifier.
    /// </summary>
    /// <param name="format">The unsupported format specifier supplied by the caller.</param>
    /// <exception cref="FormatException">Always thrown.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static void ThrowFormatSpecifierUnsupported(ReadOnlySpan<char> format) =>
        throw new FormatException(
            string.Format(
                CultureInfo.CurrentCulture,
                FinancialResourceStrings.Format_Invalid_FormatSpecifier,
                format.ToString()));
}
