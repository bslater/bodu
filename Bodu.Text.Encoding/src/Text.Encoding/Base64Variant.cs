// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Variant.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Identifies the Base64 alphabet and formatting rules used by <see cref="Base64" />.
/// </summary>
/// <remarks>
/// All variants share the same byte-to-symbol mapping at the bit level; they differ only in alphabet substitutions and
/// the default behaviour around padding and line breaks.
/// </remarks>
public enum Base64Variant : byte
{
    /// <summary>
    /// RFC 4648 §4 standard Base64 using the alphabet <c>A-Z a-z 0-9 + /</c>. Encoded output is padded with <c>=</c> to
    /// a multiple of four characters by default.
    /// </summary>
    Standard = 0,

    /// <summary>
    /// RFC 4648 §5 URL- and filename-safe Base64 using the alphabet <c>A-Z a-z 0-9 - _</c>. Padding is omitted by
    /// default, matching the conventions used in JWT, OAuth tokens, and most URL contexts.
    /// </summary>
    UrlSafe = 1,

    /// <summary>
    /// RFC 2045 MIME Base64 using the standard alphabet with mandatory line breaks every 76 characters.
    /// </summary>
    Mime = 2,
}
