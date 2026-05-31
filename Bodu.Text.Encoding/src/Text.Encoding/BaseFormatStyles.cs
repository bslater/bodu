// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BaseFormatStyles.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Specifies formatting styles that influence how base-encoded input strings are parsed during decoding.
/// </summary>
/// <remarks>
/// These options control the leniency of the decoder. Multiple flags may be combined using a bitwise OR. The default of
/// <see cref="None" /> requires strict, decoration-free input.
/// </remarks>
[Flags]
public enum BaseFormatStyles : byte
{
    /// <summary>
    /// Indicates strict parsing mode. The input must contain only valid alphabet characters and must not include any
    /// prefix or whitespace decoration.
    /// </summary>
    None = 0,

    /// <summary>
    /// Allows the parser to accept and ignore an optional <c>0x</c> or <c>0X</c> prefix at the beginning of the input.
    /// </summary>
    AllowPrefix = 1 << 0,

    /// <summary>
    /// Allows the parser to ignore ASCII whitespace characters ( <c>' '</c>, <c>'\t'</c>, <c>'\r'</c>, <c>'\n'</c>)
    /// anywhere in the input.
    /// </summary>
    IgnoreWhitespace = 1 << 1,

    /// <summary>
    /// Allows the parser to accept encoded input that omits the trailing padding characters mandated by the encoding
    /// specification. This flag has no effect on encodings that do not use padding.
    /// </summary>
    AllowMissingPadding = 1 << 2,

    /// <summary>
    /// Requires that the input is in canonical form — the unused bits of the final partial symbol must be zero. This
    /// prevents encoding ambiguity where multiple distinct input strings would decode to the same byte sequence (for
    /// example, RFC 4648 §3.5 explicitly allows decoders to either reject or normalise such inputs).
    /// </summary>
    /// <remarks>
    /// Applies to <see cref="Base32" /> and <see cref="Base64" /> only. The flag is a no-op for <see cref="Base16" />
    /// (which has no unused bits per character), <see cref="Base58" /> (a non-power-of-two radix where canonicity is
    /// determined by leading-zero preservation), and <see cref="Base85" /> (which uses block-based encoding with a
    /// different tail convention).
    /// </remarks>
    RequireCanonicalEncoding = 1 << 3,
}
