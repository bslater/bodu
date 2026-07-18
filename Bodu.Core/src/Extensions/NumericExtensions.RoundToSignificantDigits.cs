// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.RoundToSignificantDigits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NumericExtensions
{
    /// <summary>
    /// Rounds a <see cref="double" /> to the specified number of significant decimal digits.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="digits">
    /// The number of significant digits to retain. Must be between <c>1</c> and <c>15</c>, inclusive (the practical
    /// precision limit of <see cref="double" />).
    /// </param>
    /// <returns>
    /// <paramref name="value" /> rounded to <paramref name="digits" /> significant digits. Returns
    /// <paramref name="value" /> unchanged when it is zero, NaN, or infinite.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than <c>1</c> or greater than <c>15</c>.
    /// </exception>
    /// <remarks>
    /// Unlike <see cref="Math.Round(double, int)" />, which rounds to a fixed number of fractional digits, this method
    /// scales the rounding magnitude to the order of magnitude of <paramref name="value" />. Negative values are
    /// handled symmetrically.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// 12345.6789.RoundToSignificantDigits(3); // => 12300
    /// 0.0012345.RoundToSignificantDigits(2); // => 0.0012
    ///]]>
    /// </code>
    /// </example>
    public static double RoundToSignificantDigits(this double value, int digits)
    {
        ThrowHelper.ThrowIfOutOfRange(digits, 1, 15);

        if (value == 0d || double.IsNaN(value) || double.IsInfinity(value))
            return value;

        double magnitude = Math.Pow(10, digits - (int)Math.Ceiling(Math.Log10(Math.Abs(value))));
        return Math.Round(value * magnitude, MidpointRounding.AwayFromZero) / magnitude;
    }

    /// <summary>
    /// Rounds a <see cref="decimal" /> to the specified number of significant decimal digits.
    /// </summary>
    /// <param name="value">The value to round.</param>
    /// <param name="digits">
    /// The number of significant digits to retain. Must be between <c>1</c> and <c>28</c>, inclusive (the precision
    /// limit of <see cref="decimal" />).
    /// </param>
    /// <returns>
    /// <paramref name="value" /> rounded to <paramref name="digits" /> significant digits. Returns
    /// <paramref name="value" /> unchanged when it is zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="digits" /> is less than <c>1</c> or greater than <c>28</c>.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Rounding away from zero pushes the result beyond <see cref="decimal.MaxValue" /> or
    /// <see cref="decimal.MinValue" /> (possible only for values within one rounding step of the range limit).
    /// </exception>
    /// <remarks>
    /// The computation is performed entirely in <see cref="decimal" /> arithmetic so the full 28-digit precision is
    /// preserved; no intermediate <see cref="double" /> conversion occurs.
    /// </remarks>
    public static decimal RoundToSignificantDigits(this decimal value, int digits)
    {
        ThrowHelper.ThrowIfOutOfRange(digits, 1, 28);

        if (value == 0m) return value;

        // Order of magnitude in pure decimal arithmetic: exponent such that 10^(exponent-1) <= |value| < 10^exponent.
        decimal abs = Math.Abs(value);
        int exponent = 1;
        while (abs >= 10m)
        {
            abs /= 10m;
            exponent++;
        }

        while (abs < 1m)
        {
            abs *= 10m;
            exponent--;
        }

        int decimalPlaces = digits - exponent;
        if (decimalPlaces >= 0)
        {
            // A decimal carries at most 28 fractional digits, so rounding beyond that is the identity.
            return Math.Round(value, Math.Min(decimalPlaces, 28), MidpointRounding.AwayFromZero);
        }

        // Rounding above the units position: scale down, round to an integer, scale back.
        decimal scale = 1m;
        for (int i = 0; i < -decimalPlaces; i++)
            scale *= 10m;

        return Math.Round(value / scale, 0, MidpointRounding.AwayFromZero) * scale;
    }
}
