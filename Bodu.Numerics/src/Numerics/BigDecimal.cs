// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable, arbitrary-precision decimal number — a <see cref="BigInteger" /> unscaled value together
/// with a base-ten scale.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="BigDecimal" /> is the value <c>UnscaledValue &#215; 10<sup>-Scale</sup></c>. Unlike
/// <see cref="decimal" /> it has no fixed precision or range limit: both the number of significant digits and the
/// exponent grow as needed.
/// </para>
/// <para>
/// Every value is held in a single canonical form so equal values share one representation and compare and hash
/// equally: trailing zeros are removed from the unscaled value down to — but never past — a scale of zero, so
/// <c>1.0</c> and <c>1.00</c> are the same value, and an integer such as <c>100</c> keeps a scale of zero.
/// </para>
/// <para>
/// Addition, subtraction, multiplication, and remainder are exact. Division is generally not exact (for example
/// <c>1 / 3</c>), so the <c>/</c> operator and <see cref="Divide(BigDecimal, BigDecimal)" /> round to a default working
/// scale of <see cref="DefaultDivisionScale" /> decimal places using <see cref="MidpointRounding.ToEven" />; use
/// <see cref="Divide(BigDecimal, BigDecimal, int, MidpointRounding)" /> to choose the scale and rounding explicitly.
/// Exact quotients self-trim through canonicalization, so <c>1 / 2</c> is <c>0.5</c>.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// BigDecimal price = BigDecimal.Parse("19.99");
/// BigDecimal tax = price * BigDecimal.Parse("0.10");   // 1.999
/// BigDecimal total = price + tax;                      // 21.989
///
/// BigDecimal third = BigDecimal.One / 3;               // 0.3333… (50 places, half-even)
/// BigDecimal half = BigDecimal.One / 2;                // 0.5 (exact)
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct BigDecimal
{
    /// <summary>The default number of fractional decimal places produced by the <c>/</c> operator and <see cref="Divide(BigDecimal, BigDecimal)" />.</summary>
    public const int DefaultDivisionScale = 50;

    /// <summary>The maximum magnitude permitted for a negative scale before it is rejected as unrepresentable.</summary>
    /// <remarks>
    /// A negative scale is folded into the unscaled value as <c>mantissa &#215; 10<sup>-scale</sup></c>. Left
    /// unbounded, an attacker-supplied scale or parse exponent drives <see cref="BigInteger.Pow(BigInteger, int)" />
    /// with an arbitrarily large exponent, producing a multi-gigabyte value that exhausts memory or hangs. Capping
    /// the magnitude keeps the widening bounded while still admitting any realistic value.
    /// </remarks>
    private const int MaxNegativeScaleMagnitude = 1_000_000;

    /// <summary>The unscaled integer significand; the value is <see cref="_mantissa" /> &#215; 10<sup>-<see cref="_scale" /></sup>.</summary>
    private readonly BigInteger _mantissa;

    /// <summary>The non-negative base-ten scale — the number of fractional decimal places in the canonical form.</summary>
    private readonly int _scale;

    /// <summary>
    /// Initializes a new instance of the <see cref="BigDecimal" /> struct from an unscaled value and scale, reducing
    /// the result to canonical form.
    /// </summary>
    /// <param name="unscaledValue">The unscaled integer significand.</param>
    /// <param name="scale">
    /// The base-ten scale; a negative scale multiplies the unscaled value by <c>10<sup>-scale</sup></c>.
    /// </param>
    public BigDecimal(BigInteger unscaledValue, int scale)
    {
        Normalize(ref unscaledValue, ref scale);
        _mantissa = unscaledValue;
        _scale = scale;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BigDecimal" /> struct representing the integer
    /// <paramref name="value" />.
    /// </summary>
    /// <param name="value">The integer value the decimal represents.</param>
    public BigDecimal(BigInteger value)
    {
        _mantissa = value;
        _scale = 0;
    }

    /// <summary>
    /// Gets a <see cref="BigDecimal" /> representing the value zero.
    /// </summary>
    /// <value>The value <c>0</c>.</value>
    public static BigDecimal Zero =>
        default;

    /// <summary>
    /// Gets a <see cref="BigDecimal" /> representing the value one.
    /// </summary>
    /// <value>The value <c>1</c>.</value>
    public static BigDecimal One =>
        new(BigInteger.One);

    /// <summary>
    /// Gets a <see cref="BigDecimal" /> representing the value negative one.
    /// </summary>
    /// <value>The value <c>-1</c>.</value>
    public static BigDecimal MinusOne =>
        new(BigInteger.MinusOne);

    /// <summary>
    /// Gets a <see cref="BigDecimal" /> representing the value ten.
    /// </summary>
    /// <value>The value <c>10</c>.</value>
    public static BigDecimal Ten =>
        new(new BigInteger(10));

    /// <summary>
    /// Gets the unscaled integer significand of the canonical form.
    /// </summary>
    /// <value>
    /// The value <c>V</c> such that this decimal equals <c>V &#215; 10<sup>-<see cref="Scale" /></sup></c>.
    /// </value>
    public BigInteger UnscaledValue =>
        _mantissa;

    /// <summary>
    /// Gets the base-ten scale of the canonical form — the number of fractional decimal places.
    /// </summary>
    /// <value>A non-negative scale; zero for an integer value.</value>
    public int Scale =>
        _scale;

    /// <summary>
    /// Gets the sign of the value.
    /// </summary>
    /// <value><c>-1</c> when negative, <c>0</c> when zero, and <c>1</c> when positive.</value>
    public int Sign =>
        _mantissa.Sign;

    /// <summary>
    /// Gets a value indicating whether the value is zero.
    /// </summary>
    /// <value><see langword="true" /> when the value is zero; otherwise <see langword="false" />.</value>
    public bool IsZero =>
        _mantissa.IsZero;

    /// <summary>
    /// Deconstructs the value into its canonical unscaled value and scale.
    /// </summary>
    /// <param name="unscaledValue">When this method returns, contains the canonical unscaled value.</param>
    /// <param name="scale">When this method returns, contains the canonical scale.</param>
    public void Deconstruct(out BigInteger unscaledValue, out int scale)
    {
        unscaledValue = _mantissa;
        scale = _scale;
    }

    /// <summary>
    /// Reduces an unscaled value and scale to canonical form: a non-negative scale with no trailing zeros in the
    /// unscaled value unless the scale is already zero, and <c>(0, 0)</c> for the value zero.
    /// </summary>
    /// <param name="mantissa">The unscaled value to reduce, updated in place.</param>
    /// <param name="scale">The scale to reduce, updated in place.</param>
    private static void Normalize(ref BigInteger mantissa, ref int scale)
    {
        if (mantissa.IsZero)
        {
            scale = 0;
            return;
        }

        // A negative scale is folded into the unscaled value so the canonical scale is always non-negative. Bound
        // the widening exponent first: -scale is computed as a long so scale == int.MinValue does not overflow, and
        // an excessive magnitude is rejected rather than driving an unbounded BigInteger.Pow.
        if (scale < 0)
        {
            long power = -(long)scale;
            if (power > MaxNegativeScaleMagnitude)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scale),
                    string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Arg_OutOfRange_BigDecimalScale, MaxNegativeScaleMagnitude));
            }

            mantissa *= BigInteger.Pow(BigInteger.CreateChecked(10), (int)power);
            scale = 0;
            return;
        }

        // Strip trailing base-ten zeros, but stop at scale zero so integers keep their full digit run.
        var ten = new BigInteger(10);
        while (scale > 0)
        {
            BigInteger quotient = BigInteger.DivRem(mantissa, ten, out BigInteger remainder);
            if (!remainder.IsZero)
                break;

            mantissa = quotient;
            scale--;
        }
    }

    /// <summary>
    /// Rescales <paramref name="value" /> to the target scale <paramref name="targetScale" /> without rounding,
    /// assuming the target scale is at least the value's own scale.
    /// </summary>
    /// <param name="value">The value to rescale.</param>
    /// <param name="targetScale">The scale to rescale to; must be greater than or equal to <c>value.Scale</c>.</param>
    /// <returns>The unscaled value expressed at <paramref name="targetScale" />.</returns>
    private static BigInteger ScaleTo(in BigDecimal value, int targetScale) =>
        value._mantissa * BigInteger.Pow(new BigInteger(10), targetScale - value._scale);
}
