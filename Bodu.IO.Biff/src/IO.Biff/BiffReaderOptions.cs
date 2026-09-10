// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Defines the customizations a <see cref="BiffReader" /> is created with.
/// </summary>
/// <remarks>
/// Both options seed state the reader would otherwise establish from the stream itself. They exist for the case where
/// the surrounding context already knows the answer — for example a sheet substream read after the workbook globals
/// have fixed the version and code page — so the reader need not see a <c>BOF</c> or <c>CODEPAGE</c> record first.
/// </remarks>
public readonly struct BiffReaderOptions
{
    /// <summary>
    /// Gets the BIFF version the stream is known to be encoded in.
    /// </summary>
    /// <value>
    /// The version, or <see cref="BiffVersion.Unknown" /> (the default) to let the reader establish it from the first
    /// <c>BOF</c> record. A <c>BOF</c> record that disagrees with a supplied version is rejected as malformed.
    /// </value>
    public BiffVersion Version { get; init; }

    /// <summary>
    /// Gets the code page byte strings are known to be encoded in.
    /// </summary>
    /// <value>
    /// The Windows code page number, or zero (the default) to assume <see cref="BiffLimits.DefaultCodePage" /> until a
    /// <c>CODEPAGE</c> record is read.
    /// </value>
    public int CodePage { get; init; }
}
