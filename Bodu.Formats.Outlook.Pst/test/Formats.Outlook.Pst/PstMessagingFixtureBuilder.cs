// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstMessagingFixtureBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Pst.Internal;

namespace Bodu.Formats.Outlook.Pst;

/// <summary>
/// Authors a complete synthetic mail store by composing the container fixture builders: a store object, a root folder
/// with a hierarchy table, a user folder with a contents table, and messages carrying recipient and attachment tables,
/// attachment objects, and an embedded message — the messaging structures the reference corpus cannot pin content
/// assertions on.
/// </summary>
/// <remarks>
/// <para>
/// The fixture is the content oracle for the messaging-layer tests: every string, code page, multi-value, and
/// attachment payload it writes is exposed as a constant the tests assert against. The reference corpus remains the
/// structural oracle for real writer output.
/// </para>
/// <para>
/// The malformation knobs remove legitimate structure (<see cref="IncludeRecipientTable" />,
/// <see cref="IncludeEmbeddedMessageSubnode" />) or add invalid structure
/// (<see cref="IncludeDanglingAttachmentRow" />) so tests can drive the strict-versus-tolerant contract without
/// patching bytes.
/// </para>
/// </remarks>
internal sealed class PstMessagingFixtureBuilder
{
    /// <summary>The user folder's node identifier (a normal folder, index <c>0x40</c>).</summary>
    internal const uint InboxNodeId = 0x802;

    /// <summary>The full message's node identifier (a normal message, index <c>0x41</c>).</summary>
    internal const uint MessageNodeId = 0x824;

    /// <summary>The plain message's node identifier (a normal message with no subnodes, index <c>0x42</c>).</summary>
    internal const uint PlainMessageNodeId = 0x844;

    /// <summary>The message's recipient-table subnode identifier (<c>NID_TYPE_RECIPIENT_TABLE</c>).</summary>
    internal const uint RecipientTableNodeId = 0x692;

    /// <summary>The message's attachment-table subnode identifier (<c>NID_TYPE_ATTACHMENT_TABLE</c>).</summary>
    internal const uint AttachmentTableNodeId = 0x671;

    /// <summary>The by-value attachment object's subnode identifier (<c>NID_TYPE_ATTACHMENT</c>).</summary>
    internal const uint ByValueAttachmentNodeId = 0xA05;

    /// <summary>The embedded-message attachment object's subnode identifier (<c>NID_TYPE_ATTACHMENT</c>).</summary>
    internal const uint EmbeddedAttachmentNodeId = 0xC05;

    /// <summary>An attachment-typed identifier the attachment table can reference without a backing subnode.</summary>
    internal const uint DanglingAttachmentNodeId = 0xB05;

    /// <summary>The embedded message's subnode identifier under the embedded attachment (a normal message).</summary>
    internal const uint EmbeddedMessageNodeId = 0xE04;

    /// <summary>The subnode identifier carrying a large, subnode-resident by-value attachment payload.</summary>
    internal const uint LargeAttachmentDataNodeId = 0x1025;

    /// <summary>The store object's display name.</summary>
    internal const string StoreDisplayName = "Synthetic Store";

    /// <summary>The user folder's display name.</summary>
    internal const string InboxDisplayName = "Inbox";

    /// <summary>The full message's stored subject, carrying the MS-PST subject-prefix marker.</summary>
    internal const string StoredSubject = "\u0001\u0005RE: Quarterly numbers";

    /// <summary>The full message's subject after prefix normalization.</summary>
    internal const string NormalizedSubject = "RE: Quarterly numbers";

    /// <summary>The full message's sender display name.</summary>
    internal const string SenderName = "Avery Doyle";

    /// <summary>The full message's sender email address.</summary>
    internal const string SenderEmailAddress = "avery@example.com";

    /// <summary>The code page the full message declares (<c>windows-1251</c>).</summary>
    internal const int MessageCodePage = 1251;

    /// <summary>The full message's <c>PT_STRING8</c> body text, written in the declared code page.</summary>
    internal const string BodyText = "Привет, мир";

    /// <summary>The plain message's stored subject, with no prefix marker.</summary>
    internal const string PlainSubject = "Plain status note";

    /// <summary>The full message's HTML body text, stored as code-page bytes under <c>PidTagHtml</c>.</summary>
    internal const string HtmlBodyText = "<html><body>Квартальный отчёт</body></html>";

    /// <summary>The full message's RTF body text, stored MELA-wrapped under <c>PidTagRtfCompressed</c>.</summary>
    internal const string RtfBodyText = "{\\rtf1\\ansi Synthetic quarterly report}";

