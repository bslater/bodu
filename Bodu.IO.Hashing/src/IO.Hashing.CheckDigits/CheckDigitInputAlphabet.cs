// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitInputAlphabet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

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

    /// <summary>
    /// The Code 39 alphabet: ASCII decimal digits (<c>'0'</c> to <c>'9'</c>), uppercase Latin letters (<c>'A'</c> to
    /// <c>'Z'</c>), and the seven symbols <c>'-'</c>, <c>'.'</c>, space, <c>'$'</c>, <c>'/'</c>, <c>'+'</c>, and
    /// <c>'%'</c>.
    /// </summary>
    Code39,

    /// <summary>
    /// The Crockford Base32 alphabet: the thirty-two encoding symbols (<c>'0'</c>–<c>'9'</c> and <c>'A'</c>–<c>'Z'</c>
    /// excluding <c>'I'</c>, <c>'L'</c>, <c>'O'</c>, and <c>'U'</c>). Decoding is case-insensitive and accepts the
    /// aliases <c>'I'</c>/<c>'L'</c> for <c>1</c> and <c>'O'</c> for <c>0</c>.
    /// </summary>
    CrockfordBase32,
}
