// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffBoolErrRecord.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Represents a decoded <c>BOOLERR</c> record: a cell holding either a boolean or an error code.
/// </summary>
/// <param name="Row">The zero-based row index.</param>
/// <param name="Column">The zero-based column index.</param>
/// <param name="XfIndex">The extended-format index of the cell.</param>
/// <param name="RawValue">The value byte: zero or one for a boolean, the error code otherwise.</param>
/// <param name="IsError">Whether <paramref name="RawValue" /> is an error code rather than a boolean.</param>
public readonly record struct BiffBoolErrRecord(int Row, int Column, ushort XfIndex, byte RawValue, bool IsError)
{
    /// <summary>The payload length of the record.</summary>
    internal const int Length = 8;

    /// <summary>
    /// Gets a value indicating whether the cell holds <see langword="true" />.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the value byte is non-zero; meaningful only when <see cref="IsError" /> is
    /// <see langword="false" />.
    /// </value>
    public bool BooleanValue => RawValue != 0;

    /// <summary>
    /// Gets the error code of the cell.
    /// </summary>
    /// <value>
    /// The BIFF error code (<c>0x00</c> #NULL!, <c>0x07</c> #DIV/0!, <c>0x0F</c> #VALUE!, <c>0x17</c> #REF!,
    /// <c>0x1D</c> #NAME?, <c>0x24</c> #NUM!, <c>0x2A</c> #N/A); meaningful only when <see cref="IsError" /> is
    /// <see langword="true" />.
    /// </value>
    public byte ErrorCode => RawValue;

    /// <summary>
    /// Decodes a <c>BOOLERR</c> payload.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="BiffFormatException">Thrown when the payload is shorter than eight bytes.</exception>
    internal static BiffBoolErrRecord Read(ReadOnlySpan<byte> payload)
    {
        const BiffRecordType type = BiffRecordType.BoolErr;
        BiffPayload.RequireLength(payload, Length, type);

        return new BiffBoolErrRecord(
            BiffPayload.ReadUInt16(payload, 0, type),
            BiffPayload.ReadUInt16(payload, 2, type),
            BiffPayload.ReadUInt16(payload, 4, type),
            payload[6],
            payload[7] != 0);
    }
}
