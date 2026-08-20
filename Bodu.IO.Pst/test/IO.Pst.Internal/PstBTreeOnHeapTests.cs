// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstBTreeOnHeapTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the BTree-on-heap reader: header parsing, leaf enumeration across leaf-only and indexed trees, keyed
/// lookup, and rejection of malformed tree geometry.
/// </summary>
[TestClass]
public class PstBTreeOnHeapTests
{
    /// <summary>The node identifier the fixtures use for the node under test.</summary>
    private const uint NodeId = 0x21;

    /// <summary>
    /// Opens a container and parses the heap of the node under test.
    /// </summary>
    /// <param name="ltp">The LTP builder holding the heap contents.</param>
    /// <param name="validationLevel">The validation level to open under.</param>
    /// <returns>The open file and the parsed heap; the caller disposes the file.</returns>
    private static (PstFile File, PstHeapNode Heap) Open(PstLtpFixtureBuilder ltp, PstValidationLevel validationLevel = PstValidationLevel.Compatible)
    {
        var builder = new PstFixtureBuilder();
        ltp.AddHeapNode(builder, NodeId);

        PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = validationLevel });
        Assert.IsTrue(PstBTree.TryFindNode(file.GetSource(), NodeId, out PstNbtEntry entry));
        return (file, PstHeapNode.Parse(file.GetSource(), entry));
    }

    /// <summary>
    /// Verifies that a well-formed header parses its declared geometry.
    /// </summary>
    [TestMethod]
    public void ReadHeader_WhenWellFormed_ShouldParseGeometry()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint headerHid = ltp.AddBTreeOnHeap(2, 6, (1, new byte[6]));

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);

            Assert.AreEqual(2, header.KeySize);
            Assert.AreEqual(6, header.DataSize);
            Assert.AreEqual(0, header.IndexLevels);
            Assert.AreNotEqual(0u, header.RootHid);
        }
    }

    /// <summary>
    /// Verifies that malformed headers are rejected: a wrong type byte, an unsupported key width, and a zero data
    /// width.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="typeByte">The header's type byte.</param>
    /// <param name="keySize">The header's declared key width.</param>
    /// <param name="dataSize">The header's declared data width.</param>
    [TestMethod]
    [DataRow("wrong type byte", (byte)0xB4, (byte)2, (byte)6)]
    [DataRow("unsupported key width", (byte)0xB5, (byte)3, (byte)6)]
    [DataRow("zero data width", (byte)0xB5, (byte)2, (byte)0)]
    public void ReadHeader_WhenHeaderIsMalformed_ShouldThrowPstFileFormatException(string testName, byte typeByte, byte keySize, byte dataSize)
    {
        Assert.IsNotNull(testName);

        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        byte[] raw = [typeByte, keySize, dataSize, 0, 0, 0, 0, 0];
        uint headerHid = ltp.AddItem(raw);

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = PstBTreeOnHeap.ReadHeader(heap, headerHid));
        }
    }

    /// <summary>
    /// Verifies that an empty tree — a zero root identifier — enumerates no records.
    /// </summary>
    [TestMethod]
    public void EnumerateRecords_WhenTreeIsEmpty_ShouldYieldNothing()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint headerHid = ltp.AddBTreeOnHeap(2, 6);

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);

            Assert.AreEqual(0, PstBTreeOnHeap.EnumerateRecords(heap, header, PstValidationLevel.Compatible).Count());
        }
    }

    /// <summary>
    /// Verifies that a leaf-only tree enumerates its records in stored order with the declared key and data widths.
    /// </summary>
    [TestMethod]
    public void EnumerateRecords_WhenLeafOnlyTree_ShouldYieldRecordsInOrder()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint headerHid = ltp.AddBTreeOnHeap(
            2,
            4,
            (0x10, [1, 0, 0, 0]),
            (0x20, [2, 0, 0, 0]),
            (0x30, [3, 0, 0, 0]));

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);
            var records = PstBTreeOnHeap.EnumerateRecords(heap, header, PstValidationLevel.Compatible).ToList();

            Assert.AreEqual(3, records.Count);
            Assert.AreEqual(0x10, BinaryPrimitives.ReadUInt16LittleEndian(records[0].Key.Span));
            Assert.AreEqual(0x30, BinaryPrimitives.ReadUInt16LittleEndian(records[2].Key.Span));
            Assert.AreEqual(3, records[2].Data.Span[0]);
        }
    }

    /// <summary>
    /// Verifies that a two-level tree — an index item over two leaf items — enumerates every leaf record in order.
    /// </summary>
    [TestMethod]
    public void EnumerateRecords_WhenIndexedTree_ShouldDescendAndYieldEveryLeaf()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint firstLeaf = ltp.AddBthLeafItem(2, 4, (0x10, [1, 0, 0, 0]), (0x20, [2, 0, 0, 0]));
        uint secondLeaf = ltp.AddBthLeafItem(2, 4, (0x30, [3, 0, 0, 0]), (0x40, [4, 0, 0, 0]));
        uint index = ltp.AddBthIndexItem(2, (0x10, firstLeaf), (0x30, secondLeaf));
        uint headerHid = ltp.AddBthHeaderItem(2, 4, indexLevels: 1, index);

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);
            var records = PstBTreeOnHeap.EnumerateRecords(heap, header, PstValidationLevel.Compatible).ToList();

            Assert.AreEqual(4, records.Count);
            CollectionAssert.AreEqual(
                new ushort[] { 0x10, 0x20, 0x30, 0x40 },
                records.Select(r => BinaryPrimitives.ReadUInt16LittleEndian(r.Key.Span)).ToArray());
        }
    }

    /// <summary>
    /// Verifies keyed lookup across a leaf-only tree: hits return the record data, misses — below, between, and above
    /// the stored keys — report absence.
    /// </summary>
    [TestMethod]
    public void TryFind_WhenLeafOnlyTree_ShouldFindPresentKeysAndMissAbsentOnes()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint headerHid = ltp.AddBTreeOnHeap(
            2,
            4,
            (0x10, [1, 0, 0, 0]),
            (0x30, [3, 0, 0, 0]));

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);

            Assert.IsTrue(PstBTreeOnHeap.TryFind(heap, header, 0x30, out ReadOnlyMemory<byte> data));
            Assert.AreEqual(3, data.Span[0]);

            Assert.IsFalse(PstBTreeOnHeap.TryFind(heap, header, 0x05, out _));
            Assert.IsFalse(PstBTreeOnHeap.TryFind(heap, header, 0x20, out _));
            Assert.IsFalse(PstBTreeOnHeap.TryFind(heap, header, 0x99, out _));
        }
    }

    /// <summary>
    /// Verifies keyed lookup through an index level: the descent follows the child whose first key bounds the target.
    /// </summary>
    [TestMethod]
    public void TryFind_WhenIndexedTree_ShouldDescendToTheCorrectLeaf()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint firstLeaf = ltp.AddBthLeafItem(4, 4, (0x10, [1, 0, 0, 0]), (0x20, [2, 0, 0, 0]));
        uint secondLeaf = ltp.AddBthLeafItem(4, 4, (0x30, [3, 0, 0, 0]), (0x40, [4, 0, 0, 0]));
        uint index = ltp.AddBthIndexItem(4, (0x10, firstLeaf), (0x30, secondLeaf));
        uint headerHid = ltp.AddBthHeaderItem(4, 4, indexLevels: 1, index);

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);

            Assert.IsTrue(PstBTreeOnHeap.TryFind(heap, header, 0x40, out ReadOnlyMemory<byte> data));
            Assert.AreEqual(4, data.Span[0]);

            Assert.IsFalse(PstBTreeOnHeap.TryFind(heap, header, 0x08, out _));
            Assert.IsFalse(PstBTreeOnHeap.TryFind(heap, header, 0x25, out _));
        }
    }

    /// <summary>
    /// Verifies that a leaf item whose length is not a whole number of records is rejected.
    /// </summary>
    [TestMethod]
    public void EnumerateRecords_WhenLeafLengthIsNotAWholeRecordCount_ShouldThrowPstFileFormatException()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint leaf = ltp.AddItem([1, 2, 3, 4, 5]);
        uint headerHid = ltp.AddBthHeaderItem(2, 4, indexLevels: 0, leaf);

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);

            _ = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = PstBTreeOnHeap.EnumerateRecords(heap, header, PstValidationLevel.Compatible).ToList();
            });
        }
    }

    /// <summary>
    /// Verifies that leaf keys that regress are accepted under <see cref="PstValidationLevel.Compatible" /> but
    /// rejected under <see cref="PstValidationLevel.Strict" />.
    /// </summary>
    [TestMethod]
    public void EnumerateRecords_WhenLeafKeysRegress_ShouldThrowOnlyUnderStrict()
    {
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0xB5 };
        uint headerHid = ltp.AddBTreeOnHeap(
            2,
            4,
            (0x30, [3, 0, 0, 0]),
            (0x10, [1, 0, 0, 0]));

        (PstFile file, PstHeapNode heap) = Open(ltp);
        using (file)
        {
            PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, headerHid);

            Assert.AreEqual(2, PstBTreeOnHeap.EnumerateRecords(heap, header, PstValidationLevel.Compatible).Count());
            _ = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = PstBTreeOnHeap.EnumerateRecords(heap, header, PstValidationLevel.Strict).ToList();
            });
        }
    }
}
