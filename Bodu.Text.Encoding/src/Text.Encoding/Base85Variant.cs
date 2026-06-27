// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Variant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Identifies the Base85 alphabet and framing rules used by <see cref="Base85" />.
/// </summary>
/// <remarks>
/// Base85 encodes four bytes as five characters using radix 85. Variants differ in alphabet, alphabet ordering, the
/// availability of the all-zero shortcut <c>z</c>, and whether partial trailing groups are permitted.
/// </remarks>
public enum Base85Variant : byte
{
    /// <summary>
    /// The Adobe Ascii85 alphabet, used by PDF and PostScript. Uses ASCII characters <c>!</c> (0x21) through <c>u</c>
    /// (0x75). The character <c>z</c> is recognised as a shortcut for four zero bytes. Partial trailing groups of one,
    /// two, or three bytes are permitted; output length grows by one character per byte plus one.
    /// </summary>
    Ascii85 = 0,

    /// <summary>
    /// The Z85 alphabet defined by ZeroMQ (RFC 32 / "ZMTP/2.0"). Uses a shell-safe alphabet that avoids quote, slash,
    /// semicolon, and backslash. No <c>z</c> shortcut is recognised, and the input length must be a multiple of four
    /// bytes (output length is then a multiple of five characters).
    /// </summary>
    Z85 = 1,

    /// <summary>
    /// The Git binary-patch Base85 alphabet with compact, self-delimiting partial-tail framing.
    /// </summary>
    /// <remarks>
    /// Uses the alphabet <c>0-9 A-Z a-z ! # $ % &amp; ( ) * + - ; &lt; = &gt; ? @ ^ _ ` { | } ~</c>. It does not use
    /// the Adobe <c>z</c> zero shortcut and does not use the Adobe <c>&lt;~</c> / <c>~&gt;</c> delimiters.
    /// <see cref="Base85" /> emits a compact, self-delimiting partial tail (a final remainder of one, two, or three
    /// bytes becomes two, three, or four characters), which round-trips through
    /// <see cref="Base85.Decode(System.ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> without external
    /// metadata — this is <b>not</b> the exact Git binary-patch line encoding, which always emits five characters per
    /// group and carries the decoded byte count out of band. Use the <c>EncodeGitPadded</c> / <c>DecodeGitPadded</c>
    /// helpers for that patch-compatible primitive. This variant implements only the Git alphabet; it does not parse
    /// Git binary patches.
    /// </remarks>
    GitCompact = 2,
}
