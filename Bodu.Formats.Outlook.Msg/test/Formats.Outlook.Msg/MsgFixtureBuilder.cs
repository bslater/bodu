// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgFixtureBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Builds synthetic <c>.msg</c> compound files for tests, composing MS-OXMSG property streams, value streams, and
/// child storages with byte-exact control over every structure the reader decodes.
/// </summary>
/// <remarks>
/// The byte layouts follow MS-OXMSG: the property stream carries a kind-specific header (root 32 bytes, embedded
/// message 24, recipient/attachment 8) followed by 16-byte records; a variable-length value lives in its
/// <c>__substg1.0_</c> stream with the record's size field including the string terminator; a multi-valued
/// variable-length property stores a length stream (4-byte entries for the string types, 8-byte for binary) plus one
/// <c>-XXXXXXXX</c> element stream per value; a multi-valued fixed-length property packs its elements contiguously in
/// a single stream.
/// </remarks>
internal sealed class MsgFixtureBuilder
{
    /// <summary>The default property flags (readable | writable).</summary>
    private const uint DefaultFlags = 0x6;

    /// <summary>The storage kind this builder emits, which selects the property-stream header shape.</summary>
    private readonly MsgPropertyStreamKind _kind;

    /// <summary>The 16-byte property records in insertion order.</summary>
    private readonly List<MsgPropertyEntry> _entries = new();

    /// <summary>The value streams to create alongside the property stream.</summary>
    private readonly List<(string Name, byte[] Content)> _streams = new();

    /// <summary>The child storages (recipients, attachments, embedded messages) to create.</summary>
    private readonly List<(string Name, MsgFixtureBuilder Child)> _storages = new();

    /// <summary>Whether the property stream itself is suppressed (for malformed-container cases).</summary>
    private bool _omitPropertiesStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="MsgFixtureBuilder" /> class.
    /// </summary>
    /// <param name="kind">The storage kind the builder emits; the root by default.</param>
    internal MsgFixtureBuilder(MsgPropertyStreamKind kind = MsgPropertyStreamKind.Root)
    {
        _kind = kind;
    }

    /// <summary>
    /// Creates a builder carrying a minimal, well-formed message: a Unicode subject and body.
    /// </summary>
    /// <returns>The builder, for further configuration.</returns>
    internal static MsgFixtureBuilder CreateMinimal() =>
        new MsgFixtureBuilder()
            .AddUnicode(MapiPropertyIds.Subject, "Test subject")
            .AddUnicode(MapiPropertyIds.Body, "Test body");

