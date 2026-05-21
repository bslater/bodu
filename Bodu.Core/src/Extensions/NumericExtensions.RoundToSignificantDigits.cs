// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensions.RoundToSignificantDigits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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

        var magnitude = Math.Pow(10, digits - (int)Math.Ceiling(Math.Log10(Math.Abs(value))));
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
    public static decimal RoundToSignificantDigits(this decimal value, int digits)
    {
        ThrowHelper.ThrowIfOutOfRange(digits, 1, 28);

        if (value == 0m) return value;

        var magnitudeExponent = digits - Math.Ceiling(Math.Log10((double)Math.Abs(value)));
        var magnitude = (decimal)Math.Pow(10, magnitudeExponent);
        return Math.Round(value * magnitude, MidpointRounding.AwayFromZero) / magnitude;
    }
}
