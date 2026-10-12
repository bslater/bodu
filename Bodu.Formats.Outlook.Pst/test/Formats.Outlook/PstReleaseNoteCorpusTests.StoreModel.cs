// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.StoreModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

using Bodu.IO.Pst;
using Bodu.IO.Pst.Internal;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the mail store <c>Mail</c> models, and writes it through <see cref="PstFixtureBuilder" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every object is a heap-on-node: a property context fills its heap by the heap rule (<see cref="HeapWriter" />),
    /// its values in identifier order, then adds its BTH leaf and BTHHEADER to the current block. A value is inline for
    /// the types <see cref="PropertyModel.IsInline" /> names; otherwise it is a heap item when at most 3580 bytes, or
    /// else a subnode of the owning node with identifier <c>((0x800 + k) &lt;&lt; 5) | 0x1F</c>, the lowest k not yet
    /// used in that node's subnode list. A node's subnodes form one SLBLOCK in identifier order.
    /// </para>
    /// <para>
    /// A table puts column 0x67F20003 first and the other cell types in tag order, 8- and 4-byte cells first, then
    /// 2-byte, then 1-byte, then the existence bitmap; a variable-size cell holds an HNID by the same rule. Its row
    /// matrix is a heap item up to <see cref="MatrixHeapLimit" /> bytes, else a subnode of the table split into blocks
    /// of whole rows.
    /// </para>
    /// </remarks>
    private sealed class StoreModel
    {
        /// <summary>The builder the store is written through.</summary>
        private readonly PstFixtureBuilder _file;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreModel" /> class.
        /// </summary>
        /// <param name="file">
        /// The builder the store is written through, its format and header knobs already set.
        /// </param>
        public StoreModel(PstFixtureBuilder file)
        {
            _file = file;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the message store object, node 0x21, is written.
        /// </summary>
        public bool IncludeStoreObject { get; set; } = true;

        /// <summary>
        /// Gets the message store object's properties.
        /// </summary>
        public List<PropertyModel> StoreProperties { get; } = [];

        /// <summary>
        /// Gets or sets the name-to-id map, node 0x61, or <see langword="null" /> to write none.
        /// </summary>
        public NameMapModel? NameMap { get; set; }

        /// <summary>
        /// Gets the root folder, node 0x122, whose node records itself as its parent.
        /// </summary>
        public FolderModel Root { get; } = new(0x122);

        /// <summary>
        /// Gets or sets the row-matrix size above which a table's matrix moves from a heap item to a subnode.
        /// </summary>
        public int MatrixHeapLimit { get; set; } = InputGenerator.MaxHeapItem;

        /// <summary>
        /// Gets or sets a value indicating whether every row-matrix block but the last is padded to the full payload.
        /// </summary>
        public bool PadMatrixBlocks { get; set; }

        /// <summary>
        /// Writes the store: the store object, the name-to-id map, then the folders depth first from the root, each
        /// folder's tables, child folders and messages in that order.
        /// </summary>
        public void Write()
        {
            if (IncludeStoreObject)
                WriteTopNode(0x21, StoreProperties, 0);

            if (NameMap is NameMapModel map)
            {
                List<PropertyModel> properties =
                [
                    PropertyModel.Int32(0x0001, 251),
                    PropertyModel.Binary(0x0002, map.Guids),
                    PropertyModel.Binary(0x0003, map.Entries),
                    PropertyModel.Binary(0x0004, map.Strings),
                ];
                WriteTopNode(0x61, properties, 0);
            }

            WriteFolder(Root, Root.ParentOverride ?? Root.NodeId);
        }

        /// <summary>
        /// Gets the lowest subnode identifier <c>((0x800 + k) &lt;&lt; 5) | 0x1F</c> a subnode list does not use.
        /// </summary>
        /// <param name="subnodes">The subnode list.</param>
        /// <returns>The identifier.</returns>
        private static uint NextSubnodeId(List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)> subnodes)
        {
            uint index = 0x800;
            while (subnodes.Exists(subnode => subnode.NodeId == ((index << 5) | 0x1F)))
                index++;

            return (index << 5) | 0x1F;
        }

        /// <summary>
        /// Writes a folder's node, its hierarchy and contents tables, its child folders, then its messages.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="parent">The parent its node records.</param>
        private void WriteFolder(FolderModel folder, uint parent)
        {
            WriteTopNode(folder.NodeId, folder.Properties, parent);
            uint index = folder.NodeId >> 5;

            if (folder.WriteHierarchyTable)
                WriteReferenceTable((index << 5) | 0x0D, [.. folder.Children.Select(child => child.NodeId)]);

            if (folder.WriteContentsTable)
                WriteReferenceTable((index << 5) | 0x0E, [.. folder.DanglingContentsRows, .. folder.Messages.Select(message => message.NodeId)]);

            foreach (FolderModel child in folder.Children)
                WriteFolder(child, child.ParentOverride ?? folder.NodeId);

            foreach (MessageModel message in folder.Messages)
            {
                (ulong dataBlock, ulong subnodeBlock) = WriteMessage(message);
                _file.AddNode(message.NodeId, dataBlock, subnodeBlock, folder.NodeId);
            }
        }

        /// <summary>
        /// Writes a message's recipient table, attachment table and attachment objects as subnodes, then its property
        /// context.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The identifiers of the message's data block and subnode tree block.</returns>
        private (ulong DataBlockId, ulong SubnodeBlockId) WriteMessage(MessageModel message)
        {
            var subnodes = new List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)>();
            if (message.Recipients is List<RecipientModel> recipients)
            {
                List<(uint RowId, List<PropertyModel> Cells)> rows = [.. recipients.Select((recipient, i) => ((uint)(i + 1), recipient.Properties))];
                subnodes.Add(WriteTableSubnode(0x692, rows));
            }

            if (message.Attachments is List<AttachmentModel> attachments)
            {
                List<(uint RowId, List<PropertyModel> Cells)> rows =
                    [.. attachments.Select((attachment, i) => (attachment.NodeId, (List<PropertyModel>)[PropertyModel.Int32(0x0E21, i), .. attachment.RowProperties]))];
                subnodes.Add(WriteTableSubnode(0x671, rows));

                foreach (AttachmentModel attachment in attachments.Where(attachment => attachment.WriteNode))
                    subnodes.Add(WriteAttachment(attachment));
            }

            ulong dataBlock = WritePropertyContext(message.Properties, subnodes);
            return (dataBlock, WriteSubnodeTree(subnodes, message.SubnodeIndex));
        }

        /// <summary>
        /// Writes an attachment object: its property context, with the PidTagAttachDataObject record of an embedded
        /// message or object payload, and that message or payload as a subnode.
        /// </summary>
        /// <param name="attachment">The attachment.</param>
        /// <returns>The attachment object's subnode entry.</returns>
        private (uint NodeId, ulong DataBlockId, ulong SubnodeBlockId) WriteAttachment(AttachmentModel attachment)
        {
            var subnodes = new List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)>();
            List<PropertyModel> properties = [.. attachment.Properties];
            if (attachment.Embedded is MessageModel embedded)
            {
                var record = new byte[8];
                BinaryPrimitives.WriteUInt32LittleEndian(record, embedded.NodeId);
                properties.Add(new PropertyModel(0x3701, 0x000D) { Data = record, Placement = ValuePlacement.Heap });
                if (attachment.WriteEmbeddedNode && !attachment.EmbeddedHasData)
                {
                    subnodes.Add((embedded.NodeId, 0, 0));
                }
                else if (attachment.WriteEmbeddedNode)
                {
                    (ulong dataBlock, ulong subnodeBlock) = WriteMessage(embedded);
                    subnodes.Add((embedded.NodeId, dataBlock, subnodeBlock));
                }
            }
            else if (attachment.ObjectData is byte[] objectData)
            {
                const uint ObjectNodeId = (0x0901u << 5) | 0x1F;
                var record = new byte[8];
                BinaryPrimitives.WriteUInt32LittleEndian(record, ObjectNodeId);
                BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(4), (uint)objectData.Length);
                properties.Add(new PropertyModel(0x3701, 0x000D) { Data = record, Placement = ValuePlacement.Heap });
                subnodes.Add((ObjectNodeId, InputGenerator.WriteData(_file, objectData), 0));
            }

            ulong attachmentData = WritePropertyContext(properties, subnodes);
            return (attachment.NodeId, attachmentData, WriteSubnodeTree(subnodes, index: false));
        }

        /// <summary>
        /// Writes a top-level node holding a property context, with any subnodes its values need.
        /// </summary>
        /// <param name="nodeId">The node identifier.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="parent">The parent the node records.</param>
        private void WriteTopNode(uint nodeId, List<PropertyModel> properties, uint parent)
        {
            var subnodes = new List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)>();
            ulong dataBlock = WritePropertyContext(properties, subnodes);
            _file.AddNode(nodeId, dataBlock, WriteSubnodeTree(subnodes, index: false), parent);
        }

        /// <summary>
        /// Writes a subnode tree: one SLBLOCK in identifier order or, with <paramref name="index" />, an SIBLOCK over
        /// one SLBLOCK per entry.
        /// </summary>
        /// <param name="subnodes">The subnode entries.</param>
        /// <param name="index">Whether to write the SIBLOCK level.</param>
        /// <returns>The tree's root block identifier, or zero when there are no subnodes.</returns>
        private ulong WriteSubnodeTree(List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)> subnodes, bool index)
        {
            if (subnodes.Count == 0)
                return 0;

            (uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)[] sorted = [.. subnodes.OrderBy(subnode => subnode.NodeId)];
            if (!index)
                return _file.AddSubnodeLeafBlock(sorted);

            ulong[] leaves = [.. sorted.Select(entry => _file.AddSubnodeLeafBlock(entry))];
            return _file.AddSubnodeIndexBlock(leaves);
        }

        /// <summary>
        /// Writes a property context's heap, storing each value by the heap rule.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="subnodes">The owning node's subnodes, to which subnode-resident values are added.</param>
        /// <returns>The heap's data block identifier.</returns>
        private ulong WritePropertyContext(List<PropertyModel> properties, List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)> subnodes)
        {
            var heap = new HeapWriter(InputGenerator.BlockPayload(_file.Format));
            (ushort PropertyId, ushort WireType, uint RawValue)[] records =
                [.. properties.OrderBy(property => property.Id).Select(property => (property.Id, property.Type, Reference(heap, property, subnodes)))];
            heap.Ltp.AddPropertyContext(records);

            return heap.Emit(_file);
        }

        /// <summary>
        /// Stores a value that is not inline and returns the reference its record or cell holds.
        /// </summary>
        /// <param name="heap">The owning heap.</param>
        /// <param name="property">The property.</param>
        /// <param name="subnodes">The owning node's subnodes, to which a subnode-resident value is added.</param>
        /// <returns>The inline value, a heap identifier, a subnode identifier, zero or the raw reference.</returns>
        private uint Reference(HeapWriter heap, PropertyModel property, List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)> subnodes)
        {
            if (property.IsInline)
                return property.Inline;

            switch (property.Placement)
            {
                case ValuePlacement.Null:
                    return 0;

                case ValuePlacement.Raw:
                    return property.RawReference;

                case ValuePlacement.Heap:
                case ValuePlacement.Auto when property.Data.Length <= InputGenerator.MaxHeapItem:
                    return heap.AddItem(property.Data);

                default:
                    uint nodeId = NextSubnodeId(subnodes);
                    subnodes.Add((nodeId, InputGenerator.WriteData(_file, property.Data), 0));
                    return nodeId;
            }
        }

        /// <summary>
        /// Writes a top-level table whose rows carry only their row identifiers, as a hierarchy or contents table does.
        /// </summary>
        /// <param name="tableNodeId">The table's node identifier.</param>
        /// <param name="rowIds">The row identifiers.</param>
        private void WriteReferenceTable(uint tableNodeId, uint[] rowIds)
        {
            var subnodes = new List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)>();
            ulong dataBlock = WriteTable([.. rowIds.Select(rowId => (rowId, new List<PropertyModel>()))], subnodes);
            _file.AddNode(tableNodeId, dataBlock, WriteSubnodeTree(subnodes, index: false), 0);
        }

        /// <summary>
        /// Writes a table that is a subnode of a message, with the table's own subnodes in its own subnode tree.
        /// </summary>
        /// <param name="tableNodeId">The table's subnode identifier.</param>
        /// <param name="rows">The rows: row identifier and cells.</param>
        /// <returns>The table's subnode entry.</returns>
        private (uint NodeId, ulong DataBlockId, ulong SubnodeBlockId) WriteTableSubnode(uint tableNodeId, List<(uint RowId, List<PropertyModel> Cells)> rows)
        {
            var subnodes = new List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)>();
            ulong dataBlock = WriteTable(rows, subnodes);

            return (tableNodeId, dataBlock, WriteSubnodeTree(subnodes, index: false));
        }

        /// <summary>
        /// Writes a table context's heap: the row matrix, the row index BTH and the TCINFO.
        /// </summary>
        /// <param name="rows">The rows: row identifier and cells.</param>
        /// <param name="subnodes">The table's subnodes, to which a subnode-resident matrix or value is added.</param>
        /// <returns>The heap's data block identifier.</returns>
        private ulong WriteTable(List<(uint RowId, List<PropertyModel> Cells)> rows, List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)> subnodes)
        {
            int payload = InputGenerator.BlockPayload(_file.Format);
            var heap = new HeapWriter(payload);
            var columns = new List<(uint Tag, int Width)> { (0x67F20003, 4) };
            foreach (PropertyModel cell in rows.SelectMany(row => row.Cells).GroupBy(cell => cell.Tag).Select(group => group.First()).OrderBy(cell => cell.Tag))
            {
                if (cell.Tag != 0x67F20003)
                    columns.Add((cell.Tag, cell.CellWidth));
            }

            // Cells are laid out by width: 8- and 4-byte cells first, in column order, then 2-byte, then 1-byte.
            var offsets = new Dictionary<uint, int>();
            int offset = 0;
            foreach ((uint tag, int width) in columns.Where(column => column.Width >= 4).Concat(columns.Where(column => column.Width == 2)).Concat(columns.Where(column => column.Width == 1)))
            {
                offsets[tag] = offset;
                offset += width;
            }

            int end4 = columns.Where(column => column.Width >= 4).Sum(column => column.Width);
            int end2 = end4 + columns.Where(column => column.Width == 2).Sum(column => column.Width);
            int end1 = offset;
            int rowWidth = end1 + ((columns.Count + 7) / 8);

            var matrix = new List<byte[]>();
            foreach ((uint rowId, List<PropertyModel> cells) in rows)
            {
                var row = new byte[rowWidth];
                BinaryPrimitives.WriteUInt32LittleEndian(row, rowId);
                row[end1] |= 0x80;
                foreach (PropertyModel cell in cells)
                {
                    int column = columns.FindIndex(candidate => candidate.Tag == cell.Tag);
                    int at = offsets[cell.Tag];
                    if (cell.IsInline && cell.CellWidth == 1)
                    {
                        row[at] = (byte)cell.Inline;
                    }
                    else if (cell.IsInline && cell.CellWidth == 2)
                    {
                        BinaryPrimitives.WriteUInt16LittleEndian(row.AsSpan(at), (ushort)cell.Inline);
                    }
                    else if (cell.IsInline)
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(at), cell.Inline);
                    }
                    else if (cell.CellWidth == 8)
                    {
                        cell.Data.AsSpan(0, 8).CopyTo(row.AsSpan(at));
                    }
                    else
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(at), Reference(heap, cell, subnodes));
                    }

                    row[end1 + (column / 8)] |= (byte)(0x80 >> (column % 8));
                }

                matrix.Add(row);
            }

            uint matrixHnid = 0;
            if (rows.Count > 0 && matrix.Sum(row => row.Length) <= MatrixHeapLimit)
            {
                matrixHnid = heap.AddItem([.. matrix.SelectMany(row => row)]);
            }
            else if (rows.Count > 0)
            {
                int perBlock = Math.Max(1, payload / rowWidth);
                var segments = new List<byte[]>();
                for (int start = 0; start < matrix.Count; start += perBlock)
                {
                    byte[] segment = [.. matrix.Skip(start).Take(perBlock).SelectMany(row => row)];
                    if (PadMatrixBlocks && start + perBlock < matrix.Count)
                        Array.Resize(ref segment, payload);

                    segments.Add(segment);
                }

                matrixHnid = NextSubnodeId(subnodes);
                subnodes.Add((matrixHnid, InputGenerator.WriteSegments(_file, segments), 0));
            }

            // The row index maps each row identifier to its row number, four bytes wide in Unicode and two in ANSI.
            int numberWidth = _file.Format == PstFileFormat.Ansi ? 2 : 4;
            (ulong Key, byte[] Data)[] index = [.. rows
                .Select((row, i) =>
                {
                    var number = new byte[numberWidth];
                    if (numberWidth == 4)
                        BinaryPrimitives.WriteUInt32LittleEndian(number, (uint)i);
                    else
                        BinaryPrimitives.WriteUInt16LittleEndian(number, (ushort)i);

                    return ((ulong)row.RowId, number);
                })
                .OrderBy(record => record.Item1)];
            uint rowIndex = heap.AddBth(4, (byte)numberWidth, index);

            List<(uint Tag, int Offset, int Width)> descriptors = [.. columns.Select(column => (column.Tag, offsets[column.Tag], column.Width))];
            heap.Ltp.ClientSignature = 0x7C;
            heap.Ltp.UserRootHid = heap.AddItem(ContextGenerator.TableInfo(descriptors, end4, end2, end1, rowWidth, rowIndex, matrixHnid));

            return heap.Emit(_file);
        }
    }
}
