// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
{
    /// <summary>
    /// Gets a value indicating whether the value is an integer — has no fractional part.
    /// </summary>
    /// <value><see langword="true" /> when the canonical scale is zero; otherwise <see langword="false" />.</value>
    public bool IsInteger =>
        _scale == 0;

    /// <summary>
    /// Gets a value indicating whether the value is negative.
    /// </summary>
    /// <value><see langword="true" /> when the value is less than zero; otherwise <see langword="false" />.</value>
    public bool IsNegative =>
        _mantissa.Sign < 0;

    /// <summary>
    /// Gets a value indicating whether the value is positive — strictly greater than zero.
    /// </summary>
    /// <value><see langword="true" /> when the value is greater than zero; otherwise <see langword="false" />.</value>
    public bool IsPositive =>
        _mantissa.Sign > 0;

    /// <summary>
    /// Gets the precision — the number of significant decimal digits in the unscaled value.
    /// </summary>
    /// <value>The digit count of the unscaled value; <c>1</c> for zero.</value>
    public int Precision =>
        _mantissa.IsZero ? 1 : BigInteger.Abs(_mantissa).ToString(CultureInfo.InvariantCulture).Length;
}
