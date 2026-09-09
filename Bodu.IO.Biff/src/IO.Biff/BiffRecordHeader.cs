// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRecordHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Biff;

/// <summary>
/// Represents the four-byte header that precedes every BIFF record: a 16-bit little-endian record identifier followed
/// by the 16-bit little-endian length of the payload that follows.
/// </summary>
/// <param name="Id">The 16-bit record identifier.</param>
/// <param name="Length">The declared payload length, in bytes.</param>
public readonly record struct BiffRecordHeader(ushort Id, ushort Length)
{
    /// <summary>
    /// Gets the record identifier as a <see cref="BiffRecordType" />.
    /// </summary>
    /// <value>
    /// The typed identifier. The value may not correspond to a defined member when the record is one the codec does not
    /// name.
    /// </value>
    public BiffRecordType Type => (BiffRecordType)Id;

    /// <summary>
    /// Attempts to parse a record header from the start of the supplied bytes.
    /// </summary>
    /// <param name="source">The bytes to parse.</param>
    /// <param name="header">When this method returns, the parsed header when one was available.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="source" /> holds at least
    /// <see cref="BiffLimits.RecordHeaderSize" /> bytes; otherwise <see langword="false" />.
    /// </returns>
    public static bool TryParse(ReadOnlySpan<byte> source, out BiffRecordHeader header)
    {
        if (source.Length < BiffLimits.RecordHeaderSize)
        {
            header = default;
            return false;
        }

        header = new BiffRecordHeader(
            BinaryPrimitives.ReadUInt16LittleEndian(source),
            BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(2)));
        return true;
    }

    /// <summary>
    /// Writes the header to the start of the supplied buffer.
    /// </summary>
    /// <param name="destination">The buffer that receives the four header bytes.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is shorter than <see cref="BiffLimits.RecordHeaderSize" />.
    /// </exception>
    public void WriteTo(Span<byte> destination)
    {
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, BiffLimits.RecordHeaderSize);

        BinaryPrimitives.WriteUInt16LittleEndian(destination, Id);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(2), Length);
    }
}
