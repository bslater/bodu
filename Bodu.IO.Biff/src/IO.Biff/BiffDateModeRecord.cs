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
/// <param name="Is1904">Whether the 1904 date system is in use.</param>
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
