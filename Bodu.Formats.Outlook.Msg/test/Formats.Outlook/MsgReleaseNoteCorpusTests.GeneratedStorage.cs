// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgReleaseNoteCorpusTests.GeneratedStorage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public sealed partial class MsgReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one storage of a message described by a <c>gen:Msg(...)</c> specification: its property records,
    /// streams and child storages, and, for the named-property mapping storage, the entries its three streams hold.
    /// </summary>
    /// <remarks>
    /// The property stream is composed byte by byte (the header, the 16-byte records in the order given, then the tail)
    /// and handed to <see cref="MsgFixtureBuilder" /> as a raw stream, so that a row states every byte that differs
    /// from the well-formed default.
    /// </remarks>
    private sealed class GeneratedStorage
    {
        /// <summary>The flags a typed property's record carries: readable and writable.</summary>
        internal const uint DefaultFlags = 0x6;

        /// <summary>The GUID stream of a named-property mapping storage.</summary>
        private readonly List<byte> _guids = [];

        /// <summary>The entry stream of a named-property mapping storage.</summary>
        private readonly List<byte> _entries = [];

        /// <summary>The string stream of a named-property mapping storage.</summary>
        private readonly List<byte> _strings = [];

        /// <summary>Whether the named-property streams have been added to <see cref="Streams" />.</summary>
        private bool _nameIdFinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedStorage" /> class.
        /// </summary>
        /// <param name="kind">The kind of storage, which sets the size of its property-stream header.</param>
        /// <param name="isNameId">Whether the storage is the named-property mapping storage.</param>
        internal GeneratedStorage(MsgPropertyStreamKind kind, bool isNameId = false)
        {
            Kind = kind;
            IsNameId = isNameId;
        }

        /// <summary>
        /// Gets the kind of storage, which sets the size of its property-stream header.
        /// </summary>
        /// <value>The storage kind.</value>
        internal MsgPropertyStreamKind Kind { get; }

        /// <summary>
        /// Gets a value indicating whether the storage is the named-property mapping storage.
        /// </summary>
        /// <value><see langword="true" /> for <c>__nameid_version1.0</c>, which has no property stream.</value>
        internal bool IsNameId { get; }

        /// <summary>
        /// Gets the property records, in the order the specification gives them.
        /// </summary>
        /// <value>The records.</value>
        internal List<MsgPropertyEntry> Records { get; } = [];

        /// <summary>
        /// Gets the streams, in the order they were added.
        /// </summary>
        /// <value>Each stream's name and content.</value>
        internal List<(string Name, byte[] Content)> Streams { get; } = [];

        /// <summary>
        /// Gets the child storages, in the order they were added.
        /// </summary>
        /// <value>Each child's name and description.</value>
        internal List<(string Name, GeneratedStorage Child)> Children { get; } = [];

        /// <summary>
        /// Gets the whole-file transforms, which only the root storage carries, in the order written.
        /// </summary>
        /// <value>Each transform's kind (<c>cut</c>, <c>unallocated</c>, <c>reverse</c> or <c>move</c>) and argument.</value>
        internal List<(string Kind, string Argument)> Transforms { get; } = [];

        /// <summary>
        /// Gets or sets a value indicating whether the storage has no property stream.
        /// </summary>
        /// <value><see langword="true" /> when the <c>noprops</c> item was given.</value>
        internal bool NoProps { get; set; }

        /// <summary>
        /// Gets or sets the property stream to write instead of the composed one.
        /// </summary>
        /// <value>The bytes of a <c>props=</c> item, or <see langword="null" />.</value>
        internal byte[]? Props { get; set; }

        /// <summary>
        /// Gets or sets the property-stream header to write instead of the default one.
        /// </summary>
        /// <value>The bytes of a <c>hdr=</c> item, or <see langword="null" />.</value>
        internal byte[]? Header { get; set; }

        /// <summary>
        /// Gets or sets the bytes written after the last property record.
        /// </summary>
        /// <value>The bytes of a <c>tail=</c> item; empty by default.</value>
        internal byte[] Tail { get; set; } = [];

        /// <summary>
        /// Gets the number of entries the named-property entry stream holds.
        /// </summary>
        /// <value>The entry count, which is the default property index of the next entry.</value>
        internal int EntryCount { get; private set; }

        /// <summary>
        /// Adds a stream, or replaces the one of the same name, compared case-insensitively as compound-file names are.
        /// </summary>
        /// <param name="name">The stream name.</param>
        /// <param name="content">The stream content.</param>
        internal void SetStream(string name, byte[] content)
        {
            int index = Streams.FindIndex(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                Streams[index] = (name, content);
            else
                Streams.Add((name, content));
        }

        /// <summary>
        /// Counts the child storages whose names start with a prefix.
        /// </summary>
        /// <param name="prefix">The name prefix.</param>
        /// <returns>The number of matching children.</returns>
        internal int CountChildren(string prefix) =>
            Children.Count(c => c.Name.StartsWith(prefix, StringComparison.Ordinal));

        /// <summary>
        /// Sets the size field, the low 32 bits of the value field, of the last record with a tag.
        /// </summary>
        /// <param name="tag">The record's tag.</param>
        /// <param name="size">The size.</param>
        /// <exception cref="FormatException">No record has the tag.</exception>
        internal void SetSize(uint tag, uint size)
        {
            int index = FindLastRecord(tag, "size");
            Records[index] = Records[index] with { Raw = (Records[index].Raw & 0xFFFFFFFF00000000UL) | size };
        }

        /// <summary>
        /// Sets the flags of the last record with a tag.
        /// </summary>
        /// <param name="tag">The record's tag.</param>
        /// <param name="flags">The flags.</param>
        /// <exception cref="FormatException">No record has the tag.</exception>
        internal void SetFlags(uint tag, uint flags)
        {
            int index = FindLastRecord(tag, "flags");
            Records[index] = Records[index] with { Flags = flags };
        }

        /// <summary>
        /// Appends a GUID to the named-property GUID stream.
        /// </summary>
        /// <param name="guid">The GUID, written in the MS-OXCDATA byte order.</param>
        internal void AddGuid(Guid guid) =>
            _guids.AddRange(guid.ToByteArray());

        /// <summary>
        /// Appends a numeric entry to the named-property entry stream.
        /// </summary>
        /// <param name="lid">The numeric identifier.</param>
        /// <param name="guidIndex">The GUID index: 1 for <c>PS_MAPI</c>, 2 for <c>PS_PUBLIC_STRINGS</c>, 3 and up for the GUID stream.</param>
        /// <param name="propertyIndex">The property index, or <see langword="null" /> for the number of entries before it.</param>
        internal void AddNumericEntry(uint lid, int guidIndex, int? propertyIndex) =>
            AddEntry(lid, guidIndex, isString: false, propertyIndex);

        /// <summary>
        /// Appends a string entry, writing its name to the string stream at the next 4-byte boundary.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="guidIndex">The GUID index: 1 for <c>PS_MAPI</c>, 2 for <c>PS_PUBLIC_STRINGS</c>, 3 and up for the GUID stream.</param>
        /// <param name="propertyIndex">The property index, or <see langword="null" /> for the number of entries before it.</param>
        /// <remarks>
        /// The string stream is padded with zeros to a multiple of four, then gets the name's UTF-16 byte count (four
        /// bytes) and the name in UTF-16LE, and is padded with zeros to a multiple of four again.
        /// </remarks>
        internal void AddStringEntry(string name, int guidIndex, int? propertyIndex)
        {
            PadStrings();
            uint offset = (uint)_strings.Count;
            byte[] text = Encoding.Unicode.GetBytes(name);
            var length = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(length, (uint)text.Length);
            _strings.AddRange(length);
            _strings.AddRange(text);
            PadStrings();

            AddEntry(offset, guidIndex, isString: true, propertyIndex);
        }

        /// <summary>
        /// Adds the GUID, entry and string streams of a named-property mapping storage, each unless a <c>stream:</c> item
        /// already supplied a stream of that name.
        /// </summary>
        internal void FinishNameId()
        {
            if (_nameIdFinished)
                return;

            _nameIdFinished = true;
            AddIfAbsent(MsgStreamNames.GetSubstgStreamName(0x00020102), [.. _guids]);
            AddIfAbsent(MsgStreamNames.GetSubstgStreamName(0x00030102), [.. _entries]);
            AddIfAbsent(MsgStreamNames.GetSubstgStreamName(0x00040102), [.. _strings]);
        }

        /// <summary>
        /// Composes the property stream: the header (default or given), the records, then the tail.
        /// </summary>
        /// <returns>The property-stream bytes, or the <c>props=</c> bytes when they were given.</returns>
        internal byte[] PropertiesStream()
        {
            if (Props is not null)
                return Props;

            byte[] header = Header ?? DefaultHeader();
            var payload = new byte[header.Length + (Records.Count * 16) + Tail.Length];
            header.CopyTo(payload, 0);
            for (int i = 0; i < Records.Count; i++)
            {
                Span<byte> record = payload.AsSpan(header.Length + (i * 16), 16);
                BinaryPrimitives.WriteUInt32LittleEndian(record, Records[i].Tag);
                BinaryPrimitives.WriteUInt32LittleEndian(record[4..], Records[i].Flags);
                BinaryPrimitives.WriteUInt64LittleEndian(record[8..], Records[i].Raw);
            }

            Tail.CopyTo(payload, header.Length + (Records.Count * 16));
            return payload;
        }

        /// <summary>
        /// Converts the storage, and its children in turn, into a fixture builder.
        /// </summary>
        /// <returns>A builder that writes the property stream (unless omitted), then the streams, then the children.</returns>
        internal MsgFixtureBuilder ToBuilder()
        {
            MsgFixtureBuilder builder = new MsgFixtureBuilder(Kind).OmitPropertiesStream();
            if (IsNameId)
                FinishNameId();
            else if (!NoProps)
                builder.AddRawStream(MsgStreamNames.PropertiesStreamName, PropertiesStream());

            foreach ((string name, byte[] content) in Streams)
                builder.AddRawStream(name, content);

            foreach ((string name, GeneratedStorage child) in Children)
                builder.AddStorage(name, child.ToBuilder());

            return builder;
        }

        /// <summary>
        /// Composes the default property-stream header for the storage's kind.
        /// </summary>
        /// <returns>
        /// The header: zero bytes, except that a root or embedded-message header holds, at offsets 8 and 16, the number
        /// of recipient storages, and at offsets 12 and 20 the number of attachment storages.
        /// </returns>
        private byte[] DefaultHeader()
        {
            var header = new byte[MsgPropertyStreamReader.GetHeaderSize(Kind)];
            if (Kind != MsgPropertyStreamKind.RecipientOrAttachment)
            {
                uint recipients = (uint)CountChildren(MsgStreamNames.RecipientStoragePrefix);
                uint attachments = (uint)CountChildren(MsgStreamNames.AttachmentStoragePrefix);
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(8), recipients);
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), attachments);
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(16), recipients);
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(20), attachments);
            }

            return header;
        }

        /// <summary>
        /// Finds the last record with a tag.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <param name="item">The item that needs the record, for the error message.</param>
        /// <returns>The record's index in <see cref="Records" />.</returns>
        /// <exception cref="FormatException">No record has the tag.</exception>
        private int FindLastRecord(uint tag, string item)
        {
            int index = Records.FindLastIndex(r => r.Tag == tag);
            return index >= 0 ? index : throw new FormatException($"{item}: there is no record with tag {tag:X8}.");
        }

        /// <summary>
        /// Pads the named-property string stream with zeros to a multiple of four bytes.
        /// </summary>
        private void PadStrings()
        {
            while (_strings.Count % 4 != 0)
                _strings.Add(0);
        }

        /// <summary>
        /// Adds a stream unless one of that name, compared case-insensitively, is already present.
        /// </summary>
        /// <param name="name">The stream name.</param>
        /// <param name="content">The stream content.</param>
        private void AddIfAbsent(string name, byte[] content)
        {
            if (!Streams.Any(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                Streams.Insert(0, (name, content));
        }

        /// <summary>
        /// Appends an 8-byte entry to the named-property entry stream.
        /// </summary>
        /// <param name="idOrOffset">The numeric identifier, or the name's offset in the string stream.</param>
        /// <param name="guidIndex">The GUID index.</param>
        /// <param name="isString">Whether the entry names a string property.</param>
        /// <param name="propertyIndex">The property index, or <see langword="null" /> for the number of entries before it.</param>
        private void AddEntry(uint idOrOffset, int guidIndex, bool isString, int? propertyIndex)
        {
            var entry = new byte[8];
            BinaryPrimitives.WriteUInt32LittleEndian(entry, idOrOffset);
            BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(4), (ushort)((guidIndex << 1) | (isString ? 1 : 0)));
            BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(6), (ushort)(propertyIndex ?? EntryCount));
            _entries.AddRange(entry);
            EntryCount++;
        }
    }
}
