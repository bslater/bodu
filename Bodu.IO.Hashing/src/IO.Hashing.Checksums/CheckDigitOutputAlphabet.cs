// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitOutputAlphabet.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Identifies the alphabet from which a single-character check-digit algorithm may emit its check character.
/// </summary>
/// <remarks>
/// Consumed by <see cref="AlphanumericCheckDigitAlgorithm" />. Multi-character algorithms defined via
/// <see cref="MultiCharCheckDigitAlgorithm" /> emit decimal digits only and therefore do not use this enumeration.
/// </remarks>
public enum CheckDigitOutputAlphabet
{
    /// <summary>
    /// ASCII decimal digits only (<c>'0'</c> to <c>'9'</c>).
    /// </summary>
    DecimalDigits,

    /// <summary>
    /// ASCII decimal digits (<c>'0'</c> to <c>'9'</c>) plus the sentinel <c>'X'</c> used to represent the check value
    /// ten in schemes such as ISBN-10 and ISO 7064 MOD 11-2.
    /// </summary>
    DecimalDigitsOrX,

    /// <summary>
    /// The full forty-three-symbol Code 39 alphabet: the decimal digits, the uppercase Latin letters, and the symbols
    /// <c>'-'</c>, <c>'.'</c>, space, <c>'$'</c>, <c>'/'</c>, <c>'+'</c>, and <c>'%'</c>.
    /// </summary>
    Code39,

    /// <summary>
    /// The thirty-seven-symbol Crockford Base32 check alphabet: the thirty-two encoding symbols plus the five
    /// check-only symbols <c>'*'</c> (32), <c>'~'</c> (33), <c>'$'</c> (34), <c>'='</c> (35), and <c>'U'</c> (36).
    /// </summary>
    CrockfordBase32Check,
}
