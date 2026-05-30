// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateValidation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Numerics;

/// <summary>
/// Provides shared argument-validation helpers used by the exchange-rate types so that ISO 4217-style code rules and
/// provider-name rules are enforced identically across every public entry point.
/// </summary>
internal static class ExchangeRateValidation
{
    /// <summary>
    /// Validates that <paramref name="value" /> is a non-null three-character uppercase ASCII currency code.
    /// </summary>
    /// <param name="value">The candidate currency code to validate.</param>
    /// <param name="paramName">
    /// The name of the originating parameter. Captured automatically by the compiler when omitted.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="value" /> is not exactly three characters long or contains any character that is not an
    /// uppercase ASCII letter.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequireIsoCode(
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
                "Currency code must be a three-character uppercase ISO-style code.",
                paramName);
        }
    }

    /// <summary>
    /// Validates that <paramref name="value" /> is a non-null, non-empty, non-whitespace provider identifier.
    /// </summary>
    /// <param name="value">The candidate provider identifier to validate.</param>
    /// <param name="paramName">
    /// The name of the originating parameter. Captured automatically by the compiler when omitted.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="value" /> is empty or contains only white-space characters.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequireProvider(
        string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ThrowHelper.ThrowIfNull(value, paramName);

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Provider identifier must not be empty or white-space.", paramName);
    }
}
