// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Variant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Identifies the Base32 alphabet and decoding rules used by <see cref="Base32" />.
/// </summary>
/// <remarks>
/// Each variant differs in its alphabet, default casing, padding convention, and decoder leniency around visually
/// ambiguous characters. The encoder always emits the canonical case for the chosen variant; the decoder is
/// case-insensitive for every variant.
/// </remarks>
public enum Base32Variant : byte
{
    /// <summary>
    /// RFC 4648 §6 Base32 using the alphabet <c>A-Z</c> followed by <c>2-7</c>. Encoded output is padded with <c>=</c>
    /// characters to align to multiples of eight by default.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// RFC 4648 §7 base32hex (sometimes called "hex-extended") using the alphabet <c>0-9</c> followed by <c>A-V</c>.
    /// Used by NSEC3 DNSSEC labels. Encoded output is padded with <c>=</c> by default.
    /// </summary>
    HexExtended = 1,

    /// <summary>
    /// Crockford Base32 — alphabet <c>0-9</c> then <c>A-Z</c> with <c>I</c>, <c>L</c>, <c>O</c>, and <c>U</c> excluded
    /// to reduce visual ambiguity. The decoder aliases <c>I</c>/<c>L</c> to <c>1</c> and <c>O</c> to <c>0</c>. No
    /// padding is emitted by default.
    /// </summary>
    Crockford = 2,

    /// <summary>
    /// z-base-32 — a lowercase, human-oriented alphabet designed for spoken transmission. No padding is emitted by
    /// default.
    /// </summary>
    ZBase32 = 3,
}
