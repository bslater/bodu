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

        if (!IsValidIsoCodeShape(value))
        {
            throw new ArgumentException(
                FinancialResourceStrings.Arg_Invalid_IsoCodeShape,
                paramName);
        }
    }

    /// <summary>
    /// Determines whether <paramref name="value" /> has the canonical ISO 4217 alphabetic shape: exactly three
    /// uppercase ASCII letters.
    /// </summary>
    /// <param name="value">The candidate currency code.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is exactly three uppercase ASCII letters; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The single shape predicate shared by the throwing <see cref="ThrowIfNotValidIsoCode(string, string?)" /> guard
    /// and the non-throwing lookup paths, so the ISO-shape rule is defined once. In particular it excludes the numeric
    /// underlying-value form (for example <c>"840"</c>) that
    /// <see cref="System.Enum.TryParse{TEnum}(string, out TEnum)" /> would otherwise accept.
    /// </remarks>
    internal static bool IsValidIsoCodeShape([NotNullWhen(true)] string? value) =>
        value is { Length: 3 }
        && char.IsAsciiLetterUpper(value[0])
        && char.IsAsciiLetterUpper(value[1])
        && char.IsAsciiLetterUpper(value[2]);

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
