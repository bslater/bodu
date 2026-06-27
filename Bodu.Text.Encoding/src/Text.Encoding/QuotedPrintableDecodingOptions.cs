// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableDecodingOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Specifies relaxations applied by the <see cref="QuotedPrintable" /> decoder over its strict RFC 2045 default.
/// </summary>
[Flags]
public enum QuotedPrintableDecodingOptions : byte
{
    /// <summary>
    /// Strict RFC 2045 decoding: only uppercase hex escapes, no bare line feeds, and trailing literal whitespace at a
    /// line end is rejected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Accepts lowercase hexadecimal digits (<c>a</c>–<c>f</c>) in <c>=HH</c> escapes in addition to uppercase.
    /// </summary>
    AllowLowercaseHex = 1 << 0,

    /// <summary>
    /// Deletes trailing literal space and tab characters at the end of an encoded line instead of rejecting them, as
    /// RFC 2045 permits decoders to do for transport-inserted whitespace.
    /// </summary>
    IgnoreTrailingWhitespace = 1 << 1,

    /// <summary>
    /// Accepts a bare LF (<c>0x0A</c>) as both a hard line break and, after <c>=</c>, a soft line break — in addition
    /// to canonical CRLF.
    /// </summary>
    AllowBareLineFeed = 1 << 2,
}
