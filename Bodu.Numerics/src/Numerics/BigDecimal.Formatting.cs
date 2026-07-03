// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimal.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Numerics;

public readonly partial struct BigDecimal
{
    /// <summary>
    /// Returns the plain (non-scientific) decimal string representation of the value using the invariant culture.
    /// </summary>
    /// <returns>The canonical decimal text, for example <c>"0.5"</c>, <c>"100"</c>, or <c>"-3.14"</c>.</returns>
    public override string ToString()
    {
        if (_scale == 0)
            return _mantissa.ToString(CultureInfo.InvariantCulture);

        bool negative = _mantissa.Sign < 0;
        string digits = BigInteger.Abs(_mantissa).ToString(CultureInfo.InvariantCulture);
        if (digits.Length <= _scale)
            digits = digits.PadLeft(_scale + 1, '0');

        int pointIndex = digits.Length - _scale;
        string text = string.Concat(digits.AsSpan(0, pointIndex), ".", digits.AsSpan(pointIndex));
        return negative ? "-" + text : text;
    }
}
