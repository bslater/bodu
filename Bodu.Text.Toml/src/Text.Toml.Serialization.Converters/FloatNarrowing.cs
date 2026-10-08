// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FloatNarrowing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Narrows the binary64 value of a TOML float to a smaller floating-point member type, rejecting a finite value outside
/// that type's range rather than letting it become an infinity.
/// </summary>
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
        // Between binary floating-point types every Create method rounds to nearest, and a value beyond the range of T
        // becomes an infinity, which only a finite source distinguishes from TOML's own inf.
        T narrowed = T.CreateTruncating(value);
        if (T.IsInfinity(narrowed) && double.IsFinite(value))
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_FloatOverflow, value, typeof(T)));
        }

        return narrowed;
    }
}
