// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableEncodingMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Identifies how <see cref="QuotedPrintable" /> treats line breaks in the source when encoding.
/// </summary>
public enum QuotedPrintableEncodingMode : byte
{
    /// <summary>
    /// Treats every source byte as arbitrary data. No byte sequence is interpreted as a hard line break; in particular
    /// CR (<c>0x0D</c>) and LF (<c>0x0A</c>) are escaped as <c>=0D</c> and <c>=0A</c>. This is the safest mode for
    /// round-tripping arbitrary binary payloads.
    /// </summary>
    Binary = 0,

    /// <summary>
    /// Recognises a canonical CRLF (<c>0x0D 0x0A</c>) pair in the source as a hard line break, emitted using the
    /// configured newline. A lone CR or lone LF is not a canonical break and is escaped as <c>=0D</c> or <c>=0A</c>.
    /// </summary>
    Text = 1,
}
