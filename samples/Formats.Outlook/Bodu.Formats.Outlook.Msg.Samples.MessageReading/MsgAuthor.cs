// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgAuthor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg.Samples.MessageReading;

/// <summary>
/// Writes a small, well-formed <c>.msg</c> file so the sample has something to read without shipping a binary
/// fixture — and, in doing so, shows what a <c>.msg</c> actually is.
/// </summary>
/// <remarks>
/// <para>
/// A <c>.msg</c> is an OLE2 compound file, which <c>Bodu.IO.Compound</c> can author, carrying MS-OXMSG streams and
/// storages inside it. This class writes only the four shapes the sample needs:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>__properties_version1.0</c> — a kind-specific header (32 bytes at the root, 8 inside a recipient or
/// attachment storage) followed by one 16-byte record per property: the 32-bit tag, 32 bits of flags, and either an
/// inline value or, for a variable-length type, the byte length of its value stream.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>__substg1.0_XXXXXXXX</c> — the value stream for one variable-length property, named for its tag in
/// eight uppercase hex digits. The size recorded above counts the string terminator that is not stored here.
/// </description>
/// </item>
/// <item>
/// <description><c>__recip_version1.0_#00000000</c> — one storage per recipient, numbered from zero.</description>
/// </item>
/// <item>
/// <description><c>__attach_version1.0_#00000000</c> — likewise for attachments.</description>
/// </item>
/// </list>
/// <para>
/// The reader is the thing being demonstrated; this writer exists only to produce its input. The library is
/// read-only by design, so there is no supported authoring API to use here — these are the raw layouts.
/// </para>
/// </remarks>
internal static class MsgAuthor
{
    /// <summary>The root property stream's header length, per MS-OXMSG.</summary>
    private const int RootHeaderSize = 32;

    /// <summary>The header length inside a recipient or attachment storage.</summary>
    private const int ChildHeaderSize = 8;

    /// <summary>The property flags a normal readable/writable property carries.</summary>
    private const uint ReadableWritable = 0x6;

    /// <summary>The MAPI type tag for a UTF-16LE string.</summary>
    private const ushort TypeUnicode = 0x001F;

    /// <summary>The MAPI type tag for a binary payload.</summary>
    private const ushort TypeBinary = 0x0102;

    /// <summary>The MAPI type tag for a 32-bit integer.</summary>
    private const ushort TypeInt32 = 0x0003;

    /// <summary>
    /// Writes a message with a subject, body, sender, two recipients, and one text attachment.
    /// </summary>
    /// <param name="path">The file path to write.</param>
    internal static void WriteSampleMessage(string path)
    {
        using var file = CompoundFile.Create(path);

        var root = new PropertyStorage(RootHeaderSize);
        root.AddUnicode(MapiPropertyIds.MessageClass, "IPM.Note");
        root.AddUnicode(MapiPropertyIds.Subject, "Quarterly figures");
        root.AddUnicode(MapiPropertyIds.NormalizedSubject, "Quarterly figures");
        root.AddUnicode(MapiPropertyIds.Body, "Numbers attached. The Sydney office is up 12% on the quarter.\r\n\r\n-- Dana");
        root.AddUnicode(MapiPropertyIds.SenderName, "Dana Whitfield");
        root.AddUnicode(MapiPropertyIds.SenderEmailAddress, "dana.whitfield@example.com");

        // Recipients and attachments are child storages, numbered from zero. The root header records
        // how many of each there are, which is what the reader enumerates.
        var to = new PropertyStorage(ChildHeaderSize);
        to.AddUnicode(MapiPropertyIds.DisplayName, "Priya Raman");
        to.AddUnicode(MapiPropertyIds.EmailAddress, "priya.raman@example.com");
        to.AddInt32(MapiPropertyIds.RecipientType, 1);   // 1 = To

        var cc = new PropertyStorage(ChildHeaderSize);
        cc.AddUnicode(MapiPropertyIds.DisplayName, "Accounts Team");
        cc.AddUnicode(MapiPropertyIds.EmailAddress, "accounts@example.com");
        cc.AddInt32(MapiPropertyIds.RecipientType, 2);   // 2 = Cc

        var attachment = new PropertyStorage(ChildHeaderSize);
        attachment.AddUnicode(MapiPropertyIds.AttachFilename, "q3.csv");
        attachment.AddUnicode(MapiPropertyIds.AttachLongFilename, "q3-summary.csv");
        attachment.AddInt32(MapiPropertyIds.AttachMethod, 1);   // 1 = the data is in AttachData
        attachment.AddBinary(
            MapiPropertyIds.AttachData,
            System.Text.Encoding.ASCII.GetBytes("office,revenue\r\nSydney,1240000\r\nMelbourne,980000\r\n"));

        root.RecipientCount = 2;
        root.AttachmentCount = 1;

        root.WriteTo(file.RootStorage);
        to.WriteTo(file.RootStorage.CreateStorage("__recip_version1.0_#00000000"));
        cc.WriteTo(file.RootStorage.CreateStorage("__recip_version1.0_#00000001"));
        attachment.WriteTo(file.RootStorage.CreateStorage("__attach_version1.0_#00000000"));

        file.Commit();
    }

