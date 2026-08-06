// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CfbBinaryReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the little-endian cursor the header parser reads through.
/// </summary>
/// <remarks>
/// Every read is bounds-checked against the backing span and reports a malformed header rather than an index
/// exception, because the cursor's only caller parses untrusted bytes. The reader is a <c>ref struct</c>, so these
/// tests construct it directly rather than reaching it through a container.
/// </remarks>
[TestClass]
public class CfbBinaryReaderTests
{
    /// <summary>
    /// Builds a 32-byte buffer whose bytes ascend from zero, so a decoded value identifies the offset it came from.
    /// </summary>
    /// <returns>The test buffer.</returns>
    private static byte[] Ascending()
    {
        byte[] data = new byte[32];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)i;

        return data;
    }

    /// <summary>
    /// Verifies that a new reader starts at offset zero and reports the length of the data it was given.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldStartAtZeroWithFullLength()
    {
        var reader = new CfbBinaryReader(Ascending());

        Assert.AreEqual(0, reader.Position);
        Assert.AreEqual(32, reader.Length);
    }

    /// <summary>
    /// Verifies that each fixed-width read decodes little-endian and advances the cursor by its own width, so
    /// consecutive reads tile the buffer without gaps or overlap.
    /// </summary>
    [TestMethod]
    public void Reads_WhenCalledInSequence_ShouldDecodeLittleEndianAndAdvanceByWidth()
    {
        byte[] data = Ascending();
        var reader = new CfbBinaryReader(data);

        Assert.AreEqual(BinaryPrimitives.ReadUInt16LittleEndian(data), reader.ReadUInt16());
        Assert.AreEqual(2, reader.Position);

        Assert.AreEqual(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(2)), reader.ReadInt32());
        Assert.AreEqual(6, reader.Position);

        Assert.AreEqual(BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(6)), reader.ReadUInt32());
        Assert.AreEqual(10, reader.Position);

        Assert.AreEqual(BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(10)), reader.ReadUInt64());
        Assert.AreEqual(18, reader.Position);
    }

    /// <summary>
    /// Verifies that <c>ReadBytes</c> returns exactly the requested slice and advances past it.
    /// </summary>
    [TestMethod]
    public void ReadBytes_WhenCountRequested_ShouldReturnThatSliceAndAdvance()
    {
        var reader = new CfbBinaryReader(Ascending());
        reader.Seek(4);

        ReadOnlySpan<byte> slice = reader.ReadBytes(3);

        Assert.AreEqual(3, slice.Length);
        Assert.AreEqual(4, slice[0]);
        Assert.AreEqual(6, slice[2]);
        Assert.AreEqual(7, reader.Position);
    }

    /// <summary>
    /// Verifies that seeking to the end is permitted - it is the position a fully consumed reader holds - while
    /// seeking past it is rejected.
    /// </summary>
    [TestMethod]
    public void Seek_WhenTargetIsTheEnd_ShouldSucceedButPastItShouldThrow()
    {
        byte[] data = Ascending();
        var reader = new CfbBinaryReader(data);

        reader.Seek(data.Length);
        Assert.AreEqual(data.Length, reader.Position);

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            var inner = new CfbBinaryReader(data);
            inner.Seek(data.Length + 1);
        });
    }

    /// <summary>
    /// Verifies that a negative seek is rejected. The bound is tested as an unsigned comparison, so a negative offset
    /// must not wrap past the length check.
    /// </summary>
    [TestMethod]
    public void Seek_WhenTargetIsNegative_ShouldThrowExactly()
    {
        byte[] data = Ascending();

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            var reader = new CfbBinaryReader(data);
            reader.Seek(-1);
        });
    }

    /// <summary>
    /// Verifies that <c>Skip</c> advances relative to the current position rather than from the start.
    /// </summary>
    [TestMethod]
    public void Skip_WhenCalled_ShouldAdvanceRelativeToPosition()
    {
        var reader = new CfbBinaryReader(Ascending());
        reader.Seek(4);

        reader.Skip(6);

        Assert.AreEqual(10, reader.Position);
        Assert.AreEqual(BinaryPrimitives.ReadUInt16LittleEndian(Ascending().AsSpan(10)), reader.ReadUInt16());
    }

    /// <summary>
    /// Verifies that a skip past the end is rejected, since it resolves to an out-of-range seek.
    /// </summary>
    [TestMethod]
    public void Skip_WhenPastEnd_ShouldThrowExactly()
    {
        byte[] data = Ascending();

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            var reader = new CfbBinaryReader(data);
            reader.Skip(data.Length + 1);
        });
    }

    /// <summary>
    /// Verifies that a read running past the end reports a malformed header rather than an index exception, whichever
    /// width was asked for.
    /// </summary>
    [TestMethod]
    public void Reads_WhenFewerBytesRemainThanRequested_ShouldThrowExactly()
    {
        byte[] data = new byte[4];

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            var reader = new CfbBinaryReader(data);
            _ = reader.ReadUInt64();
        });

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            var reader = new CfbBinaryReader(data);
            reader.Seek(3);
            _ = reader.ReadUInt32();
        });

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            var reader = new CfbBinaryReader(data);
            reader.Seek(3);
            _ = reader.ReadUInt16();
        });
    }

    /// <summary>
    /// Verifies that a negative byte count is rejected rather than treated as a zero-length read.
    /// </summary>
    [TestMethod]
    public void ReadBytes_WhenCountIsNegative_ShouldThrowExactly()
    {
        byte[] data = Ascending();

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            var reader = new CfbBinaryReader(data);
            _ = reader.ReadBytes(-1);
        });
    }
}
