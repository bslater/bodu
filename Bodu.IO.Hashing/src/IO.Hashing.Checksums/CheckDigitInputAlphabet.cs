// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitInputAlphabet.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Identifies the alphabet from which a check-digit or check-character algorithm accepts its body input.
/// </summary>
/// <remarks>
/// Consumed by <see cref="AlphanumericCheckDigitAlgorithm" /> and <see cref="MultiCharCheckDigitAlgorithm" /> so that
/// test harnesses and diagnostic surfaces can reason about the valid input set without inspecting the concrete
/// algorithm.
/// </remarks>
public enum CheckDigitInputAlphabet
{
    /// <summary>
    /// ASCII decimal digits only (<c>'0'</c> to <c>'9'</c>).
    /// </summary>
    DecimalDigits,

    /// <summary>
    /// ASCII decimal digits (<c>'0'</c> to <c>'9'</c>) and uppercase Latin letters (<c>'A'</c> to <c>'Z'</c>).
    /// </summary>
    AlphanumericUppercase,
}
