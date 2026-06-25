// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbBinaryReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Compound.Internal;

/// <summary>
/// Provides a forward-only cursor that reads little-endian primitives from a byte span, used while parsing the
/// fixed-layout regions of a compound file (the header and the directory entries).
/// </summary>
/// <remarks>
/// The reader does not own or copy the underlying span; it advances an internal position as values are read and throws
/// <see cref="CompoundFileFormatException" /> when a read would run past the end of the data.
/// </remarks>
internal ref struct CfbBinaryReader
{
    /// <summary>The backing data the cursor reads from.</summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>The current read offset, in bytes, from the start of <see cref="_data" />.</summary>
    private int _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="CfbBinaryReader" /> struct over the supplied data.
    /// </summary>
    /// <param name="data">The byte span to read from.</param>
    public CfbBinaryReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }

    /// <summary>
    /// Gets the current read offset, in bytes, from the start of the data.
    /// </summary>
    /// <value>The current zero-based position.</value>
    public readonly int Position => _position;

    /// <summary>
    /// Gets the total number of bytes available to the reader.
    /// </summary>
    /// <value>The length of the backing data.</value>
    public readonly int Length => _data.Length;

    /// <summary>
    /// Moves the cursor to an absolute byte offset.
    /// </summary>
    /// <param name="position">The zero-based offset to seek to.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when <paramref name="position" /> is negative or beyond the end of the data.
    /// </exception>
    public void Seek(int position)
    {
        if ((uint)position > (uint)_data.Length)
            CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidHeader);

        _position = position;
    }

    /// <summary>
    /// Advances the cursor by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to skip.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the skip would run past the end of the data.
    /// </exception>
    public void Skip(int count) =>
        Seek(_position + count);

    /// <summary>
    /// Reads a 16-bit little-endian unsigned integer and advances the cursor by two bytes.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when fewer than two bytes remain.</exception>
    public ushort ReadUInt16() =>
        BinaryPrimitives.ReadUInt16LittleEndian(Take(sizeof(ushort)));

    /// <summary>
    /// Reads a 32-bit little-endian signed integer and advances the cursor by four bytes.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when fewer than four bytes remain.</exception>
    public int ReadInt32() =>
        BinaryPrimitives.ReadInt32LittleEndian(Take(sizeof(int)));

    /// <summary>
    /// Reads a 32-bit little-endian unsigned integer and advances the cursor by four bytes.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when fewer than four bytes remain.</exception>
    public uint ReadUInt32() =>
        BinaryPrimitives.ReadUInt32LittleEndian(Take(sizeof(uint)));

    /// <summary>
    /// Reads a 64-bit little-endian unsigned integer and advances the cursor by eight bytes.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="CompoundFileFormatException">Thrown when fewer than eight bytes remain.</exception>
    public ulong ReadUInt64() =>
        BinaryPrimitives.ReadUInt64LittleEndian(Take(sizeof(ulong)));

    /// <summary>
    /// Reads <paramref name="count" /> raw bytes and advances the cursor by the same amount.
    /// </summary>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>A span over the requested bytes within the backing data.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when fewer than <paramref name="count" /> bytes remain.
    /// </exception>
    public ReadOnlySpan<byte> ReadBytes(int count) =>
        Take(count);

    /// <summary>
    /// Slices <paramref name="count" /> bytes from the current position and advances the cursor.
    /// </summary>
    /// <param name="count">The number of bytes to consume.</param>
    /// <returns>A span over the consumed bytes.</returns>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when the read would run past the end of the data.
    /// </exception>
    private ReadOnlySpan<byte> Take(int count)
    {
        if (count < 0 || _position + count > _data.Length)
            CompoundThrowHelper.ThrowFormat(CompoundResourceStrings.Format_Invalid_CompoundHeader, CompoundFileError.InvalidHeader);

        ReadOnlySpan<byte> slice = _data.Slice(_position, count);
        _position += count;
        return slice;
    }
}
