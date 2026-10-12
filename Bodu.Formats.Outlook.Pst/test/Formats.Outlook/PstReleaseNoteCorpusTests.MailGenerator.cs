// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.MailGenerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using Bodu.IO.Pst;
using Bodu.IO.Pst.Internal;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Runs the <c>Mail(...)</c> generator: a mail store modelled from the call's keys and written by
    /// <see cref="StoreModel" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The store object (node 0x21) holds PidTagDisplayName <c>storename</c> (default <c>Store</c>) unless
    /// <c>storeprops=none</c>, plus the <c>sprop</c> values; <c>store=false</c> leaves it out. The root folder (node
    /// 0x122) holds PidTagDisplayName <c>Root</c> and PidTagSubfolders true. The Inbox (<c>inbox</c>, default
    /// <c>Inbox</c>) is created first, then each <c>folder</c> path with its parents, then the <c>twin</c> and the
    /// <c>searchfolder</c>, with node identifiers <c>(x &lt;&lt; 5) | 0x02</c> (a search folder's type 0x03) for x from
    /// 0x401 in creation order.
    /// </para>
    /// <para>
    /// The Inbox holds <c>messages</c> messages (default 1), message i (1-based) being <c>((0x10000 + i) &lt;&lt; 5) |
    /// 0x04</c> with PidTagMessageClass <c>IPM.Note</c> and PidTagSubject <c>Message i</c>. The first message takes
    /// <c>subject</c>, <c>class</c>, <c>cp</c>, <c>prop</c>, <c>rcpt</c>, <c>att</c>, <c>atttable=empty</c>,
    /// <c>rcpttable=empty</c> and <c>subnodeindex=true</c>. Text is Unicode, or in the ANSI format 8-bit text in
    /// Windows-1252, or for the messages in the code page <c>cp</c> names.
    /// </para>
    /// </remarks>
    private static class MailGenerator
    {
        /// <summary>The code page of 8-bit text when a call names none.</summary>
        private const int DefaultCodePage = 1252;

        /// <summary>
        /// Runs a <c>Mail</c> call.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <returns>The file.</returns>
        /// <exception cref="FormatException">
        /// A key's value is malformed, or a first-message key is given with no message.
        /// </exception>
        public static GeneratedFile Build(GeneratorCall call)
        {
            PstFixtureBuilder file = InputGenerator.NewBuilder(call);
            bool ansi = file.Format == PstFileFormat.Ansi;
            bool includeStore = call.GetBoolean("store", true);
            string? storeProperties = call.Get("storeprops");
            string storeName = call.GetString("storename", "Store");
            IReadOnlyList<string> storeSpecs = call.GetAll("sprop");
            string inboxName = call.GetString("inbox", "Inbox");
            IReadOnlyList<string> folderPaths = call.GetAll("folder");
            IReadOnlyList<string> folderSpecs = call.GetAll("fprop");
            string? twinPath = call.Get("twin");
            string? searchFolder = call.Get("searchfolder");
            string? inboxParent = call.Get("inboxparent");
            IReadOnlyList<string> danglingRows = call.GetAll("danglingrows");
            bool contents = call.GetBoolean("contents", true);
            bool hierarchy = call.GetBoolean("hierarchy", true);
            int messageCount = call.GetInt32("messages", 1);
            string? subject = call.Get("subject");
            string? messageClass = call.Get("class");
            int codePage = call.GetInt32("cp", 0);
            IReadOnlyList<string> messageSpecs = call.GetAll("prop");
            IReadOnlyList<string> recipientSpecs = call.GetAll("rcpt");
            IReadOnlyList<string> attachmentSpecs = call.GetAll("att");
            string? attachmentTable = call.Get("atttable");
            string? recipientTable = call.Get("rcpttable");
            bool subnodeIndex = call.GetBoolean("subnodeindex", false);
            string? names = call.Get("names");
            string? nameGuids = call.Get("nameguids");
            string? nameEntries = call.Get("nameentries");
            string? nameStrings = call.Get("namestrings");
            if (storeProperties is not (null or "none")) throw new FormatException("gen: Mail: storeprops is none, or left out.");
            if (attachmentTable is not (null or "empty")) throw new FormatException("gen: Mail: atttable is empty, or left out.");
            if (recipientTable is not (null or "empty")) throw new FormatException("gen: Mail: rcpttable is empty, or left out.");
            if (messageCount < 1 && (subject ?? messageClass ?? attachmentTable ?? recipientTable) is not null)
                throw new FormatException("gen: Mail: the first message's keys are given, but messages=0 writes no message.");
            if (messageCount < 1 && (codePage != 0 || subnodeIndex || messageSpecs.Count + recipientSpecs.Count + attachmentSpecs.Count > 0))
                throw new FormatException("gen: Mail: the first message's keys are given, but messages=0 writes no message.");

            Encoding storeEncoding = EncodingFor(DefaultCodePage);
            Encoding messageEncoding = EncodingFor(codePage == 0 ? DefaultCodePage : codePage);
            PropertyModel Text(ushort id, string text, Encoding encoding) =>
                ansi ? PropertyModel.String8(id, text, encoding) : PropertyModel.Unicode(id, text);

            var model = new StoreModel(file)
            {
                IncludeStoreObject = includeStore,
                MatrixHeapLimit = call.GetInt32("matrixheap", InputGenerator.MaxHeapItem),
                PadMatrixBlocks = call.GetBoolean("padrows", false),
            };

            if (storeProperties is null)
                model.StoreProperties.Add(Text(0x3001, storeName, storeEncoding));

            foreach (string spec in storeSpecs)
                Replace(model.StoreProperties, PropertyModel.Parse(spec, storeEncoding));

            model.Root.Properties.Add(Text(0x3001, "Root", storeEncoding));
            model.Root.Properties.Add(PropertyModel.Boolean(0x360A, true));

            // Folders are numbered in creation order: the Inbox, each folder path with its parents, the twin, then the
            // search folder.
            var folders = new List<FolderModel>();
            var byPath = new Dictionary<string, FolderModel>(StringComparer.Ordinal);
            uint nextIndex = 0x401;
            FolderModel AddFolder(FolderModel parent, string name)
            {
                var folder = new FolderModel((nextIndex++ << 5) | 0x02);
                folder.Properties.Add(Text(0x3001, name, storeEncoding));
                parent.Children.Add(folder);
                folders.Add(folder);
                return folder;
            }

            FolderModel FolderAt(string path)
            {
                if (byPath.TryGetValue(path, out FolderModel? existing))
                    return existing;

                int slash = path.LastIndexOf('/');
                FolderModel parent = slash < 0 ? model.Root : FolderAt(path[..slash]);
                FolderModel folder = AddFolder(parent, slash < 0 ? path : path[(slash + 1)..]);
                byPath[path] = folder;
                return folder;
            }

            FolderModel inbox = FolderAt(inboxName);
            foreach (string path in folderPaths)
                FolderAt(path);

            if (twinPath is not null)
            {
                FolderAt(twinPath);
                int slash = twinPath.LastIndexOf('/');
                AddFolder(slash < 0 ? model.Root : FolderAt(twinPath[..slash]), slash < 0 ? twinPath : twinPath[(slash + 1)..]);
            }

            if (searchFolder is not null)
            {
                var search = new FolderModel((nextIndex++ << 5) | 0x03);
                search.Properties.Add(Text(0x3001, searchFolder, storeEncoding));
                model.Root.Children.Add(search);
            }

            if (inboxParent is not null)
                inbox.ParentOverride = (uint)GeneratorCall.ParseInteger(inboxParent);

            foreach (string row in danglingRows)
                inbox.DanglingContentsRows.Add((uint)GeneratorCall.ParseInteger(row));

            inbox.WriteContentsTable = contents;
            inbox.WriteHierarchyTable = hierarchy;

            for (int i = 1; i <= messageCount; i++)
            {
                var message = new MessageModel(((0x10000u + (uint)i) << 5) | 0x04);
                message.Properties.Add(Text(0x001A, "IPM.Note", messageEncoding));
                message.Properties.Add(Text(0x0037, "Message " + i.ToString(CultureInfo.InvariantCulture), messageEncoding));
                inbox.Messages.Add(message);
            }

            foreach (FolderModel folder in folders)
            {
                folder.Properties.Add(PropertyModel.Int32(0x3602, folder.Messages.Count));
                folder.Properties.Add(PropertyModel.Boolean(0x360A, folder.Children.Count > 0));
            }

            if (messageCount >= 1)
            {
                MessageModel first = inbox.Messages[0];
                if (subject is not null)
                    Replace(first.Properties, Text(0x0037, subject, messageEncoding));

                if (messageClass is not null)
                    Replace(first.Properties, Text(0x001A, messageClass, messageEncoding));

                if (codePage != 0)
                    Replace(first.Properties, PropertyModel.Int32(0x3FFD, codePage));

                foreach (string spec in messageSpecs)
                    Replace(first.Properties, PropertyModel.Parse(spec, messageEncoding));

                if (recipientSpecs.Count > 0 || recipientTable is not null)
                    first.Recipients = [.. recipientSpecs.Select(spec => ParseRecipient(spec, ansi, messageEncoding))];

                if (attachmentSpecs.Count > 0 || attachmentTable is not null)
                    first.Attachments = [.. attachmentSpecs.Select((spec, i) => ParseAttachment(spec, i, ansi, messageEncoding))];

                first.SubnodeIndex = subnodeIndex;
            }

            foreach (string spec in folderSpecs)
                Replace(inbox.Properties, PropertyModel.Parse(spec, storeEncoding));

            if (names is not null)
                model.NameMap = ParseNames(names);

            if (nameGuids is not null)
                (model.NameMap ??= new NameMapModel()).Guids = ParseStream(nameGuids);

            if (nameEntries is not null)
                (model.NameMap ??= new NameMapModel()).Entries = ParseStream(nameEntries);

            if (nameStrings is not null)
                (model.NameMap ??= new NameMapModel()).Strings = ParseStream(nameStrings);

            model.Write();
            return new GeneratedFile(file.Build(), file.Format);
        }

        /// <summary>
        /// Gets the encoding of a code page, through <see cref="CodePagesEncodingProvider" /> for the Windows code
        /// pages.
        /// </summary>
        /// <param name="codePage">The code page.</param>
        /// <returns>
        /// The encoding, or Latin-1 where the runtime offers none (UTF-7, code page 65000, among them).
        /// </returns>
        private static Encoding EncodingFor(int codePage)
        {
            try
            {
                return CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.GetEncoding(codePage);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                return Encoding.Latin1;
            }
        }

        /// <summary>
        /// Replaces the property with an identifier, or adds it.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <param name="property">The new property.</param>
        private static void Replace(List<PropertyModel> properties, PropertyModel property)
        {
            properties.RemoveAll(existing => existing.Id == property.Id);
            properties.Add(property);
        }

        /// <summary>
        /// Reads the bytes of a name-to-id stream: hexadecimal digits, or <c>fill:n</c>.
        /// </summary>
        /// <param name="text">The value.</param>
        /// <returns>The stream bytes.</returns>
        private static byte[] ParseStream(string text) =>
            text.StartsWith("fill:", StringComparison.Ordinal)
                ? InputGenerator.Fill((int)GeneratorCall.ParseInteger(text[5..]))
                : GeneratorCall.ParseHex(text);

        /// <summary>
        /// Reads an <c>rcpt</c> specification: <c>type~name~email~addrtype[~prop=...]</c>.
        /// </summary>
        /// <param name="spec">The specification.</param>
        /// <param name="ansi">Whether the file is in the ANSI format, which stores text as 8-bit strings.</param>
        /// <param name="encoding">The message's encoding.</param>
        /// <returns>
        /// The recipient: PidTagRecipientType, PidTagDisplayName, PidTagEmailAddress and PidTagAddressType, each left out
        /// when its field is empty, then the <c>prop</c> values.
        /// </returns>
        /// <exception cref="FormatException">A field after the fourth is not <c>prop=</c>.</exception>
        private static RecipientModel ParseRecipient(string spec, bool ansi, Encoding encoding)
        {
            string[] parts = spec.Split('~');
            var recipient = new RecipientModel();
            if (parts[0].Length > 0)
                recipient.Properties.Add(PropertyModel.Int32(0x0C15, (int)GeneratorCall.ParseInteger(parts[0])));

            ushort[] ids = [0x3001, 0x3003, 0x3002];
            for (int i = 0; i < ids.Length && i + 1 < parts.Length; i++)
            {
                if (parts[i + 1].Length > 0)
                    recipient.Properties.Add(ansi ? PropertyModel.String8(ids[i], parts[i + 1], encoding) : PropertyModel.Unicode(ids[i], parts[i + 1]));
            }

            foreach (string extra in parts.Skip(4))
            {
                if (!extra.StartsWith("prop=", StringComparison.Ordinal)) throw new FormatException($"gen: rcpt: unknown field '{extra}'.");

                recipient.Properties.Add(PropertyModel.Parse(extra[5..], encoding));
            }

            return recipient;
        }

        /// <summary>
        /// Reads an <c>att</c> specification: <c>method~longname~content[~key=value...]</c>.
        /// </summary>
        /// <param name="spec">The specification.</param>
        /// <param name="index">The attachment's position among its message's attachments.</param>
        /// <param name="ansi">Whether the file is in the ANSI format, which stores text as 8-bit strings.</param>
        /// <param name="encoding">The message's encoding.</param>
        /// <returns>The attachment, its object being subnode <c>((0x401 + index) &lt;&lt; 5) | 0x05</c>.</returns>
        /// <exception cref="FormatException">A field is malformed or unknown.</exception>
        /// <remarks>
        /// <para>
        /// The method is PidTagAttachMethod (<c>none</c> leaves it out) and the long name PidTagAttachLongFilename. For
        /// method 5 the content is the subject of an embedded message, subnode 0x8024 of the attachment object with
        /// PidTagMessageClass <c>IPM.Note</c>; otherwise it is PidTagAttachDataBinary as UTF-8 text, <c>0x</c> hex or
        /// <c>fill:n</c>, or <c>-</c> for none.
        /// </para>
        /// <para>
        /// The keys are <c>short</c>, <c>size</c>, <c>mime</c>, <c>cid</c>, <c>data=subnode</c>, <c>node=false</c>,
        /// <c>embednode=false</c>, <c>embeddata=none</c>, <c>object=hex</c>, <c>ercpt</c> and <c>eatt</c> (a recipient
        /// and an attachment of the embedded message, <c>|</c> standing for <c>~</c>), <c>prop</c> (repeatable),
        /// <c>rowprop</c> (a cell of the attachment-table row) and <c>eprop</c> (a property of the embedded message,
        /// replacing one with the same identifier).
        /// </para>
        /// </remarks>
        private static AttachmentModel ParseAttachment(string spec, int index, bool ansi, Encoding encoding)
        {
            string[] parts = spec.Split('~');
            var attachment = new AttachmentModel(((0x401u + (uint)index) << 5) | 0x05);
            string method = parts[0];
            string longName = parts.Length > 1 ? parts[1] : string.Empty;
            string content = parts.Length > 2 ? parts[2] : "-";
            PropertyModel Text(ushort id, string value) =>
                ansi ? PropertyModel.String8(id, value, encoding) : PropertyModel.Unicode(id, value);

            if (method != "none")
                attachment.Properties.Add(PropertyModel.Int32(0x3705, (int)GeneratorCall.ParseInteger(method)));

            if (longName.Length > 0)
                attachment.Properties.Add(Text(0x3707, longName));

            PropertyModel? data = null;
            if (method == "5")
            {
                var embedded = new MessageModel((0x401u << 5) | 0x04);
                embedded.Properties.Add(Text(0x001A, "IPM.Note"));
                embedded.Properties.Add(Text(0x0037, content));
                attachment.Embedded = embedded;
            }
            else if (content != "-")
            {
                byte[] payload = content.StartsWith("0x", StringComparison.Ordinal)
                    ? GeneratorCall.ParseHex(content)
                    : content.StartsWith("fill:", StringComparison.Ordinal)
                        ? InputGenerator.Fill((int)GeneratorCall.ParseInteger(content[5..]))
                        : Encoding.UTF8.GetBytes(content);
                data = PropertyModel.Binary(0x3701, payload);
                attachment.Properties.Add(data);
            }

            foreach (string extra in parts.Skip(3))
            {
                int equals = extra.IndexOf('=', StringComparison.Ordinal);
                string key = equals < 0 ? extra : extra[..equals];
                string value = equals < 0 ? string.Empty : extra[(equals + 1)..];
                MessageModel? inner = attachment.Embedded;
                switch (key)
                {
                    case "short":
                        attachment.Properties.Add(Text(0x3704, value));
                        break;

                    case "size":
                        attachment.Properties.Add(PropertyModel.Int32(0x0E20, (int)GeneratorCall.ParseInteger(value)));
                        break;

                    case "mime":
                        attachment.Properties.Add(Text(0x370E, value));
                        break;

                    case "cid":
                        attachment.Properties.Add(Text(0x3712, value));
                        break;

                    case "data" when value == "subnode" && data is not null:
                        data.Placement = ValuePlacement.Subnode;
                        break;

                    case "node" when value == "false":
                        attachment.WriteNode = false;
                        break;

                    case "embednode" when value == "false" && inner is not null:
                        attachment.WriteEmbeddedNode = false;
                        break;

                    case "embeddata" when value == "none" && inner is not null:
                        attachment.EmbeddedHasData = false;
                        break;

                    case "object":
                        attachment.ObjectData = GeneratorCall.ParseHex(value);
                        break;

                    case "ercpt" when inner is not null:
                        (inner.Recipients ??= []).Add(ParseRecipient(value.Replace('|', '~'), ansi, encoding));
                        break;

                    case "eatt" when inner is not null:
                        inner.Attachments ??= [];
                        inner.Attachments.Add(ParseAttachment(value.Replace('|', '~'), inner.Attachments.Count, ansi, encoding));
                        break;

                    case "prop":
                        attachment.Properties.Add(PropertyModel.Parse(value, encoding));
                        break;

                    case "rowprop":
                        attachment.RowProperties.Add(PropertyModel.Parse(value, encoding));
                        break;

                    case "eprop" when inner is not null:
                        Replace(inner.Properties, PropertyModel.Parse(value, encoding));
                        break;

                    default:
                        throw new FormatException($"gen: att: unknown field '{extra}'.");
                }
            }

            return attachment;
        }

        /// <summary>
        /// Reads the <c>names</c> entries, separated by <c>;</c>, into the three name-to-id streams.
        /// </summary>
        /// <param name="spec">The entries.</param>
        /// <returns>The streams.</returns>
        /// <exception cref="FormatException">An entry is malformed.</exception>
        /// <remarks>
        /// <para>
        /// An entry is <c>n~guid~id[~idx]</c> (a numeric name), <c>s~guid~name[~idx]</c> (a string name, appended to
        /// the string stream as a 4-byte length, UTF-16LE and zero padding to a multiple of 4) or
        /// <c>o~guid~offset[~idx]</c> (a string entry naming that string-stream offset). The guid is <c>1</c>
        /// (PS_MAPI), <c>2</c> (PS_PUBLIC_STRINGS), <c>#k</c> (wGuid k as given) or a GUID, appended to the GUID stream
        /// in order of first use and numbered from 3.
        /// </para>
        /// <para>
        /// Each NAMEID is dwPropertyID (the number or the string offset), <c>wGuid &lt;&lt; 1 | N</c> and wPropIdx
        /// (idx, or the entry's position).
        /// </para>
        /// </remarks>
        private static NameMapModel ParseNames(string spec)
        {
            var guids = new List<Guid>();
            var entries = new List<byte>();
            var strings = new List<byte>();
            string[] items = spec.Split(';');
            for (int position = 0; position < items.Length; position++)
            {
                string[] parts = items[position].Split('~');
                if (parts.Length is < 3 or > 4 || parts[0] is not ("n" or "s" or "o"))
                    throw new FormatException($"gen: names: the entry '{items[position]}' is not n~guid~id, s~guid~name or o~guid~offset, with an optional ~idx.");

                bool isString = parts[0] != "n";
                int guidIndex = parts[1] switch
                {
                    "1" => 1,
                    "2" => 2,
                    ['#', .. string raw] => (int)GeneratorCall.ParseInteger(raw),
                    string text => 3 + GuidIndex(guids, Guid.Parse(text, CultureInfo.InvariantCulture)),
                };

                uint value;
                if (parts[0] == "s")
                {
                    value = (uint)strings.Count;
                    byte[] name = Encoding.Unicode.GetBytes(parts[2]);
                    strings.AddRange(Little32((uint)name.Length));
                    strings.AddRange(name);
                    while (strings.Count % 4 != 0)
                        strings.Add(0);
                }
                else
                {
                    value = (uint)GeneratorCall.ParseInteger(parts[2]);
                }

                var propertyIndex = parts.Length > 3 ? (ushort)GeneratorCall.ParseInteger(parts[3]) : (ushort)position;
                var nameId = new byte[8];
                BinaryPrimitives.WriteUInt32LittleEndian(nameId, value);
                BinaryPrimitives.WriteUInt16LittleEndian(nameId.AsSpan(4), (ushort)((guidIndex << 1) | (isString ? 1 : 0)));
                BinaryPrimitives.WriteUInt16LittleEndian(nameId.AsSpan(6), propertyIndex);
                entries.AddRange(nameId);
            }

            return new NameMapModel
            {
                Guids = [.. guids.SelectMany(guid => guid.ToByteArray())],
                Entries = [.. entries],
                Strings = [.. strings],
            };
        }

        /// <summary>
        /// Gets a GUID's position in the GUID stream, appending it on first use.
        /// </summary>
        /// <param name="guids">The GUID stream's GUIDs, in order.</param>
        /// <param name="guid">The GUID.</param>
        /// <returns>The zero-based position.</returns>
        private static int GuidIndex(List<Guid> guids, Guid guid)
        {
            int index = guids.IndexOf(guid);
            if (index >= 0)
                return index;

            guids.Add(guid);
            return guids.Count - 1;
        }

        /// <summary>
        /// Writes a 32-bit value little-endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The four bytes.</returns>
        private static byte[] Little32(uint value)
        {
            var bytes = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);

            return bytes;
        }
    }
}
