// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffDateModeRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>DATEMODE</c> record: whether serial dates in the workbook count from 1 January 1904 rather
/// than from the 1900 date system.
/// </summary>
/// <remarks>
/// The record identifier is <see cref="BiffRecordType.DateMode" />. The payload is a single 16-bit value in both
/// versions; any non-zero value selects the 1904 system.
/// </remarks>
/// <param name="Is1904">Whether the 1904 date system is in use.</param>
/// <seealso cref="BiffReader.GetDateMode" /> <seealso cref="BiffWriter.WriteDateMode(bool)" />
public readonly record struct BiffDateModeRecord(bool Is1904)
{
    /// <summary>The payload length of the record.</summary>
    internal const int Length = 2;

    /// <summary>
    /// Decodes a <c>DATEMODE</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than two bytes.</exception>
    internal static BiffDateModeRecord Read(ReadOnlySpan<byte> payload) =>
        new(BiffPayload.ReadUInt16(payload, 0, BiffRecordType.DateMode) != 0);
}
