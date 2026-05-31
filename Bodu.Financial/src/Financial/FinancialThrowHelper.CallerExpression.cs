// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

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

        var totalWeight = 0m;
        for (var i = 0; i < ratios.Length; i++)
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
                    CultureInfo.InvariantCulture,
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
                CultureInfo.InvariantCulture,
                FinancialResourceStrings.Format_Invalid_FormatSpecifier,
                format.ToString()));
}