    /// <summary>The numeric name identifier the synthetic name-to-id map's first entry carries.</summary>
    internal const uint NamedNumericId = 0x00008888;

    /// <summary>The string name the synthetic name-to-id map's second entry carries.</summary>
    internal const string NamedStringName = "Keywords";

    /// <summary>The property identifier the numeric named entry maps to.</summary>
    internal const ushort NamedNumericPropertyId = 0x8000;

    /// <summary>The property identifier the string named entry maps to.</summary>
    internal const ushort NamedStringPropertyId = 0x8001;

    /// <summary>The <c>PS_PUBLIC_STRINGS</c> property-set identifier the string named entry is scoped by.</summary>
    internal static readonly Guid PublicStringsPropertySetId = new("00020329-0000-0000-C000-000000000046");

    /// <summary>The property-set identifier the numeric named entry is scoped by, stored in the GUID stream.</summary>
    internal static readonly Guid NamedPropertySetId = new("20000000-1111-2222-3333-444444444444");

    /// <summary>The property identifier the multi-valued Unicode fixture value uses.</summary>
    internal const ushort MvUnicodePropertyId = 0x6000;

    /// <summary>The property identifier the multi-valued Int32 fixture value uses.</summary>
    internal const ushort MvInt32PropertyId = 0x6001;

    /// <summary>The first recipient's display name.</summary>
    internal const string RecipientOneName = "Robin Osei";

    /// <summary>The first recipient's email address.</summary>
    internal const string RecipientOneEmail = "robin@example.com";

    /// <summary>The first recipient's address type.</summary>
    internal const string RecipientOneAddressType = "SMTP";

    /// <summary>The second recipient's display name; its row omits the address-type cell.</summary>
    internal const string RecipientTwoName = "Sam Kealoha";

    /// <summary>The second recipient's email address.</summary>
    internal const string RecipientTwoEmail = "sam@example.com";

    /// <summary>The by-value attachment's long file name.</summary>
    internal const string AttachmentLongFileName = "quarterly-report.pdf";

    /// <summary>The by-value attachment's short (8.3) file name.</summary>
    internal const string AttachmentShortFileName = "QUARTE~1.PDF";

    /// <summary>The by-value attachment's content identifier.</summary>
    internal const string AttachmentContentId = "part1.report@example.com";

    /// <summary>The by-value attachment's MIME tag.</summary>
    internal const string AttachmentMimeTag = "application/pdf";

    /// <summary>The embedded message's stored subject.</summary>
    internal const string EmbeddedSubject = "Embedded status update";

    /// <summary>The embedded message's sender display name.</summary>
    internal const string EmbeddedSenderName = "Nested Sender";

    /// <summary>The embedded message's <c>PT_STRING8</c> body, decoding only under the inherited code page.</summary>
    internal const string EmbeddedBodyText = "Вложение";

    /// <summary>The <c>PT_LONG</c> wire type.</summary>
    private const ushort Int32Type = 0x0003;

    /// <summary>The <c>PT_BOOLEAN</c> wire type.</summary>
    private const ushort BooleanType = 0x000B;

    /// <summary>The <c>PT_OBJECT</c> wire type.</summary>
    private const ushort ObjectType = 0x000D;

    /// <summary>The <c>PT_STRING8</c> wire type.</summary>
    private const ushort String8Type = 0x001E;

    /// <summary>The <c>PT_UNICODE</c> wire type.</summary>
    private const ushort UnicodeType = 0x001F;

    /// <summary>The <c>PT_BINARY</c> wire type.</summary>
    private const ushort BinaryType = 0x0102;

    /// <summary>The <c>PT_MV_LONG</c> wire type.</summary>
    private const ushort MvInt32Type = 0x1003;

    /// <summary>The <c>PT_MV_UNICODE</c> wire type.</summary>
    private const ushort MvUnicodeType = 0x101F;

    /// <summary>
    /// Gets the multi-valued Unicode fixture elements.
    /// </summary>
    /// <value>The element strings, in stored order.</value>
    internal static string[] MvUnicodeValues { get; } = ["alpha", "beta järn"];

    /// <summary>
    /// Gets the multi-valued Int32 fixture elements.
    /// </summary>
    /// <value>The element values, in stored order.</value>
    internal static int[] MvInt32Values { get; } = [3, 5, 8];

    /// <summary>
    /// Gets the by-value attachment's content payload.
    /// </summary>
    /// <value>The payload bytes.</value>
    internal static byte[] AttachmentContent { get; } =
        Encoding.ASCII.GetBytes("%PDF-1.7 synthetic attachment payload");

