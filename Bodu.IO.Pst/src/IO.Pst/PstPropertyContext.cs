// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Collections;
using System.Globalization;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Represents a node's property context (<c>PC</c>): the LTP property bag of 16-bit property identifiers with
/// wire-typed values, in the manner the exploration plan sketches — format-agnostic, with no MAPI semantics.
/// </summary>
/// <remarks>
/// The context's records are materialized when the context is read; each value's payload is resolved on access, so a
/// large subnode-resident value costs its read only when it is actually retrieved. Enumeration yields values in
/// property-identifier order (the tree's stored order).
/// </remarks>
public sealed class PstPropertyContext
    : IReadOnlyCollection<PstPropertyValue>
{
    /// <summary>The context's heap.</summary>
    private readonly PstHeapNode _heap;

    /// <summary>The value-reference resolver over the owning node.</summary>
    private readonly PstLtpContext _context;

    /// <summary>The context's records in key order, values unresolved.</summary>
    private readonly List<PstPcEntry> _entries;

    /// <summary>
    /// Initializes a new instance of the <see cref="PstPropertyContext" /> class.
    /// </summary>
    /// <param name="heap">The context's heap.</param>
    /// <param name="context">The value-reference resolver.</param>
    /// <param name="entries">The context's records in key order.</param>
    internal PstPropertyContext(PstHeapNode heap, PstLtpContext context, List<PstPcEntry> entries)
    {
        _heap = heap;
        _context = context;
        _entries = entries;
    }

    /// <summary>
    /// Gets the number of properties the context carries.
    /// </summary>
    /// <value>The property count.</value>
    public int Count => _entries.Count;

    /// <summary>
    /// Determines whether the context carries a property.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <returns><see langword="true" /> when the property is present.</returns>
    public bool Contains(ushort propertyId) =>
        FindEntry(propertyId) >= 0;

    /// <summary>
    /// Enumerates the identifiers of the properties the context holds, in ascending order, without materializing any
    /// value.
    /// </summary>
    /// <returns>The property identifiers.</returns>
    public IEnumerable<ushort> EnumeratePropertyIds()
    {
        foreach (PstPcEntry entry in _entries)
            yield return entry.PropertyId;
    }

    /// <summary>
    /// Attempts to determine a property's wire type without materializing its value.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <param name="wireType">When this method returns <see langword="true" />, the raw 16-bit wire type.</param>
    /// <returns><see langword="true" /> when the property is present.</returns>
    public bool TryGetWireType(ushort propertyId, out ushort wireType)
    {
        int index = FindEntry(propertyId);
        if (index < 0)
        {
            wireType = 0;
            return false;
        }

        wireType = _entries[index].WireType;
        return true;
    }

    /// <summary>
    /// Attempts to determine the length of a property's payload without reading it.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <param name="length">When this method returns <see langword="true" />, the payload length in bytes.</param>
    /// <returns><see langword="true" /> when the property is present.</returns>
    /// <exception cref="PstFileFormatException">The value's storage is malformed.</exception>
    /// <remarks>
    /// For a subnode-resident value only the data tree's index blocks are read, so the length is available for a
    /// payload of any size regardless of <see cref="PstFileOptions.MaxNodeDataLength" />.
    /// </remarks>
    public bool TryGetValueLength(ushort propertyId, out long length)
    {
        int index = FindEntry(propertyId);
        if (index < 0)
        {
            length = 0;
            return false;
        }

        PstPcEntry entry = _entries[index];
        length = IsOutOfLine(entry)
            ? _context.GetHnidLength(_heap, entry.RawValue)
            : Materialize(entry).RawData.Length;
        return true;
    }

    /// <summary>
    /// Attempts to open a property's payload as a read-only, seekable stream.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <param name="stream">When this method returns <see langword="true" />, the payload stream.</param>
    /// <returns><see langword="true" /> when the property is present.</returns>
    /// <exception cref="PstFileFormatException">The value's storage is malformed.</exception>
    /// <remarks>
    /// <para>
    /// The stream is the streaming counterpart of <see cref="TryGetValue" />: a heap-resident value is served from
    /// the heap's decoded bytes, and a subnode-resident value is read block by block on demand — the same stream
    /// <see cref="PstNode.OpenDataStream" /> returns — so <see cref="PstFileOptions.MaxNodeDataLength" />, which
    /// bounds materialization, does not apply. The stream is bound to the owning session and must be disposed before
    /// it.
    /// </para>
    /// <para>
    /// Inline and fixed-width values are exposed as their raw little-endian bytes for uniformity; the typed accessors
    /// on <see cref="PstPropertyValue" /> remain the natural way to read them.
    /// </para>
    /// </remarks>
    public bool TryOpenValueStream(ushort propertyId, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out Stream stream)
    {
        int index = FindEntry(propertyId);
        if (index < 0)
        {
            stream = null;
            return false;
        }

        PstPcEntry entry = _entries[index];
        stream = IsOutOfLine(entry)
            ? _context.OpenHnidStream(_heap, entry.RawValue)
            : PstLtpContext.OpenMemoryStream(Materialize(entry).RawData);
        return true;
    }

    /// <summary>
    /// Attempts to retrieve a property's value, resolving its payload.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <param name="value">When this method returns <see langword="true" />, the property value.</param>
    /// <returns><see langword="true" /> when the property is present.</returns>
    /// <exception cref="PstFileFormatException">The property's value reference does not resolve.</exception>
    public bool TryGetValue(ushort propertyId, out PstPropertyValue value)
    {
        int index = FindEntry(propertyId);
        if (index < 0)
        {
            value = default;
            return false;
        }

        value = Materialize(_entries[index]);
        return true;
    }

    /// <summary>
    /// Retrieves a property's value, resolving its payload.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <returns>The property value.</returns>
    /// <exception cref="PstFileException">The property is not present (<see cref="PstFileException.Error" /> is <see cref="PstFileError.PropertyNotFound" />).</exception>
    /// <exception cref="PstFileFormatException">The property's value reference does not resolve.</exception>
    public PstPropertyValue GetValue(ushort propertyId)
    {
        if (!TryGetValue(propertyId, out PstPropertyValue value))
        {
            throw new PstFileException(string.Format(
                CultureInfo.CurrentCulture, PstResourceStrings.IO_KeyNotFound_PstProperty, propertyId, new PstNodeId(_context.NodeId)), PstFileError.PropertyNotFound);
        }

        return value;
    }

    /// <summary>
    /// Enumerates the context's values in property-identifier order, resolving each payload as it is yielded.
    /// </summary>
    /// <returns>The value enumerator.</returns>
    /// <exception cref="PstFileFormatException">A value reference does not resolve.</exception>
    public IEnumerator<PstPropertyValue> GetEnumerator()
    {
        foreach (PstPcEntry entry in _entries)
            yield return Materialize(entry);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Finds a property's record index by binary search over the key-ordered records.
    /// </summary>
    /// <param name="propertyId">The property identifier.</param>
    /// <returns>The record index, or a negative value when absent.</returns>
    private int FindEntry(ushort propertyId)
    {
        int low = 0;
        int high = _entries.Count - 1;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            ushort key = _entries[middle].PropertyId;
            if (key == propertyId)
                return middle;

            if (key < propertyId)
                low = middle + 1;
            else
                high = middle - 1;
        }

        return -1;
    }

    /// <summary>
    /// Determines whether a record's value lives out of line behind an HNID (a variable-length value), as opposed to
    /// inline in the record, in a small fixed-width heap item, or as an unrecognized raw dword.
    /// </summary>
    /// <param name="entry">The property record.</param>
    /// <returns><see langword="true" /> when the record's raw value is an HNID to a variable-length payload.</returns>
    private static bool IsOutOfLine(PstPcEntry entry) =>
        !PstWireType.TryGetInlineSize(entry.WireType, out _)
            && !PstWireType.TryGetFixedHeapSize(entry.WireType, out _)
            && PstWireType.IsKnown(entry.WireType);

    /// <summary>
    /// Resolves a record into its value, materializing inline, heap-resident, or subnode-resident payloads per the
    /// wire type's storage classification.
    /// </summary>
    /// <param name="entry">The record to resolve.</param>
    /// <returns>The property value.</returns>
    /// <exception cref="PstFileFormatException">
    /// The value reference does not resolve, or a fixed-size payload is shorter than its declared width.
    /// </exception>
    private PstPropertyValue Materialize(PstPcEntry entry)
    {
        if (PstWireType.TryGetInlineSize(entry.WireType, out int inlineSize))
        {
            var dword = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(dword, entry.RawValue);
            return new PstPropertyValue(entry.PropertyId, entry.WireType, dword.AsMemory(0, inlineSize));
        }

        if (PstWireType.TryGetFixedHeapSize(entry.WireType, out int fixedSize))
        {
            byte[] payload = _context.ResolveHnidPayload(_heap, entry.RawValue);
            if (payload.Length < fixedSize)
            {
                throw new PstFileFormatException(string.Format(
                    CultureInfo.CurrentCulture, PstResourceStrings.Format_Invalid_PstPropertyContext, new PstNodeId(_context.NodeId)), PstFileError.InvalidPropertyContext);
            }

            return new PstPropertyValue(entry.PropertyId, entry.WireType, payload.AsMemory(0, fixedSize));
        }

        if (PstWireType.IsKnown(entry.WireType))
            return new PstPropertyValue(entry.PropertyId, entry.WireType, _context.ResolveHnidPayload(_heap, entry.RawValue));

        // An unrecognized wire type survives Compatible reads as its raw value dword, never chased as a reference.
        var raw = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(raw, entry.RawValue);
        return new PstPropertyValue(entry.PropertyId, entry.WireType, raw);
    }
}
