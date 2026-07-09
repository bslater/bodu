// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatesThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Bodu.Financial.ExchangeRates;

internal static partial class ExchangeRatesThrowHelper
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
                ExchangeRatesResourceStrings.Arg_Invalid_IsoCodeShape,
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
    /// and any non-throwing validation paths, so the ISO-shape rule is defined once for the web provider machinery.
    /// </remarks>
    internal static bool IsValidIsoCodeShape([NotNullWhen(true)] string? value) =>
        value is { Length: 3 }
        && char.IsAsciiLetterUpper(value[0])
        && char.IsAsciiLetterUpper(value[1])
        && char.IsAsciiLetterUpper(value[2]);
}
