// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffPayload.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Biff;

/// <summary>
/// Provides bounds-checked reads over a BIFF record payload, raising <see cref="BiffFormatException" /> rather than an
/// indexing or argument exception when a malformed record is too short for the field being read.
/// </summary>
/// <remarks>
/// BIFF data is routinely encountered from untrusted or legacy sources. Every fixed-layout field read by the typed
/// record decoders flows through these helpers so a truncated payload fails with a single, domain-specific exception
/// type that callers can catch deterministically.
/// </remarks>
internal static class BiffPayload
{
    /// <summary>
    /// Ensures a payload is at least <paramref name="required" /> bytes long.
    /// </summary>
    /// <param name="payload">The record payload.</param>
    /// <param name="required">The minimum number of bytes the payload must contain.</param>
    /// <param name="type">The record type, used to compose the failure message.</param>
    /// <exception cref="BiffFormatException">
    /// Thrown when <paramref name="payload" /> is shorter than <paramref name="required" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RequireLength(ReadOnlySpan<byte> payload, int required, BiffRecordType type)
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
    /// <exception cref="BiffFormatException">Thrown when the payload is too short.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte ReadByte(ReadOnlySpan<byte> payload, int offset, BiffRecordType type)
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
    /// <exception cref="BiffFormatException">Thrown when the payload is too short.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort ReadUInt16(ReadOnlySpan<byte> payload, int offset, BiffRecordType type)
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
    /// <exception cref="BiffFormatException">Thrown when the payload is too short.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReadUInt32(ReadOnlySpan<byte> payload, int offset, BiffRecordType type)
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
    /// <exception cref="BiffFormatException">Thrown when the payload is too short.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadDouble(ReadOnlySpan<byte> payload, int offset, BiffRecordType type)
    {
        if (offset < 0 || offset + 8 > payload.Length)
            throw Malformed(type);

        return BinaryPrimitives.ReadDoubleLittleEndian(payload.Slice(offset));
    }

    /// <summary>
    /// Creates the malformed-record exception for a record type.
    /// </summary>
    /// <param name="type">The record type whose payload was too short or malformed.</param>
    /// <returns>A <see cref="BiffFormatException" /> naming the record type.</returns>
    public static BiffFormatException Malformed(BiffRecordType type) =>
        new(string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Format_Invalid_BiffRecordPayload, type));
}
