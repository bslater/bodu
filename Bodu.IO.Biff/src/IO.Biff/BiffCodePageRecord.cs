// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffCodePageRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>CODEPAGE</c> record: the code page byte strings in the stream are encoded in.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.CodePage" />. The payload is a single 16-bit value in both
/// versions. Reading the record also updates <see cref="BiffReader.CodePage" />, so the byte strings that follow decode
/// with it; <see cref="CodePage" /> exposes the normalized Windows code page number.
/// </remarks>
/// <param name="RawValue">
/// The value as stored, including the private markers for Apple Roman (<c>0x8000</c>) and the legacy ANSI page (<c>0x8001</c>).
/// </param>
/// <seealso cref="BiffReader.GetCodePage" /> <seealso cref="BiffReader.CodePage" />
/// <seealso cref="BiffWriter.WriteCodePage(ushort)" /> <seealso cref="BiffReaderOptions.CodePage" />
public readonly record struct BiffCodePageRecord(ushort RawValue)
{
    /// <summary>The payload length of the record.</summary>
    internal const int Length = 2;

    /// <summary>
    /// Gets the Windows code page number the value denotes.
    /// </summary>
    /// <value>The normalized code page; 1200 denotes UTF-16LE text in a BIFF8 stream.</value>
    public int CodePage => BiffTextEncoding.Normalize(RawValue);

    /// <summary>
    /// Decodes a <c>CODEPAGE</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than two bytes.</exception>
    internal static BiffCodePageRecord Read(ReadOnlySpan<byte> payload) =>
        new(BiffPayload.ReadUInt16(payload, 0, BiffRecordType.CodePage));
}
