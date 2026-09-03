// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTreeTests.ReadEntry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstBTreeTests
{
    /// <summary>
    /// Verifies that a node entry whose 64-bit identifier carries bits above the 32-bit node-identifier space is
    /// rejected rather than silently truncated onto an unrelated identifier.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenNodeIdentifierExceeds32Bits_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3]));
        byte[] file = builder.Build();

        // The leaf NBTENTRY's nid is a 64-bit field; set a bit above the 32-bit identifier space.
        long root = PstFixtureBuilder.ReadNodeTreeRootOffset(file);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan((int)root + 4), 0x0000_0001);

        using PstFile pst = PstFile.Open(new MemoryStream(file, writable: false), PstFileOptions.Default);

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = pst.EnumerateNodes().ToList();
        });

        Assert.AreEqual(PstFileError.InvalidPage, ex.Error);
    }
}
