// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FloatNarrowing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Narrows the binary64 value of a TOML float to a smaller floating-point type, rejecting a finite value outside that
/// type's range rather than letting it become an infinity.
/// </summary>
/// <remarks>
/// The serializer's <see cref="float" /> and <see cref="Half" /> converters, <c>TomlValue.TryGetValue</c> and
/// <c>Utf8TomlReader.TryGetSingle</c> all apply this one rule.
/// </remarks>
internal static class FloatNarrowing
{
    /// <summary>
    /// Narrows a TOML float's value to <typeparamref name="T" />, rounding to the nearest representable value.
    /// </summary>
    /// <typeparam name="T">The floating-point member type.</typeparam>
    /// <param name="value">The value read from the TOML float.</param>
    /// <returns>The narrowed value.</returns>
    /// <exception cref="TomlSerializationException">
    /// <paramref name="value" /> is finite but outside the range of <typeparamref name="T" />.
    /// </exception>
    /// <remarks>
    /// A value that rounds to the largest finite <typeparamref name="T" />, or that underflows towards zero, is
    /// accepted, and TOML's <c>inf</c>, <c>-inf</c>, and <c>nan</c> narrow to the matching values of
    /// <typeparamref name="T" />.
    /// </remarks>
    internal static T Narrow<T>(double value)
        where T : IFloatingPointIeee754<T>
    {
        if (!TryNarrow(value, out T narrowed))
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_FloatOverflow, value, typeof(T)));
        }

        return narrowed;
    }

    /// <summary>
    /// Attempts to narrow a TOML float's value to <typeparamref name="T" />, rounding to the nearest representable
    /// value.
    /// </summary>
    /// <typeparam name="T">The floating-point type to narrow to.</typeparam>
    /// <param name="value">The value read from the TOML float.</param>
    /// <param name="narrowed">
    /// When this method returns <see langword="true" />, the narrowed value; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the value narrows; <see langword="false" /> when it is finite but outside the range
    /// of <typeparamref name="T" />.
    /// </returns>
    internal static bool TryNarrow<T>(double value, out T narrowed)
        where T : IFloatingPointIeee754<T>
    {
        // Between binary floating-point types every Create method rounds to nearest, and a value beyond the range of T
        // becomes an infinity, which only a finite source distinguishes from TOML's own inf.
        narrowed = T.CreateTruncating(value);
        if (!IsOutOfRange(value, narrowed))
            return true;

        narrowed = T.Zero;
        return false;
    }

    /// <summary>
    /// Determines whether narrowing a binary64 value overflowed: the value is finite but its narrowed form is an
    /// infinity.
    /// </summary>
    /// <typeparam name="T">The floating-point type the value was narrowed to.</typeparam>
    /// <param name="value">The binary64 value before narrowing.</param>
    /// <param name="narrowed">The value after narrowing.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is outside the range of <typeparamref name="T" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    internal static bool IsOutOfRange<T>(double value, T narrowed)
        where T : IFloatingPointIeee754<T> =>
        double.IsFinite(value) && T.IsInfinity(narrowed);
}
