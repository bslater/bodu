// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Defines the customizations a <see cref="BiffWriter" /> is created with.
/// </summary>
/// <remarks>
/// The version selects every layout the writer emits and the maximum payload it enforces; the code page applies only to
/// BIFF5 text and is normalized like a <c>CODEPAGE</c> record value, so the format's private markers are accepted.
/// <see cref="BiffReaderOptions" /> is the reading counterpart.
/// </remarks>
/// <seealso cref="BiffWriter" /> <seealso cref="BiffReaderOptions" /> <seealso cref="BiffVersion" />
public readonly struct BiffWriterOptions
{
    /// <summary>
    /// Gets the BIFF version the writer emits.
    /// </summary>
    /// <value>
    /// <see cref="BiffVersion.Biff5" /> or <see cref="BiffVersion.Biff8" />; the writer rejects
    /// <see cref="BiffVersion.Unknown" />.
    /// </value>
    public BiffVersion Version { get; init; }

    /// <summary>
    /// Gets the code page used to encode text under <see cref="BiffVersion.Biff5" />.
    /// </summary>
    /// <value>
    /// The Windows code page number, or zero (the default) to use <see cref="BiffLimits.DefaultCodePage" />. Ignored
    /// under <see cref="BiffVersion.Biff8" />, where text is written as Unicode.
    /// </value>
    public int CodePage { get; init; }
}
