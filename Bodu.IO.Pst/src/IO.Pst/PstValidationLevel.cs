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
    /// Everything <see cref="Compatible" /> checks, plus the checksum and signature of every page and block read.
    /// </summary>
    Strict = 1,

    /// <summary>
    /// Structural validation only; all checksums are skipped. For salvage reads of damaged files.
    /// </summary>
    Minimal = 2,
}
