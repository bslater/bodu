// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Identifies the on-disk PST format variant, discriminated by the header's <c>wVer</c> field.
/// </summary>
public enum PstFileFormat
{
    /// <summary>
    /// The Unicode format (<c>wVer</c> 23) — 64-bit structures, the default since Outlook 2003.
    /// </summary>
    Unicode = 0,

    /// <summary>
    /// The legacy ANSI format (<c>wVer</c> 14 or 15) — 32-bit structures. Read with 32-bit block identifiers and offsets; strings are typically code-page (<c>PT_STRING8</c>) values.
    /// </summary>
    Ansi = 1,

    /// <summary>
    /// The 4&#160;KiB-page OST variant (<c>wVer</c> 36 and above). Read with 32-bit block identifiers and offsets; strings are typically code-page (<c>PT_STRING8</c>) values.
    /// </summary>
    Ost4K = 2,
}
