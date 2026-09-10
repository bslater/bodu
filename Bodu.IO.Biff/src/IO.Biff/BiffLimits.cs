// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffLimits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Defines the structural limits of the BIFF record format.
/// </summary>
/// <remarks>
/// The per-version maximum payload is enforced by <see cref="BiffWriter" /> and tolerated, not enforced, by <see
/// cref="BiffReader" />, which frames any length the 16-bit field can express. <see cref="DefaultCodePage" /> is
/// assumed for BIFF5 byte strings until a <c>CODEPAGE</c> record is read, and <see cref="UnicodeCodePage" /> is the
/// value a BIFF8 <c>CODEPAGE</c> record carries.
/// </remarks>
/// <seealso cref="BiffRecordHeader" /> <seealso cref="BiffWriter.MaxPayloadLength" />
public static class BiffLimits
{
    /// <summary>The size, in bytes, of a record header: a 16-bit identifier followed by a 16-bit payload length.</summary>
    public const int RecordHeaderSize = 4;

    /// <summary>The largest payload a BIFF5 record may declare, in bytes.</summary>
    public const int Biff5MaxPayloadLength = 2080;

    /// <summary>The largest payload a BIFF8 record may declare, in bytes.</summary>
    public const int Biff8MaxPayloadLength = 8224;

    /// <summary>The largest payload the 16-bit length field can express, in bytes. A reader tolerates declared lengths up to this value; a writer enforces the per-version maximum.</summary>
    public const int AbsoluteMaxPayloadLength = ushort.MaxValue;

    /// <summary>The default code page assumed for byte strings when no <c>CODEPAGE</c> record has been read (Windows-1252).</summary>
    public const int DefaultCodePage = 1252;

    /// <summary>The code page value a BIFF8 <c>CODEPAGE</c> record carries to declare UTF-16LE text.</summary>
    public const int UnicodeCodePage = 1200;

    /// <summary>
    /// Gets the largest payload a record may declare under the specified version, in bytes.
    /// </summary>
    /// <param name="version">The BIFF version.</param>
    /// <returns>
    /// <see cref="Biff5MaxPayloadLength" /> for <see cref="BiffVersion.Biff5" />, <see cref="Biff8MaxPayloadLength" />
    /// for <see cref="BiffVersion.Biff8" />, and <see cref="AbsoluteMaxPayloadLength" /> when the version is unknown.
    /// </returns>
    public static int GetMaxPayloadLength(BiffVersion version) =>
        version switch
        {
            BiffVersion.Biff5 => Biff5MaxPayloadLength,
            BiffVersion.Biff8 => Biff8MaxPayloadLength,
            _ => AbsoluteMaxPayloadLength,
        };
}
