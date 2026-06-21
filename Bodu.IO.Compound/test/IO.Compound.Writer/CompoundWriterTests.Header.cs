// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundWriterTests.Header.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound.Nodes;

namespace Bodu.IO.Compound.Writer;

public partial class CompoundWriterTests
{
    /// <summary>The compound-file signature bytes.</summary>
    private static readonly byte[] Signature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    /// <summary>
    /// Verifies that a version-3 file carries the expected header fields and is aligned to 512-byte sectors.
    /// </summary>
    [TestMethod]
    public void Save_WhenVersion3_ShouldWriteExpectedHeaderAndAlignment()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream("Data", new byte[] { 1, 2, 3 });

        byte[] bytes = root.ToArray(new CompoundWriterOptions { Version = CompoundFileVersion.V3 });

        CollectionAssert.AreEqual(Signature, bytes[..8]);
        Assert.AreEqual(3, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(26)));
        Assert.AreEqual(0xFFFE, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(28)));
        Assert.AreEqual(9, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(30)));
        Assert.AreEqual(6, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(32)));
        Assert.AreEqual(0, bytes.Length % 512);
        Assert.IsTrue(CompoundFile.IsCompoundFile(bytes));
    }

    /// <summary>
    /// Verifies that a version-4 file uses 4096-byte sectors, records its directory sector count, and is 4096-aligned.
    /// </summary>
    [TestMethod]
    public void Save_WhenVersion4_ShouldWriteExpectedHeaderAndAlignment()
    {
        CompoundStorageNode root = CompoundStorageNode.CreateRoot();
        _ = root.AddStream("Data", new byte[] { 1, 2, 3 });

        byte[] bytes = root.ToArray(new CompoundWriterOptions { Version = CompoundFileVersion.V4 });

        Assert.AreEqual(4, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(26)));
        Assert.AreEqual(12, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(30)));
        Assert.AreEqual(0, bytes.Length % 4096);

        // Version 4 must declare the number of directory sectors (offset 40); version 3 leaves it zero.
        Assert.IsTrue(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(40)) >= 1);
    }
}
