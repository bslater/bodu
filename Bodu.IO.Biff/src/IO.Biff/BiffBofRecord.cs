// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffBofRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>BOF</c> record: the version marker, the kind of substream being opened, and the build
/// information of the application that wrote it.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.Bof" />. The payload is eight bytes in BIFF5 (version, substream
/// type, build, year) and sixteen in BIFF8, which adds the file-history flags and the lowest saving version. The reader
/// tolerates any payload of at least four bytes, reporting zero for fields the record omits, and establishes
/// <see cref="BiffReader.Version" /> from the version marker when the record is read.
/// </remarks>
/// <param name="RawVersion">The <c>vers</c> field: <c>0x0500</c> for BIFF5, <c>0x0600</c> for BIFF8.</param>
/// <param name="SubstreamType">The kind of substream the record opens.</param>
/// <param name="Build">The build identifier of the writing application.</param>
/// <param name="Year">The build year of the writing application.</param>
/// <param name="FileHistoryFlags">The BIFF8 file-history flags; zero under BIFF5.</param>
/// <param name="LowestSaveVersion">The lowest BIFF version that has saved the file (BIFF8); zero under BIFF5.</param>
/// <seealso cref="BiffReader.GetBof" /> <seealso cref="BiffReader.Version" />
/// <seealso cref="BiffWriter.WriteBof(BiffSubstreamType, ushort, ushort)" /> <seealso cref="BiffSubstreamType" />
/// <seealso cref="BiffVersion" />
public readonly record struct BiffBofRecord(
    ushort RawVersion,
    BiffSubstreamType SubstreamType,
    ushort Build,
    ushort Year,
    uint FileHistoryFlags,
    uint LowestSaveVersion)
{
    /// <summary>The payload length of a BIFF5 record.</summary>
    internal const int Biff5Length = 8;

    /// <summary>The payload length of a BIFF8 record.</summary>
    internal const int Biff8Length = 16;

    /// <summary>
    /// Gets the BIFF version the marker denotes.
    /// </summary>
    /// <value>
    /// <see cref="BiffVersion.Biff5" />, <see cref="BiffVersion.Biff8" />, or <see cref="BiffVersion.Unknown" /> for
    /// any other marker.
    /// </value>
    public BiffVersion Version =>
        RawVersion switch
        {
            0x0500 => BiffVersion.Biff5,
            0x0600 => BiffVersion.Biff8,
            _ => BiffVersion.Unknown,
        };

    /// <summary>
    /// Decodes a <c>BOF</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than four bytes.</exception>
    internal static BiffBofRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.Bof;
        BiffPayload.RequireLength(payload, 4, type);

        ushort version = BiffPayload.ReadUInt16(payload, 0, type);
        var substream = (BiffSubstreamType)BiffPayload.ReadUInt16(payload, 2, type);
        ushort build = payload.Length >= 6 ? BiffPayload.ReadUInt16(payload, 4, type) : (ushort)0;
        ushort year = payload.Length >= 8 ? BiffPayload.ReadUInt16(payload, 6, type) : (ushort)0;
        uint history = payload.Length >= 12 ? BiffPayload.ReadUInt32(payload, 8, type) : 0u;
        uint lowest = payload.Length >= 16 ? BiffPayload.ReadUInt32(payload, 12, type) : 0u;

        return new BiffBofRecord(version, substream, build, year, history, lowest);
    }
}
