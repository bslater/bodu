// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Reads a node's property context (<c>PC</c>): the BTree-on-heap keyed by 16-bit property identifiers whose 6-byte
/// records carry each property's wire type and value dword.
/// </summary>
internal static class PstPropertyContextReader
{
    /// <summary>The key width a property context's tree declares.</summary>
    private const byte PcKeySize = 2;

    /// <summary>The record data width a property context's tree declares.</summary>
    private const byte PcDataSize = 6;

    /// <summary>
    /// Reads the property context of the supplied node entry.
    /// </summary>
    /// <param name="source">The open source.</param>
    /// <param name="entry">The node whose heap carries the context.</param>
    /// <returns>The parsed heap and the context's records in key order, values unresolved.</returns>
    /// <exception cref="PstFileFormatException">
    /// The node's heap does not declare a property context, the tree's shape is not the property-context shape, or —
    /// under <see cref="PstValidationLevel.Strict" /> — a record declares an unrecognized wire type.
    /// </exception>
    internal static (PstHeapNode Heap, List<PstPcEntry> Entries) Read(PstSource source, PstNbtEntry entry)
    {
        PstHeapNode heap = PstHeapNode.Parse(source, entry);
        if (heap.ClientSignature != PstHeapNode.PropertyContextSignature)
            throw Malformed(entry.NodeId);

        var entries = new List<PstPcEntry>();
        if (heap.UserRootHid == 0)
            return (heap, entries);

        // The record layout below slices fixed offsets, so the 2/6 shape is structural at every validation level.
        PstBthHeader header = PstBTreeOnHeap.ReadHeader(heap, heap.UserRootHid);
        if (header.KeySize != PcKeySize || header.DataSize != PcDataSize)
            throw Malformed(entry.NodeId);

        foreach ((ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> data) in PstBTreeOnHeap.EnumerateRecords(heap, header, source.ValidationLevel))
        {
            ushort propertyId = BinaryPrimitives.ReadUInt16LittleEndian(key.Span);
            ushort wireType = BinaryPrimitives.ReadUInt16LittleEndian(data.Span);
            uint rawValue = BinaryPrimitives.ReadUInt32LittleEndian(data.Span.Slice(2));

            if (source.ValidationLevel == PstValidationLevel.Strict && !PstWireType.IsKnown(wireType))
            {
                throw new PstFileFormatException(string.Format(
                    CultureInfo.CurrentCulture,
                    PstResourceStrings.Format_Invalid_PstPropertyWireType,
                    propertyId,
                    new PstNodeId(entry.NodeId),
                    wireType));
            }

            entries.Add(new PstPcEntry(propertyId, wireType, rawValue));
        }

        return (heap, entries);
    }

    /// <summary>
    /// Creates the malformed-property-context exception for a node identifier.
    /// </summary>
    /// <param name="nodeId">The owning node identifier.</param>
    /// <returns>The exception to throw.</returns>
    private static PstFileFormatException Malformed(uint nodeId) =>
        new(string.Format(CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPropertyContext, new PstNodeId(nodeId)));
}
