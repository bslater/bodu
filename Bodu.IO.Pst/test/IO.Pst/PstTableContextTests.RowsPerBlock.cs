// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextTests.RowsPerBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

public partial class PstTableContextTests
{
    /// <summary>
    /// Verifies that a point lookup on a multi-block row matrix lands on the right row in both formats: the rows a
    /// block holds is ⌊block payload / row width⌋, and the block payload is 8,176 bytes in a Unicode store but 8,180
    /// in an ANSI store, so a 2,044-byte row packs three per block in one and four in the other.
    /// </summary>
    [TestMethod]
    [DataRow(PstFileFormat.Unicode)]
    [DataRow(PstFileFormat.Ansi)]
    public void TryGetRow_WhenMatrixSpansBlocks_ShouldUseTheFormatBlockPayload(PstFileFormat format)
    {
        const int WideRowWidth = 2044;
        const int RowCount = 5;
        const uint SubnodeId = 0x41;

        var builder = new PstFixtureBuilder { Format = format };
        var ltp = new PstLtpFixtureBuilder();
        int rowsPerBlock = builder.Layout.MaxBlockPayload / WideRowWidth;

        var blockIds = new List<ulong>();
        var index = new (ulong RowId, uint RowNumber)[RowCount];
        for (int start = 0; start < RowCount; start += rowsPerBlock)
        {
            int rows = Math.Min(rowsPerBlock, RowCount - start);
            var block = new byte[rows * WideRowWidth];
            for (int i = 0; i < rows; i++)
            {
                int row = start + i;
                Span<byte> bytes = block.AsSpan(i * WideRowWidth, WideRowWidth);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)(0x100 + row));
                BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(4), row * 11);
                bytes[8] = 0b1100_0000;
                index[row] = ((ulong)(0x100 + row), (uint)row);
            }

            blockIds.Add(builder.AddDataBlock(block));
        }

        ulong matrix = builder.AddXBlock((uint)(RowCount * WideRowWidth), [.. blockIds]);
        ulong subnodeTree = builder.AddSubnodeLeafBlock((SubnodeId, matrix, 0));
        _ = ltp.AddTableContext(
            [
                (RowIdTag, 0, 4, 0),
                (Int32Tag, 4, 4, 1),
            ],
            endOffset4: 8,
            endOffset2: 8,
            endOffset1: 8,
            rowWidth: WideRowWidth,
            rowsHnid: SubnodeId,
            index);
        ltp.AddHeapNode(builder, NodeId, subnodeTree);

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });
        PstTableContext context = file.GetNode(new PstNodeId(NodeId)).ReadTableContext();

        Assert.AreEqual(RowCount, context.RowCount);
        for (int row = 0; row < RowCount; row++)
        {
            Assert.IsTrue(context.TryGetRow((uint)(0x100 + row), out PstTableRow? found), $"Row {row} must resolve.");
            Assert.AreEqual((uint)(0x100 + row), found.RowId);
            Assert.IsTrue(found.TryGetCell(0x1111, out PstPropertyValue cell));
            Assert.AreEqual(row * 11, cell.GetInt32(), $"Row {row} resolved to a different row's cell.");
        }
    }
}