    /// <summary>
    /// Adds a fixed-length property record with an inline value.
    /// </summary>
    /// <param name="tag">The raw 32-bit property tag.</param>
    /// <param name="raw">The 8-byte inline value, little-endian.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddFixedEntry(uint tag, ulong raw)
    {
        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, raw));
        return this;
    }

    /// <summary>
    /// Adds a Unicode string property: the record plus its UTF-16LE value stream.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="value">The string value.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddUnicode(ushort id, string value)
    {
        byte[] bytes = System.Text.Encoding.Unicode.GetBytes(value);
        uint tag = ComposeTag(id, MapiPropertyType.Unicode);
        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, (uint)(bytes.Length + 2)));
        _streams.Add((MsgStreamNames.GetSubstgStreamName(tag), bytes));
        return this;
    }

    /// <summary>
    /// Adds a code-page string property: the record plus its encoded value stream.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="value">The string value.</param>
    /// <param name="codePage">The code page used to encode the value.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddString8(ushort id, string value, int codePage)
    {
        byte[] bytes = MsgEncodingResolver.GetEncoding(codePage, null).GetBytes(value);
        uint tag = ComposeTag(id, MapiPropertyType.String8);
        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, (uint)(bytes.Length + 1)));
        _streams.Add((MsgStreamNames.GetSubstgStreamName(tag), bytes));
        return this;
    }

    /// <summary>
    /// Adds a binary property: the record plus its value stream.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="value">The payload bytes.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddBinary(ushort id, byte[] value)
    {
        uint tag = ComposeTag(id, MapiPropertyType.Binary);
        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, (uint)value.Length));
        _streams.Add((MsgStreamNames.GetSubstgStreamName(tag), value));
        return this;
    }

    /// <summary>
    /// Adds a GUID property: the record plus its 16-byte value stream.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="value">The GUID value.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddGuidValue(ushort id, Guid value)
    {
        uint tag = ComposeTag(id, MapiPropertyType.Guid);
        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, 16));
        _streams.Add((MsgStreamNames.GetSubstgStreamName(tag), value.ToByteArray()));
        return this;
    }

    /// <summary>
    /// Adds a multi-valued Unicode string property: the record, the 4-byte-entry length stream, and one element
    /// stream per value.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="values">The string values.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddMultiValueUnicode(ushort id, params string[] values)
    {
        uint tag = ComposeTag(id, MapiPropertyType.Unicode) | 0x1000;
        var lengths = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++)
        {
            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(values[i]);
            BinaryPrimitives.WriteUInt32LittleEndian(lengths.AsSpan(i * 4), (uint)(bytes.Length + 2));
            _streams.Add((MsgStreamNames.GetMultiValueElementStreamName(tag, i), bytes));
        }

        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, (uint)lengths.Length));
        _streams.Add((MsgStreamNames.GetSubstgStreamName(tag), lengths));
        return this;
    }

    /// <summary>
    /// Adds a multi-valued binary property: the record, the 8-byte-entry length stream, and one element stream per
    /// value.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="values">The payloads.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddMultiValueBinary(ushort id, params byte[][] values)
    {
        uint tag = ComposeTag(id, MapiPropertyType.Binary) | 0x1000;
        var lengths = new byte[values.Length * 8];
        for (int i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(lengths.AsSpan(i * 8), (uint)values[i].Length);
            _streams.Add((MsgStreamNames.GetMultiValueElementStreamName(tag, i), values[i]));
        }

        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, (uint)lengths.Length));
        _streams.Add((MsgStreamNames.GetSubstgStreamName(tag), lengths));
        return this;
    }

    /// <summary>
    /// Adds a multi-valued 32-bit integer property: the record plus a single packed value stream.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="values">The integer values.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddMultiValueInt32(ushort id, params int[] values)
    {
        uint tag = ComposeTag(id, MapiPropertyType.Int32) | 0x1000;
        var packed = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(packed.AsSpan(i * 4), values[i]);

        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, (uint)packed.Length));
        _streams.Add((MsgStreamNames.GetSubstgStreamName(tag), packed));
        return this;
    }

    /// <summary>
    /// Adds a raw stream verbatim, for malformed-structure scenarios.
    /// </summary>
    /// <param name="name">The stream name.</param>
    /// <param name="content">The stream payload.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddRawStream(string name, byte[] content)
    {
        _streams.Add((name, content));
        return this;
    }

    /// <summary>
    /// Adds a raw property record without any accompanying value stream.
    /// </summary>
    /// <param name="tag">The raw property tag.</param>
    /// <param name="raw">The 8-byte value-or-size field.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddEntryWithoutStream(uint tag, ulong raw)
    {
        _entries.Add(new MsgPropertyEntry(tag, DefaultFlags, raw));
        return this;
    }

    /// <summary>
    /// Adds a child storage built by a nested builder (a recipient, attachment, or embedded message).
    /// </summary>
    /// <param name="name">The storage name.</param>
    /// <param name="child">The nested builder describing the storage's contents.</param>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder AddStorage(string name, MsgFixtureBuilder child)
    {
        _storages.Add((name, child));
        return this;
    }

    /// <summary>
    /// Suppresses the property stream, producing a compound file that is not a valid message.
    /// </summary>
    /// <returns>This builder.</returns>
    internal MsgFixtureBuilder OmitPropertiesStream()
    {
        _omitPropertiesStream = true;
        return this;
    }

    /// <summary>
    /// Builds the compound file and returns it as a seekable stream positioned at the start.
    /// </summary>
    /// <returns>The container stream.</returns>
    internal MemoryStream Build()
    {
        MemoryStream container = new();
        using (var file = CompoundFile.Create(container, leaveOpen: true))
        {
            PopulateStorage(file.RootStorage);
            file.Commit();
        }

        container.Position = 0;
        return container;
    }

    /// <summary>
    /// Builds only the property-stream payload for this builder's kind, without a container.
    /// </summary>
    /// <returns>The property-stream bytes: header followed by 16-byte records.</returns>
    internal byte[] BuildPropertiesPayload()
    {
        int headerSize = MsgPropertyStreamReader.GetHeaderSize(_kind);
        var payload = new byte[headerSize + (_entries.Count * 16)];
        Span<byte> span = payload;

        if (_kind != MsgPropertyStreamKind.RecipientOrAttachment)
        {
            uint recipientCount = 0;
            uint attachmentCount = 0;
            foreach ((string name, _) in _storages)
            {
                if (name.StartsWith(MsgStreamNames.RecipientStoragePrefix, StringComparison.Ordinal))
                    recipientCount++;
                else if (name.StartsWith(MsgStreamNames.AttachmentStoragePrefix, StringComparison.Ordinal))
                    attachmentCount++;
            }

            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), recipientCount);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(12), attachmentCount);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(16), recipientCount);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(20), attachmentCount);
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            Span<byte> record = span.Slice(headerSize + (i * 16), 16);
            BinaryPrimitives.WriteUInt32LittleEndian(record, _entries[i].Tag);
            BinaryPrimitives.WriteUInt32LittleEndian(record.Slice(4), _entries[i].Flags);
            BinaryPrimitives.WriteUInt64LittleEndian(record.Slice(8), _entries[i].Raw);
        }

        return payload;
    }

    /// <summary>
    /// Composes a raw tag from an identifier and base type.
    /// </summary>
    /// <param name="id">The property identifier.</param>
    /// <param name="type">The base property type.</param>
    /// <returns>The raw 32-bit tag.</returns>
    private static uint ComposeTag(ushort id, MapiPropertyType type) =>
        ((uint)id << 16) | (ushort)type;

    /// <summary>
    /// Writes this builder's property stream, value streams, and child storages into a container storage.
    /// </summary>
    /// <param name="storage">The writable storage to populate.</param>
    private void PopulateStorage(CompoundStorage storage)
    {
        if (!_omitPropertiesStream)
            storage.CreateStream(MsgStreamNames.PropertiesStreamName, BuildPropertiesPayload());

        foreach ((string name, byte[] content) in _streams)
            storage.CreateStream(name, content);

        foreach ((string name, MsgFixtureBuilder child) in _storages)
            child.PopulateStorage(storage.CreateStorage(name));
    }
}