    /// <summary>
    /// Accumulates the property records and value streams for one storage, then writes them.
    /// </summary>
    private sealed class PropertyStorage
    {
        /// <summary>The property stream's header length for this storage's kind.</summary>
        private readonly int _headerSize;

        /// <summary>The 16-byte property records, in insertion order.</summary>
        private readonly List<(uint Tag, ulong Value)> _records = new();

        /// <summary>The value streams the variable-length records point at.</summary>
        private readonly List<(string Name, byte[] Content)> _streams = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyStorage" /> class.
        /// </summary>
        /// <param name="headerSize">The property stream's header length for this storage's kind.</param>
        internal PropertyStorage(int headerSize) => _headerSize = headerSize;

        /// <summary>Gets or sets the number of recipient storages, recorded in the root header.</summary>
        internal uint RecipientCount { get; set; }

        /// <summary>Gets or sets the number of attachment storages, recorded in the root header.</summary>
        internal uint AttachmentCount { get; set; }

        /// <summary>
        /// Adds a UTF-16LE string property and its value stream.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="value">The string value.</param>
        internal void AddUnicode(ushort id, string value)
        {
            var tag = Tag(id, TypeUnicode);
            var bytes = System.Text.Encoding.Unicode.GetBytes(value);

            // The recorded size counts the two-byte terminator, which the stream itself does not store.
            _records.Add((tag, (uint)(bytes.Length + 2)));
            _streams.Add((SubstgName(tag), bytes));
        }

        /// <summary>
        /// Adds a binary property and its value stream.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="value">The payload.</param>
        internal void AddBinary(ushort id, byte[] value)
        {
            var tag = Tag(id, TypeBinary);
            _records.Add((tag, (uint)value.Length));
            _streams.Add((SubstgName(tag), value));
        }

        /// <summary>
        /// Adds a 32-bit integer property, whose value is inline in the record rather than in a stream.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="value">The integer value.</param>
        internal void AddInt32(ushort id, int value) =>
            _records.Add((Tag(id, TypeInt32), (uint)value));

        /// <summary>
        /// Writes the property stream and every value stream into a storage.
        /// </summary>
        /// <param name="storage">The storage to populate.</param>
        internal void WriteTo(CompoundStorage storage)
        {
            var payload = new byte[_headerSize + (_records.Count * 16)];
            Span<byte> span = payload;

            // Only the root and embedded-message headers carry the child counts.
            if (_headerSize > ChildHeaderSize)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(span[8..], RecipientCount);
                BinaryPrimitives.WriteUInt32LittleEndian(span[12..], AttachmentCount);
                BinaryPrimitives.WriteUInt32LittleEndian(span[16..], RecipientCount);
                BinaryPrimitives.WriteUInt32LittleEndian(span[20..], AttachmentCount);
            }

            for (int i = 0; i < _records.Count; i++)
            {
                var record = span.Slice(_headerSize + (i * 16), 16);
                BinaryPrimitives.WriteUInt32LittleEndian(record, _records[i].Tag);
                BinaryPrimitives.WriteUInt32LittleEndian(record[4..], ReadableWritable);
                BinaryPrimitives.WriteUInt64LittleEndian(record[8..], _records[i].Value);
            }

            storage.CreateStream("__properties_version1.0", payload);

            foreach (var (name, content) in _streams)
                storage.CreateStream(name, content);
        }

        /// <summary>
        /// Composes a 32-bit property tag from an identifier and a type.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="type">The MAPI type.</param>
        /// <returns>The raw tag.</returns>
        private static uint Tag(ushort id, ushort type) => ((uint)id << 16) | type;

        /// <summary>
        /// Composes the value-stream name for a tag: the prefix plus eight uppercase hex digits.
        /// </summary>
        /// <param name="tag">The raw tag.</param>
        /// <returns>The stream name, for example <c>__substg1.0_0037001F</c>.</returns>
        private static string SubstgName(uint tag) =>
            "__substg1.0_" + tag.ToString("X8", CultureInfo.InvariantCulture);
    }
}
