// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8Payload.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Formats.Excel.Binary.Biff8;

/// <summary>
/// Provides bounds-checked reads over a BIFF record payload, raising <see cref="ExcelBinaryFormatException" /> rather
/// than a generic indexing or argument exception when a malformed record is too short for the field being read.
/// </summary>
/// <remarks>
/// A BIFF8 workbook is a binary format routinely encountered from untrusted or legacy sources. Every fixed-layout field
/// read flows through these helpers so a truncated payload fails with a single, domain-specific exception type that
/// callers can catch deterministically.
/// </remarks>
internal static class Biff8Payload
{
    /// <summary>
    /// Ensures a payload is at least <paramref name="required" /> bytes long.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="required">The minimum number of bytes the payload must contain.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <exception cref="ExcelBinaryFormatException">
    /// Thrown when <paramref name="payload" /> is shorter than <paramref name="required" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequireLength(ReadOnlySpan<byte> payload, int required, Biff8RecordType type)
    {
        if (payload.Length < required)
            throw Malformed(type);
    }

    /// <summary>
    /// Reads a single byte at the specified offset.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset to read.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The byte at <paramref name="offset" />.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the payload is too short.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(ReadOnlySpan<byte> payload, int offset, Biff8RecordType type)
    {
        if ((uint)offset >= (uint)payload.Length)
            throw Malformed(type);

        return payload[offset];
    }

    /// <summary>
    /// Reads a little-endian unsigned 16-bit integer at the specified offset.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset to read.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the payload is too short.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16(ReadOnlySpan<byte> payload, int offset, Biff8RecordType type)
    {
        if (offset < 0 || offset + 2 > payload.Length)
            throw Malformed(type);

        return BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(offset));
    }

    /// <summary>
    /// Reads a little-endian unsigned 32-bit integer at the specified offset.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset to read.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the payload is too short.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32(ReadOnlySpan<byte> payload, int offset, Biff8RecordType type)
    {
        if (offset < 0 || offset + 4 > payload.Length)
            throw Malformed(type);

        return BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(offset));
    }

    /// <summary>
    /// Reads a little-endian IEEE 754 double at the specified offset.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="offset">The byte offset to read.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ExcelBinaryFormatException">Thrown when the payload is too short.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadDouble(ReadOnlySpan<byte> payload, int offset, Biff8RecordType type)
    {
        if (offset < 0 || offset + 8 > payload.Length)
            throw Malformed(type);

        return BinaryPrimitives.ReadDoubleLittleEndian(payload.Slice(offset));
    }

    /// <summary>
    /// Creates the malformed-record exception for a record type.
    /// </summary>
    /// <param name="type">The record type whose payload was too short.</param>
    /// <returns>An <see cref="ExcelBinaryFormatException" /> naming the record type.</returns>
    public static ExcelBinaryFormatException Malformed(Biff8RecordType type) =>
        new(string.Format(CultureInfo.CurrentCulture, ExcelBinaryResourceStrings.Format_Invalid_Biff8RecordPayload, type));
}