    /// <summary>
    /// Gets or sets a value indicating whether the full message carries its recipient-table subnode.
    /// </summary>
    /// <value><see langword="true" /> by default.</value>
    internal bool IncludeRecipientTable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the full message carries its attachment-table subnode and attachment
    /// objects.
    /// </summary>
    /// <value><see langword="true" /> by default.</value>
    internal bool IncludeAttachmentTable { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the embedded-message attachment carries its nested message subnode.
    /// </summary>
    /// <value>
    /// <see langword="true" /> by default; clearing it produces an <c>afEmbeddedMessage</c> attachment with no message
    /// object behind it.
    /// </value>
    internal bool IncludeEmbeddedMessageSubnode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the attachment table carries an extra row whose identifier references
    /// no subnode.
    /// </summary>
    /// <value><see langword="false" /> by default.</value>
    internal bool IncludeDanglingAttachmentRow { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the store carries the message-store object node (<c>0x21</c>).
    /// </summary>
    /// <value><see langword="true" /> by default.</value>
    internal bool IncludeStoreObject { get; set; } = true;

    /// <summary>
    /// Gets or sets the code page the user folder declares through <c>PidTagMessageCodepage</c>.
    /// </summary>
    /// <value><see langword="null" /> (no declaration) by default.</value>
    internal int? InboxCodePage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the full message declares its own <c>PidTagMessageCodepage</c>.
    /// </summary>
    /// <value><see langword="true" /> by default; when cleared, the message's code-page strings rely on inheritance.</value>
    internal bool MessageDeclaresCodePage { get; set; } = true;

    /// <summary>
    /// Gets or sets a replacement for the by-value attachment's declared <c>PidTagAttachMethod</c>.
    /// </summary>
    /// <value><see langword="null" /> to declare <see cref="OutlookAttachmentMethod.ByValue" />.</value>
    internal uint? AttachMethodOverride { get; set; }

    /// <summary>
    /// Gets or sets the number of <c>XBLOCK</c>s (each 1021 references to one 8 KB block) behind a large,
    /// subnode-resident by-value attachment payload.
    /// </summary>
    /// <value><c>0</c> by default, keeping the payload heap-resident and small.</value>
    internal int LargeAttachmentXBlocks { get; set; }

    /// <summary>
    /// Gets the logical length of the large attachment payload <see cref="LargeAttachmentXBlocks" /> produces.
    /// </summary>
    /// <value>The payload length in bytes.</value>
    internal long LargeAttachmentLength =>
        (long)LargeAttachmentXBlocks * 1021 * PstFixtureBuilder.MaxBlockPayload;

    /// <summary>
    /// Gets or sets a value indicating whether the store carries the name-to-id map node.
    /// </summary>
    /// <value><see langword="true" /> by default.</value>
    internal bool IncludeNameToIdMap { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the name-to-id map's entry stream is truncated to a length that is not
    /// a multiple of the eight-byte record size.
    /// </summary>
    /// <value><see langword="false" /> by default.</value>
    internal bool TruncateNameMapEntryStream { get; set; }

    /// <summary>
    /// Gets or sets a replacement for the name-to-id map's string stream, for malformed-content tests.
    /// </summary>
    /// <value><see langword="null" /> to emit the well-formed stream.</value>
    internal byte[]? NameMapStringStreamOverride { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the full message's compressed-RTF payload is truncated below the
    /// 16-byte MS-OXRTFCP header.
    /// </summary>
    /// <value><see langword="false" /> by default.</value>
    internal bool TruncateRtfPayload { get; set; }

    /// <summary>
    /// Assembles the synthetic mail store and returns it as a seekable stream positioned at its start.
    /// </summary>
    /// <returns>The container stream.</returns>
    internal MemoryStream BuildStream()
    {
        var file = new PstFixtureBuilder();

        if (IncludeStoreObject)
            AddStoreObject(file);
        if (IncludeNameToIdMap)
            AddNameToIdMap(file);

        AddRootFolder(file);
        AddInbox(file);
        AddFullMessage(file);
        AddPlainMessage(file);

        return file.BuildStream();
    }

    /// <summary>
    /// Adds the store object node with its display name.
    /// </summary>
    /// <param name="file">The container builder.</param>
    private static void AddStoreObject(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();
        uint nameHid = ltp.AddItem(Encoding.Unicode.GetBytes(StoreDisplayName));

        _ = ltp.AddPropertyContext((MapiPropertyIds.DisplayName, UnicodeType, nameHid));
        _ = ltp.AddHeapNode(file, 0x21);
    }

    /// <summary>
    /// Adds the name-to-id map node: a numeric entry scoped by the GUID-stream identifier mapping to
    /// <see cref="NamedNumericPropertyId" />, and a string entry scoped by <c>PS_PUBLIC_STRINGS</c> mapping to
    /// <see cref="NamedStringPropertyId" />.
    /// </summary>
    /// <param name="file">The container builder.</param>
    private void AddNameToIdMap(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();

        // Entry 0: numeric name, GUID index 3 (the GUID stream's first identifier). Entry 1: string name at string
        // stream offset 0, GUID index 2 (PS_PUBLIC_STRINGS). The property index is the entry's position.
        var entries = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(entries, NamedNumericId);
        BinaryPrimitives.WriteUInt16LittleEndian(entries.AsSpan(4), 3 << 1);
        BinaryPrimitives.WriteUInt16LittleEndian(entries.AsSpan(6), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(entries.AsSpan(8), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(entries.AsSpan(12), (2 << 1) | 1);
        BinaryPrimitives.WriteUInt16LittleEndian(entries.AsSpan(14), 1);
        if (TruncateNameMapEntryStream)
            entries = entries[..12];

        byte[] nameBytes = Encoding.Unicode.GetBytes(NamedStringName);
        var strings = new byte[4 + nameBytes.Length];
        BinaryPrimitives.WriteInt32LittleEndian(strings, nameBytes.Length);
        nameBytes.CopyTo(strings, 4);
        if (NameMapStringStreamOverride is not null)
            strings = NameMapStringStreamOverride;

        uint guidsHid = ltp.AddItem(NamedPropertySetId.ToByteArray());
        uint entriesHid = ltp.AddItem(entries);
        uint stringsHid = ltp.AddItem(strings);

        _ = ltp.AddPropertyContext(
            (0x0001, Int32Type, 251),
            (0x0002, BinaryType, guidsHid),
            (0x0003, BinaryType, entriesHid),
            (0x0004, BinaryType, stringsHid));
        _ = ltp.AddHeapNode(file, 0x61);
    }

    /// <summary>
    /// Builds the <c>PidTagRtfCompressed</c> payload: the raw RTF text behind a <c>MELA</c> (uncompressed) header,
    /// or a truncated header when the knob is set.
    /// </summary>
    /// <returns>The payload bytes.</returns>
    private byte[] BuildRtfPayload()
    {
        byte[] rtf = Encoding.ASCII.GetBytes(RtfBodyText);
        var payload = new byte[16 + rtf.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(payload, (uint)(rtf.Length + 12));
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(4), (uint)rtf.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(8), 0x414C454D);
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(12), 0);
        rtf.CopyTo(payload, 16);

        return TruncateRtfPayload ? payload[..8] : payload;
    }

    /// <summary>
    /// Adds the root folder and its hierarchy table referencing the user folder.
    /// </summary>
    /// <param name="file">The container builder.</param>
    private static void AddRootFolder(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();
        uint nameHid = ltp.AddItem(Encoding.Unicode.GetBytes("Root Container"));

        _ = ltp.AddPropertyContext(
            (MapiPropertyIds.DisplayName, UnicodeType, nameHid),
            (MapiPropertyIds.Subfolders, BooleanType, 1));
        _ = ltp.AddHeapNode(file, 0x122);

        // The root's hierarchy table: one row whose identifier is the user folder's node identifier.
        AddNodeReferenceTable(file, 0x12D, InboxNodeId);
    }

    /// <summary>
    /// Adds the user folder, its declared counts, and its contents table referencing both messages.
    /// </summary>
    /// <param name="file">The container builder.</param>
    private void AddInbox(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();
        uint nameHid = ltp.AddItem(Encoding.Unicode.GetBytes(InboxDisplayName));
        uint classHid = ltp.AddItem(Encoding.Unicode.GetBytes("IPF.Note"));

        var records = new List<(ushort PropertyId, ushort WireType, uint RawValue)>
        {
            (MapiPropertyIds.DisplayName, UnicodeType, nameHid),
            (MapiPropertyIds.ContainerClass, UnicodeType, classHid),
            (MapiPropertyIds.ContentCount, Int32Type, 2),
            (MapiPropertyIds.ContentUnreadCount, Int32Type, 1),
            (MapiPropertyIds.Subfolders, BooleanType, 0),
        };
        if (InboxCodePage is int codePage)
            records.Add((MapiPropertyIds.MessageCodepage, Int32Type, (uint)codePage));

        _ = ltp.AddPropertyContext([.. records]);
        _ = ltp.AddHeapNode(file, InboxNodeId);

        AddNodeReferenceTable(file, 0x80E, MessageNodeId, PlainMessageNodeId);
    }

    /// <summary>
    /// Adds the full message: its property context, and — per the knobs — the recipient table, attachment table,
    /// attachment objects, and embedded message wired as subnodes.
    /// </summary>
    /// <param name="file">The container builder.</param>
    private void AddFullMessage(PstFixtureBuilder file)
    {
        var subnodes = new List<(uint NodeId, ulong DataBlockId, ulong SubnodeBlockId)>();

        if (IncludeRecipientTable)
            subnodes.Add((RecipientTableNodeId, AddRecipientTable(file), 0));

        if (IncludeAttachmentTable)
        {
            subnodes.Add((AttachmentTableNodeId, AddAttachmentTable(file), 0));
            (ulong attachmentData, ulong attachmentSubnodes) = AddByValueAttachment(file);
            subnodes.Add((ByValueAttachmentNodeId, attachmentData, attachmentSubnodes));

            ulong embeddedSubnodeBlockId = IncludeEmbeddedMessageSubnode
                ? file.AddSubnodeLeafBlock((EmbeddedMessageNodeId, AddEmbeddedMessage(file), 0))
                : 0;
            subnodes.Add((EmbeddedAttachmentNodeId, AddEmbeddedAttachment(file), embeddedSubnodeBlockId));
        }

        ulong subnodeBlockId = subnodes.Count > 0
            ? file.AddSubnodeLeafBlock([.. subnodes.OrderBy(static s => s.NodeId)])
            : 0;

        var ltp = new PstLtpFixtureBuilder();
        uint subjectHid = ltp.AddItem(Encoding.Unicode.GetBytes(StoredSubject));
        uint senderHid = ltp.AddItem(Encoding.Unicode.GetBytes(SenderName));
        uint emailHid = ltp.AddItem(Encoding.Unicode.GetBytes(SenderEmailAddress));
        uint classHid = ltp.AddItem(Encoding.Unicode.GetBytes("IPM.Note"));
        uint bodyHid = ltp.AddItem(GetCodePageBytes(BodyText));
        uint htmlHid = ltp.AddItem(GetCodePageBytes(HtmlBodyText));
        uint rtfHid = ltp.AddItem(BuildRtfPayload());
        uint mvUnicodeHid = ltp.AddItem(BuildMvUnicodePayload(MvUnicodeValues));
        uint mvInt32Hid = ltp.AddItem(BuildMvInt32Payload(MvInt32Values));

        var records = new List<(ushort PropertyId, ushort WireType, uint RawValue)>
        {
            (MapiPropertyIds.Subject, UnicodeType, subjectHid),
            (MapiPropertyIds.SenderName, UnicodeType, senderHid),
            (MapiPropertyIds.SenderEmailAddress, UnicodeType, emailHid),
            (MapiPropertyIds.MessageClass, UnicodeType, classHid),
            (MapiPropertyIds.Body, String8Type, bodyHid),
            (MapiPropertyIds.Html, BinaryType, htmlHid),
            (MapiPropertyIds.RtfCompressed, BinaryType, rtfHid),
            (MapiPropertyIds.HasAttachments, BooleanType, 1),
            (MvUnicodePropertyId, MvUnicodeType, mvUnicodeHid),
            (MvInt32PropertyId, MvInt32Type, mvInt32Hid),
        };
        if (MessageDeclaresCodePage)
            records.Add((MapiPropertyIds.MessageCodepage, Int32Type, MessageCodePage));

        _ = ltp.AddPropertyContext([.. records]);
        _ = ltp.AddHeapNode(file, MessageNodeId, subnodeBlockId);
    }

    /// <summary>
    /// Adds the plain message: a property context only, with no subnode tree.
    /// </summary>
    /// <param name="file">The container builder.</param>
    private static void AddPlainMessage(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();
        uint subjectHid = ltp.AddItem(Encoding.Unicode.GetBytes(PlainSubject));
        uint classHid = ltp.AddItem(Encoding.Unicode.GetBytes("IPM.Note"));

        _ = ltp.AddPropertyContext(
            (MapiPropertyIds.Subject, UnicodeType, subjectHid),
            (MapiPropertyIds.MessageClass, UnicodeType, classHid));
        _ = ltp.AddHeapNode(file, PlainMessageNodeId);
    }

    /// <summary>
    /// Builds the recipient table's heap: two rows over five columns, the second row omitting its address-type cell.
    /// </summary>
    /// <param name="file">The container builder.</param>
    /// <returns>The heap's data-block identifier, for the subnode row.</returns>
    private static ulong AddRecipientTable(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();
        uint oneNameHid = ltp.AddItem(Encoding.Unicode.GetBytes(RecipientOneName));
        uint oneEmailHid = ltp.AddItem(Encoding.Unicode.GetBytes(RecipientOneEmail));
        uint oneTypeHid = ltp.AddItem(Encoding.Unicode.GetBytes(RecipientOneAddressType));
        uint twoNameHid = ltp.AddItem(Encoding.Unicode.GetBytes(RecipientTwoName));
        uint twoEmailHid = ltp.AddItem(Encoding.Unicode.GetBytes(RecipientTwoEmail));

        byte[] matrix =
        [
            .. RecipientRow(1, (int)OutlookRecipientType.To, oneNameHid, oneEmailHid, oneTypeHid, 0b1111_1000),
            .. RecipientRow(2, (int)OutlookRecipientType.Cc, twoNameHid, twoEmailHid, 0, 0b1111_0000),
        ];
        uint matrixHid = ltp.AddItem(matrix);

        _ = ltp.AddTableContext(
            [
                (ComposeTag(MapiPropertyIds.LtpRowId, Int32Type), 0, 4, 0),
                (ComposeTag(MapiPropertyIds.RecipientType, Int32Type), 4, 4, 1),
                (ComposeTag(MapiPropertyIds.DisplayName, UnicodeType), 8, 4, 2),
                (ComposeTag(MapiPropertyIds.EmailAddress, UnicodeType), 12, 4, 3),
                (ComposeTag(MapiPropertyIds.AddressType, UnicodeType), 16, 4, 4),
            ],
            endOffset4: 20,
            endOffset2: 20,
            endOffset1: 20,
            rowWidth: 21,
            rowsHnid: matrixHid,
            (1, 0),
            (2, 1));

        return AddHeapBlocks(file, ltp);
    }

    /// <summary>
    /// Builds the attachment table's heap: one row per attachment object, the row identifier carrying the object's
    /// subnode identifier.
    /// </summary>
    /// <param name="file">The container builder.</param>
    /// <returns>The heap's data-block identifier, for the subnode row.</returns>
    private ulong AddAttachmentTable(PstFixtureBuilder file)
    {
        var rows = new List<byte[]>
        {
            AttachmentRow(ByValueAttachmentNodeId, 0),
            AttachmentRow(EmbeddedAttachmentNodeId, 1),
        };
        if (IncludeDanglingAttachmentRow)
            rows.Add(AttachmentRow(DanglingAttachmentNodeId, 2));

        var ltp = new PstLtpFixtureBuilder();
        uint matrixHid = ltp.AddItem([.. rows.SelectMany(static r => r)]);

        _ = ltp.AddTableContext(
            [
                (ComposeTag(MapiPropertyIds.LtpRowId, Int32Type), 0, 4, 0),
                (ComposeTag(MapiPropertyIds.AttachNumber, Int32Type), 4, 4, 1),
            ],
            endOffset4: 8,
            endOffset2: 8,
            endOffset1: 8,
            rowWidth: 9,
            rowsHnid: matrixHid,
            [.. rows
                .Select(static (r, i) => ((ulong)BinaryPrimitives.ReadUInt32LittleEndian(r), (uint)i))
                .OrderBy(static e => e.Item1)]);

        return AddHeapBlocks(file, ltp);
    }

    /// <summary>
    /// Builds the by-value attachment object's property context, including its binary content payload — heap-resident
    /// by default, or a large subnode-resident data tree when <see cref="LargeAttachmentXBlocks" /> is set.
    /// </summary>
    /// <param name="file">The container builder.</param>
    /// <returns>The heap's data-block identifier and the attachment's subnode-tree block identifier (zero when none).</returns>
    private (ulong DataBlockId, ulong SubnodeBlockId) AddByValueAttachment(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();
        uint longNameHid = ltp.AddItem(Encoding.Unicode.GetBytes(AttachmentLongFileName));
        uint shortNameHid = ltp.AddItem(Encoding.Unicode.GetBytes(AttachmentShortFileName));
        uint contentIdHid = ltp.AddItem(Encoding.Unicode.GetBytes(AttachmentContentId));
        uint mimeHid = ltp.AddItem(Encoding.Unicode.GetBytes(AttachmentMimeTag));

        ulong subnodeBlockId = 0;
        uint dataHnid;
        uint declaredSize;
        if (LargeAttachmentXBlocks > 0)
        {
            const int LeafRefsPerXBlock = 1021;
            ulong leafId = file.AddDataBlock(new byte[PstFixtureBuilder.MaxBlockPayload]);
            uint xBlockLength = (uint)(LeafRefsPerXBlock * PstFixtureBuilder.MaxBlockPayload);
            ulong xBlockId = file.AddXBlock(xBlockLength, [.. Enumerable.Repeat(leafId, LeafRefsPerXBlock)]);
            ulong dataTreeId = LargeAttachmentXBlocks == 1
                ? xBlockId
                : file.AddXXBlock((uint)LargeAttachmentLength, [.. Enumerable.Repeat(xBlockId, LargeAttachmentXBlocks)]);

            subnodeBlockId = file.AddSubnodeLeafBlock((LargeAttachmentDataNodeId, dataTreeId, 0));
            dataHnid = LargeAttachmentDataNodeId;
            declaredSize = (uint)LargeAttachmentLength;
        }
        else
        {
            dataHnid = ltp.AddItem(AttachmentContent);
            declaredSize = (uint)AttachmentContent.Length;
        }

        _ = ltp.AddPropertyContext(
            (MapiPropertyIds.AttachMethod, Int32Type, AttachMethodOverride ?? (uint)OutlookAttachmentMethod.ByValue),
            (MapiPropertyIds.AttachLongFilename, UnicodeType, longNameHid),
            (MapiPropertyIds.AttachFilename, UnicodeType, shortNameHid),
            (MapiPropertyIds.AttachContentId, UnicodeType, contentIdHid),
            (MapiPropertyIds.AttachMimeTag, UnicodeType, mimeHid),
            (MapiPropertyIds.AttachSize, Int32Type, declaredSize),
            (MapiPropertyIds.AttachData, BinaryType, dataHnid));

        return (AddHeapBlocks(file, ltp), subnodeBlockId);
    }

    /// <summary>
    /// Builds the embedded-message attachment object's property context, its <c>PT_OBJECT</c> value carrying the
    /// nested message's identifier per MS-PST §2.4.6.3.
    /// </summary>
    /// <param name="file">The container builder.</param>
    /// <returns>The heap's data-block identifier, for the subnode row.</returns>
    private static ulong AddEmbeddedAttachment(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();

        // A PT_OBJECT property-context value is an HID of an eight-byte { Nid, ulSize } record.
        var objectRecord = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(objectRecord, EmbeddedMessageNodeId);
        uint objectHid = ltp.AddItem(objectRecord);

        _ = ltp.AddPropertyContext(
            (MapiPropertyIds.AttachMethod, Int32Type, (uint)OutlookAttachmentMethod.EmbeddedMessage),
            (MapiPropertyIds.AttachData, ObjectType, objectHid));

        return AddHeapBlocks(file, ltp);
    }

    /// <summary>
    /// Builds the embedded message's property context; its <c>PT_STRING8</c> body declares no code page, so it
    /// decodes only under the encoding inherited from the owning message.
    /// </summary>
    /// <param name="file">The container builder.</param>
    /// <returns>The heap's data-block identifier, for the subnode row.</returns>
    private static ulong AddEmbeddedMessage(PstFixtureBuilder file)
    {
        var ltp = new PstLtpFixtureBuilder();
        uint subjectHid = ltp.AddItem(Encoding.Unicode.GetBytes(EmbeddedSubject));
        uint senderHid = ltp.AddItem(Encoding.Unicode.GetBytes(EmbeddedSenderName));
        uint classHid = ltp.AddItem(Encoding.Unicode.GetBytes("IPM.Note"));
        uint bodyHid = ltp.AddItem(GetCodePageBytes(EmbeddedBodyText));

        _ = ltp.AddPropertyContext(
            (MapiPropertyIds.Subject, UnicodeType, subjectHid),
            (MapiPropertyIds.SenderName, UnicodeType, senderHid),
            (MapiPropertyIds.MessageClass, UnicodeType, classHid),
            (MapiPropertyIds.Body, String8Type, bodyHid));

        return AddHeapBlocks(file, ltp);
    }

    /// <summary>
    /// Adds a top-level table-context node whose rows only reference other nodes — the shape of hierarchy and
    /// contents tables.
    /// </summary>
    /// <param name="file">The container builder.</param>
    /// <param name="tableNodeId">The composed table node identifier.</param>
    /// <param name="referencedNodeIds">The node identifiers the rows carry, in row order.</param>
    private static void AddNodeReferenceTable(PstFixtureBuilder file, uint tableNodeId, params uint[] referencedNodeIds)
    {
        var ltp = new PstLtpFixtureBuilder();

        var matrix = new byte[referencedNodeIds.Length * 5];
        for (int i = 0; i < referencedNodeIds.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(matrix.AsSpan(i * 5), referencedNodeIds[i]);
            matrix[(i * 5) + 4] = 0b1000_0000;
        }

        uint matrixHid = ltp.AddItem(matrix);

        _ = ltp.AddTableContext(
            [(ComposeTag(MapiPropertyIds.LtpRowId, Int32Type), 0, 4, 0)],
            endOffset4: 4,
            endOffset2: 4,
            endOffset1: 4,
            rowWidth: 5,
            rowsHnid: matrixHid,
            [.. referencedNodeIds.Select(static (id, i) => ((ulong)id, (uint)i))]);

        _ = ltp.AddHeapNode(file, tableNodeId);
    }

    /// <summary>
    /// Adds a heap's blocks to the container as data blocks (with an <c>XBLOCK</c> when the heap spans several) and
    /// returns the identifier a subnode row references.
    /// </summary>
    /// <param name="file">The container builder.</param>
    /// <param name="ltp">The heap to emit.</param>
    /// <returns>The data-block identifier.</returns>
    private static ulong AddHeapBlocks(PstFixtureBuilder file, PstLtpFixtureBuilder ltp)
    {
        List<byte[]> blocks = ltp.BuildBlocks();

        return blocks.Count == 1
            ? file.AddDataBlock(blocks[0])
            : file.AddXBlock((uint)blocks.Sum(static b => b.Length), [.. blocks.Select(file.AddDataBlock)]);
    }

    /// <summary>
    /// Builds one 21-byte recipient row.
    /// </summary>
    /// <param name="rowId">The row identifier.</param>
    /// <param name="recipientType">The recipient-type cell value.</param>
    /// <param name="nameHid">The display-name cell's value reference.</param>
    /// <param name="emailHid">The email-address cell's value reference.</param>
    /// <param name="addressTypeHid">The address-type cell's value reference.</param>
    /// <param name="bitmap">The existence bitmap byte (bits are most-significant first).</param>
    /// <returns>The row bytes.</returns>
    private static byte[] RecipientRow(uint rowId, int recipientType, uint nameHid, uint emailHid, uint addressTypeHid, byte bitmap)
    {
        var row = new byte[21];
        BinaryPrimitives.WriteUInt32LittleEndian(row, rowId);
        BinaryPrimitives.WriteInt32LittleEndian(row.AsSpan(4), recipientType);
        BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(8), nameHid);
        BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(12), emailHid);
        BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(16), addressTypeHid);
        row[20] = bitmap;
        return row;
    }

    /// <summary>
    /// Builds one 9-byte attachment-table row.
    /// </summary>
    /// <param name="attachmentNodeId">The attachment object's subnode identifier, which is the row identifier.</param>
    /// <param name="attachNumber">The attachment-number cell value.</param>
    /// <returns>The row bytes.</returns>
    private static byte[] AttachmentRow(uint attachmentNodeId, int attachNumber)
    {
        var row = new byte[9];
        BinaryPrimitives.WriteUInt32LittleEndian(row, attachmentNodeId);
        BinaryPrimitives.WriteInt32LittleEndian(row.AsSpan(4), attachNumber);
        row[8] = 0b1100_0000;
        return row;
    }

    /// <summary>
    /// Builds a variable-size multi-value payload in the MS-PST count-plus-offset-table layout.
    /// </summary>
    /// <param name="values">The element strings, encoded UTF-16LE.</param>
    /// <returns>The payload bytes.</returns>
    private static byte[] BuildMvUnicodePayload(string[] values)
    {
        byte[][] elements = [.. values.Select(static v => Encoding.Unicode.GetBytes(v))];
        var payload = new byte[4 + (elements.Length * 4) + elements.Sum(static e => e.Length)];
        BinaryPrimitives.WriteInt32LittleEndian(payload, elements.Length);

        int offset = 4 + (elements.Length * 4);
        for (int i = 0; i < elements.Length; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4 + (i * 4)), offset);
            elements[i].CopyTo(payload, offset);
            offset += elements[i].Length;
        }

        return payload;
    }

    /// <summary>
    /// Builds a packed fixed-width multi-value payload.
    /// </summary>
    /// <param name="values">The element values.</param>
    /// <returns>The payload bytes.</returns>
    private static byte[] BuildMvInt32Payload(int[] values)
    {
        var payload = new byte[values.Length * 4];
        for (int i = 0; i < values.Length; i++)
            BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(i * 4), values[i]);

        return payload;
    }

    /// <summary>
    /// Encodes fixture text in the message code page the fixture declares.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>The code-page bytes.</returns>
    private static byte[] GetCodePageBytes(string text)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        return Encoding.GetEncoding(MessageCodePage).GetBytes(text);
    }

    /// <summary>
    /// Composes a 32-bit column tag from a property identifier and wire type.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <param name="wireType">The 16-bit wire type.</param>
    /// <returns>The column tag.</returns>
    private static uint ComposeTag(ushort propertyId, ushort wireType) =>
        ((uint)propertyId << 16) | wireType;
}
