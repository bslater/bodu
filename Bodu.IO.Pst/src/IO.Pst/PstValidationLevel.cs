// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstValidationLevel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Specifies how strictly a file's structures and checksums are validated while reading.
/// </summary>
public enum PstValidationLevel
{
    /// <summary>
    /// Structural validation plus the header checksum; page and block checksums and signatures are skipped. The
    /// default, tolerating the deviations of real-world writers.
    /// </summary>
    Compatible = 0,

    /// <summary>
    /// Everything <see cref="Compatible" /> checks, plus the checks the tolerant levels skip.
    /// </summary>
    /// <remarks>
    /// Under this level the reader also rejects a page or block whose checksum, trailer signature, or recorded block
    /// identifier disagrees; a header whose declared file length lies beyond the end of the stream; a property-context
    /// record with a wire type the reader does not know; BTree-on-heap keys out of order, property-context keys among
    /// them; and a table whose row matrix holds more rows than its row index records.
    /// </remarks>
    Strict = 1,

    /// <summary>
    /// Structural validation only; every checksum is skipped, the header checksum included.
    /// </summary>
    /// <remarks>
    /// This level differs from <see cref="Compatible" /> only in skipping the header's partial checksum: both tolerant
    /// levels skip page and block checksums and apply the same structural checks, so a damaged file reads no further
    /// under this level unless its header checksum is what fails. To recover what a damaged file still holds, set
    /// <see cref="PstFileOptions.OmitUnresolvableValues" />, which leaves out a value whose reference does not resolve
    /// instead of failing its context.
    /// </remarks>
    Minimal = 2,
}
