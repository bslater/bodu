// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffCodePageRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>CODEPAGE</c> record: the code page byte strings in the stream are encoded in.
/// </summary>
/// <param name="RawValue">
/// The value as stored, including the private markers for Apple Roman (<c>0x8000</c>) and the legacy ANSI page (<c>0x8001</c>).
/// </param>
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
