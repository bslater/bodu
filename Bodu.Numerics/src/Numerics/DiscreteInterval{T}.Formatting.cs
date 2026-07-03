// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiscreteInterval{T}.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public readonly partial struct DiscreteInterval<T>
{
    /// <summary>
    /// Returns the canonical bracket-notation string representation — always closed on a finite side, using the infinity
    /// glyphs on an unbounded side, and the empty-set glyph for the empty interval.
    /// </summary>
    /// <returns>
    /// <c>"[first, last]"</c> for a bounded interval, <c>"[first, +&#x221E;)"</c> / <c>"(-&#x221E;, last]"</c> /
    /// <c>"(-&#x221E;, +&#x221E;)"</c> for unbounded shapes, or <c>"&#x2205;"</c> when empty.
    /// </returns>
    public override string ToString()
    {
        if (IsEmpty)
            return "∅";

        char lowerBracket = LowerUnbounded ? '(' : '[';
        char upperBracket = UpperUnbounded ? ')' : ']';
        string lowerText = LowerUnbounded ? "-∞" : _first.ToString(null, CultureInfo.CurrentCulture);
        string upperText = UpperUnbounded ? "+∞" : _last.ToString(null, CultureInfo.CurrentCulture);
        return $"{lowerBracket}{lowerText}, {upperText}{upperBracket}";
    }
}
