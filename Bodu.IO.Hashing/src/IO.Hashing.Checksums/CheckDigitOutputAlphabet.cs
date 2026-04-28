// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitOutputAlphabet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    /// ASCII decimal digits (<c>'0'</c> to <c>'9'</c>) plus the sentinel <c>'X'</c> used to represent the
    /// check value ten in schemes such as ISBN-10 and ISO 7064 MOD 11-2.
    /// </summary>
    DecimalDigitsOrX,
}
