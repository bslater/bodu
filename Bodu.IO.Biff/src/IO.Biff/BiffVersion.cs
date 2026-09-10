// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffVersion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the version of the Binary Interchange File Format (BIFF) a record stream is encoded in.
/// </summary>
/// <remarks>
/// The version governs how several records lay out their fields — string representation, the width of row indices in
/// <c>DIMENSIONS</c>, the presence of the shared string table — and the maximum payload a single record may carry. A
/// reader establishes the version from the first beginning-of-file record it encounters; a writer is created with the
/// version it emits.
/// </remarks>
public enum BiffVersion
{
    /// <summary>
    /// The version has not been established; no beginning-of-file record has been read and none was supplied.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// BIFF5, written by Excel 5.0 and Excel 95 (BIFF7 shares the same layouts and is treated as BIFF5). Strings are
    /// code-page byte strings and a record payload is at most 2,080 bytes.
    /// </summary>
    Biff5 = 5,

    /// <summary>
    /// BIFF8, written by Excel 97 through Excel 2003. Strings are Unicode (16-bit or compressed 8-bit) with option
    /// flags, cell text is pooled in the shared string table, and a record payload is at most 8,224 bytes.
    /// </summary>
    Biff8 = 8,
}
