// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Bodu.IO.Pst;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Holds Bodu.IO.Pst and Bodu.Formats.Outlook.Pst to the defect fixes other PST readers list in their release notes and
/// commit histories, as catalogued in <c>corpus/pst/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every runnable row's input is a <c>gen:</c> pipeline, built by <see cref="GenerateInput" />: a generator that writes
/// a PST file through the container test builders, then any transforms that patch the bytes, in the grammar
/// <c>corpus/pst/README.md</c> defines. The bytes are read on one of two surfaces, which the <c>Surface</c> option
/// selects: the mail store (<see cref="OutlookMailStore" />, the default), walked folder by folder, or the container
/// (<see cref="PstFile" />), listed node by node with an optional view of one node.
/// </para>
/// <para>
/// A rendering is a list of lines joined with LF, text escaped as <see cref="EscapeValue" /> describes and binary data
/// written by <see cref="Bytes" />; the README defines every line. A <c>parse</c> row passes when the rendering equals
/// the row's expected text, and a <c>reject</c> row when opening or rendering the input throws the exception the row
/// names.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "pst";

    /// <summary>The folder depth past which the store walk reports a cycle instead of descending further.</summary>
    private const int MaxFolderDepth = 64;

    /// <summary>The longest data a rendering writes in hexadecimal; longer data is written as a digest.</summary>
    private const int MaxHexBytes = 32;

    /// <summary>The longest body, in UTF-8 bytes, a rendering writes as text; a longer one is a digest.</summary>
    private const int MaxBodyBytes = 256;

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames =
    [
        "Surface", "ValidationLevel", "OmitUnresolvableValues", "DecompressRtf", "BlockCacheSize", "MaxNodeDataLength", "MaxDataTreeLeaves",
        "MaxEmbeddedMessageDepth", "MaxDecompressedRtfBytes", "MaxInlineAttachmentBytes", "Detail", "Names", "Node",
        "Subnode", "View", "Row", "Data",
    ];

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(PstReleaseNoteCorpusTests).Assembly));

    /// <summary>
    /// Gets the runnable rows of kind <c>parse</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> ParseRows =>
        RunnableRows("parse");

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c> that expect <see cref="PstFileFormatException" />.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> MalformedContainerRows =>
        RejectRows(nameof(PstFileFormatException));

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c> that expect <see cref="PstUnsupportedFormatException" />.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> UnsupportedVariantRows =>
        RejectRows(nameof(PstUnsupportedFormatException));

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c> on the Store surface that expect one of the container's exceptions,
    /// <see cref="PstFileFormatException" /> or <see cref="PstUnsupportedFormatException" />.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> StoreSurfaceContainerRows =>
        AllRows
            .Where(static fix => fix.IsRunnable
                && fix.Kind == "reject"
                && fix.Expected is nameof(PstFileFormatException) or nameof(PstUnsupportedFormatException)
                && RowOptions.Parse(fix.Options).Surface == "Store")
            .Select(static fix => new object[] { fix });

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c> that expect <see cref="OutlookPstFormatException" />.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> MalformedMailStoreRows =>
        RejectRows(nameof(OutlookPstFormatException));

    /// <summary>
    /// Gets every row of every embedded catalogue.
    /// </summary>
    /// <value>The rows, catalogue by catalogue in file order.</value>
    private static IEnumerable<ReleaseNoteFix> AllRows =>
        s_catalogs.Value.SelectMany(catalog => catalog.Rows);

    /// <summary>
    /// Builds the PST bytes a catalogue input describes.
    /// </summary>
    /// <param name="input">The decoded input: <c>gen:</c> followed by a generator pipeline.</param>
    /// <returns>The file bytes.</returns>
    /// <exception cref="FormatException">The input is not a well-formed pipeline of known generators.</exception>
    /// <remarks>
    /// The catalogue's <c>unit</c> tests build their models with this method, so that they read the same files the rows
    /// describe.
    /// </remarks>
    internal static byte[] GenerateInput(string input) =>
        InputGenerator.Generate(input);

    /// <summary>
    /// Renders an open mail store as a row on the Store surface does.
    /// </summary>
    /// <param name="store">The open store.</param>
    /// <param name="options">
    /// The row options, in the catalogue's <c>options</c> notation; empty for the defaults.
    /// </param>
    /// <returns>The rendering.</returns>
    /// <exception cref="FormatException">The options are malformed or name the Nodes surface.</exception>
    /// <remarks>
    /// The catalogue's <c>unit</c> tests compare stores opened in different ways through this method; the options that
    /// select reader settings do not apply, because the store is already open.
    /// </remarks>
    internal static string RenderStore(OutlookMailStore store, string options)
    {
        RowOptions parsed = RowOptions.Parse(options);
        if (parsed.Surface != "Store") throw new FormatException("A store is rendered on the Store surface.");

        return RenderStore(store, parsed);
    }

    /// <summary>
    /// Selects the runnable rows of one kind as test data.
    /// </summary>
    /// <param name="kind">The row kind.</param>
    /// <returns>
    /// One single-element argument array per <c>applies</c> or <c>dialect</c> row of <paramref name="kind" />.
    /// </returns>
    private static IEnumerable<object[]> RunnableRows(string kind) =>
        AllRows.Where(fix => fix.IsRunnable && fix.Kind == kind).Select(fix => new object[] { fix });

    /// <summary>
    /// Selects the runnable <c>reject</c> rows that expect one exception as test data.
    /// </summary>
    /// <param name="exceptionName">The exception's type name, as the rows' <c>expected</c> field writes it.</param>
    /// <returns>One single-element argument array per matching row.</returns>
    private static IEnumerable<object[]> RejectRows(string exceptionName) =>
        AllRows
            .Where(fix => fix.IsRunnable && fix.Kind == "reject" && fix.Expected == exceptionName)
            .Select(fix => new object[] { fix });

    /// <summary>
    /// Builds a row's input and renders it on the surface its options select.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    /// <returns>The rendering.</returns>
    /// <exception cref="FormatException">The row's options or input are malformed.</exception>
    private static string RenderRow(ReleaseNoteFix fix)
    {
        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = GenerateInput(CorpusEscapes.DecodeText(fix.Input));

        return options.Surface == "Nodes" ? RenderNodes(input, options) : RenderStore(input, options);
    }

    /// <summary>
    /// Opens the input as a mail store and renders it.
    /// </summary>
    /// <param name="input">The file bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The rendering.</returns>
    private static string RenderStore(byte[] input, RowOptions options)
    {
        using OutlookMailStore store = OutlookMailStore.Open(new MemoryStream(input, writable: false), options.ToReaderOptions());

        return RenderStore(store, options);
    }

    /// <summary>
    /// Renders an open mail store: the store line and listing, the named-property lines, then the folders depth first
    /// from the root.
    /// </summary>
    /// <param name="store">The open store.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The rendering.</returns>
    private static string RenderStore(OutlookMailStore store, RowOptions options)
    {
        var lines = new List<string>();
        if (options.HasDetail("store"))
        {
            MapiPropertyCollection properties = store.Properties;
            lines.Add($"#store\t{Text(store.DisplayName)}\t{Count(properties.Count)}");
            if (options.HasDetail("properties"))
                AddListing(lines, store, properties);
        }

        for (int i = 0; i < options.Names; i++)
        {
            var id = (ushort)(0x8000 + i);
            string resolution = store.TryGetPropertyName(new MapiPropertyTag(id, MapiPropertyType.Unspecified), out MapiNamedProperty name)
                ? NamedText(name)
                : "-";
            lines.Add($"#name\t0x{id.ToString("X4", CultureInfo.InvariantCulture)}\t{resolution}");
        }

        RenderFolder(lines, store, store.RootFolder, "/", options, depth: 0);
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Renders a folder line, its listing, its messages, its associated messages, then its subfolders.
    /// </summary>
    /// <param name="lines">The rendering under construction.</param>
    /// <param name="store">The open store, which resolves named properties.</param>
    /// <param name="folder">The folder.</param>
    /// <param name="path">The folder's path: <c>/</c> for the root, otherwise its parent's path and its name.</param>
    /// <param name="options">The row options.</param>
    /// <param name="depth">The folder's depth below the root.</param>
    /// <exception cref="InvalidOperationException">
    /// The walk passes <see cref="MaxFolderDepth" />, so the hierarchy has a cycle.
    /// </exception>
    private static void RenderFolder(List<string> lines, OutlookMailStore store, OutlookMailFolder folder, string path, RowOptions options, int depth)
    {
        if (depth > MaxFolderDepth) throw new InvalidOperationException($"The folder walk passed depth {MaxFolderDepth} at {path}.");

        lines.Add($"#folder\t{path}\t{Number(folder.MessageCount)}\t{Boolean(folder.HasSubfolders)}");
        if (options.HasDetail("folders"))
            AddListing(lines, store, folder.Properties);

        int index = 0;
        foreach (OutlookMailMessage message in folder.EnumerateMessages())
            RenderMessage(lines, store, message, path, Count(index++), options);

        if (options.HasDetail("associated"))
        {
            index = 0;
            foreach (OutlookMailMessage message in folder.EnumerateAssociatedMessages())
                RenderMessage(lines, store, message, path, "a" + Count(index++), options);
        }

        foreach (OutlookMailFolder child in folder.EnumerateSubfolders())
            RenderFolder(lines, store, child, (path == "/" ? "/" : path + "/") + PathSegment(child.DisplayName), options, depth + 1);
    }

    /// <summary>
    /// Renders a message line and the details the options ask for; an embedded message follows its attachment line.
    /// </summary>
    /// <param name="lines">The rendering under construction.</param>
    /// <param name="store">The open store, which resolves named properties.</param>
    /// <param name="message">The message.</param>
    /// <param name="path">The path of the folder that holds the message.</param>
    /// <param name="index">
    /// The message's index: its position, or its parent's index and its attachment's position.
    /// </param>
    /// <param name="options">The row options.</param>
    private static void RenderMessage(List<string> lines, OutlookMailStore store, OutlookMailMessage message, string path, string index, RowOptions options)
    {
        MapiPropertyCollection properties = message.Properties;
        bool full = options.HasDetail("properties");
        string listing = full ? "*" : Hash16(Encoding.UTF8.GetBytes(string.Join('\n', Listing(store, properties))));
        lines.Add($"{path}\t{index}\t{Text(message.Subject)}\t{Count(message.Recipients.Count)}\t{Count(message.Attachments.Count)}\t{listing}");
        if (full)
            AddListing(lines, store, properties);

        if (options.HasDetail("bodies"))
        {
            lines.Add("#body.text\t" + Body(message.BodyText));
            lines.Add("#body.html\t" + Body(message.BodyHtml));
            lines.Add("#body.rtf\t" + Body(message.BodyRtf));
        }

        if (options.HasDetail("recipients"))
        {
            int position = 0;
            foreach (OutlookRecipient recipient in message.Recipients)
            {
                string type = recipient.RecipientType?.ToString() ?? "-";
                lines.Add($"#recipient\t{Count(position++)}\t{type}\t{Text(recipient.DisplayName)}\t{Text(recipient.EmailAddress)}\t{Text(recipient.AddressType)}");
                if (full)
                    AddListing(lines, store, recipient.Properties);
            }
        }

        if (options.HasDetail("attachments"))
        {
            int position = 0;
            foreach (OutlookMailAttachment attachment in message.Attachments)
            {
                string number = Count(position++);
                OutlookAttachmentMethod method = attachment.Method;
                string content = method switch
                {
                    OutlookAttachmentMethod.ByValue => ReadContent(attachment),
                    OutlookAttachmentMethod.EmbeddedMessage => "message",
                    _ => "-",
                };

                lines.Add($"#attachment\t{number}\t{method}\t{Text(attachment.FileName)}\t{Number(attachment.Size)}\t{content}");
                if (full)
                    AddListing(lines, store, attachment.Properties);

                if (method == OutlookAttachmentMethod.EmbeddedMessage)
                    RenderMessage(lines, store, attachment.OpenMessage(), path, index + "." + number, options);
            }
        }
    }

    /// <summary>
    /// Reads a by-value attachment's content and renders it as <see cref="Bytes" /> does.
    /// </summary>
    /// <param name="attachment">The attachment.</param>
    /// <returns>The rendered content.</returns>
    private static string ReadContent(OutlookMailAttachment attachment)
    {
        using Stream stream = attachment.OpenContentStream();
        using var copy = new MemoryStream();
        stream.CopyTo(copy);

        return Bytes(copy.ToArray());
    }

    /// <summary>
    /// Appends a property listing, each line prefixed <c>#property</c>.
    /// </summary>
    /// <param name="lines">The rendering under construction.</param>
    /// <param name="store">The open store, which resolves named properties.</param>
    /// <param name="properties">The properties.</param>
    private static void AddListing(List<string> lines, OutlookMailStore store, MapiPropertyCollection properties)
    {
        foreach (string line in Listing(store, properties))
            lines.Add("#property\t" + line);
    }

    /// <summary>
    /// Lists properties in tag order, one <c>tag\ttype\tvalue</c> line each.
    /// </summary>
    /// <param name="store">The open store, which resolves named properties.</param>
    /// <param name="properties">The properties.</param>
    /// <returns>The listing lines.</returns>
    private static IEnumerable<string> Listing(OutlookMailStore store, MapiPropertyCollection properties) =>
        properties
            .OrderBy(property => property.Tag.Value)
            .Select(property => $"{TagText(store, property.Tag)}\t{TypeText(property.Tag)}\t{Value(property.Value)}");

    /// <summary>
    /// Renders a property tag: <c>0x</c> and eight hexadecimal digits, or <c>name:</c> and the name of a named property
    /// the store resolves.
    /// </summary>
    /// <param name="store">The open store.</param>
    /// <param name="tag">The tag.</param>
    /// <returns>The rendered tag.</returns>
    private static string TagText(OutlookMailStore store, MapiPropertyTag tag) =>
        tag.IsNamed && store.TryGetPropertyName(tag, out MapiNamedProperty name)
            ? "name:" + NamedText(name)
            : "0x" + tag.Value.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>
    /// Renders a named property: its property set in <c>D</c> form, then its quoted, escaped name or its numeric
    /// identifier as <c>0x</c> and eight hexadecimal digits.
    /// </summary>
    /// <param name="name">The named property.</param>
    /// <returns>The rendered name.</returns>
    private static string NamedText(MapiNamedProperty name) =>
        name.PropertySetId.ToString("D", CultureInfo.InvariantCulture) + ":"
            + (name.Name is string text
                ? "\"" + EscapeValue(text) + "\""
                : "0x" + name.Id.GetValueOrDefault().ToString("X8", CultureInfo.InvariantCulture));

    /// <summary>
    /// Renders a property type: the <see cref="MapiPropertyType" /> member name, or <c>0x</c> and four hexadecimal
    /// digits for a code the enumeration does not define, prefixed <c>Mv</c> for a multi-valued tag.
    /// </summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The rendered type.</returns>
    private static string TypeText(MapiPropertyTag tag)
    {
        MapiPropertyType type = tag.Type;
        string name = Enum.IsDefined(type) ? type.ToString() : "0x" + ((ushort)type).ToString("X4", CultureInfo.InvariantCulture);

        return tag.IsMultiValued ? "Mv" + name : name;
    }

    /// <summary>
    /// Renders a decoded property value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// <c>null</c> for none, a string escaped, binary data as <see cref="Bytes" /> writes it, an array as <c>[a,b]</c>,
    /// and a scalar as <see cref="Scalar" /> writes it.
    /// </returns>
    private static string Value(object? value) =>
        value switch
        {
            null => "null",
            string text => EscapeValue(text),
            byte[] bytes => Bytes(bytes),
            ReadOnlyMemory<byte> memory => Bytes(memory.Span),
            Array array => "[" + string.Join(",", array.Cast<object?>().Select(Element)) + "]",
            _ => Scalar(value),
        };

    /// <summary>
    /// Renders one element of a multi-valued property, with <c>,</c>, <c>[</c> and <c>]</c> inside a string written
    /// <c>\x2C</c>, <c>\x5B</c> and <c>\x5D</c> so that the array's punctuation stays unambiguous.
    /// </summary>
    /// <param name="value">The element.</param>
    /// <returns>The rendered element.</returns>
    private static string Element(object? value) =>
        value is string text
            ? EscapeValue(text)
                .Replace(",", @"\x2C", StringComparison.Ordinal)
                .Replace("[", @"\x5B", StringComparison.Ordinal)
                .Replace("]", @"\x5D", StringComparison.Ordinal)
            : Value(value);

    /// <summary>
    /// Renders a scalar property value in an invariant form.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    /// <c>true</c> or <c>false</c> for a Boolean, the round-trip form of a floating-point number, the ISO 8601
    /// round-trip form (<c>o</c>) of a time, the <c>D</c> form of a GUID, and the invariant form of any other number.
    /// </returns>
    private static string Scalar(object value) =>
        value switch
        {
            bool flag => Boolean(flag),
            float single => single.ToString("R", CultureInfo.InvariantCulture),
            double number => number.ToString("R", CultureInfo.InvariantCulture),
            DateTimeOffset time => time.ToString("o", CultureInfo.InvariantCulture),
            DateTime time => time.ToString("o", CultureInfo.InvariantCulture),
            Guid guid => guid.ToString("D", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => EscapeValue(value.ToString() ?? string.Empty),
        };

    /// <summary>
    /// Renders a message body: <c>-</c> for none, the escaped text when its UTF-8 form is at most
    /// <see cref="MaxBodyBytes" /> bytes, otherwise <c>sha256:</c>, the digest prefix of its UTF-8 form, <c>/</c> and
    /// its UTF-16 length.
    /// </summary>
    /// <param name="text">The body.</param>
    /// <returns>The rendered body.</returns>
    private static string Body(string? text)
    {
        if (text is null)
            return "-";

        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        return utf8.Length <= MaxBodyBytes ? EscapeValue(text) : "sha256:" + Hash16(utf8) + "/" + Count(text.Length);
    }

    /// <summary>
    /// Renders a folder name as a path segment: escaped, with <c>/</c> written <c>\x2F</c>, or <c>-</c> for none.
    /// </summary>
    /// <param name="name">The folder's display name.</param>
    /// <returns>The path segment.</returns>
    private static string PathSegment(string? name) =>
        name is null ? "-" : EscapeValue(name).Replace("/", @"\x2F", StringComparison.Ordinal);

    /// <summary>
    /// Renders optional text: escaped, or <c>-</c> for none.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The rendered text.</returns>
    private static string Text(string? text) =>
        text is null ? "-" : EscapeValue(text);

    /// <summary>
    /// Renders an optional number in invariant decimal, or <c>-</c> for none.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <returns>The rendered number.</returns>
    private static string Number(long? value) =>
        value is long number ? number.ToString(CultureInfo.InvariantCulture) : "-";

    /// <summary>
    /// Renders a count or position in invariant decimal.
    /// </summary>
    /// <param name="value">The count.</param>
    /// <returns>The rendered count.</returns>
    private static string Count(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Renders a Boolean as <c>true</c> or <c>false</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The rendered value.</returns>
    private static string Boolean(bool value) =>
        value ? "true" : "false";

    /// <summary>
    /// Opens the input as a container and renders it: the file line, one line per node, and the view the options ask
    /// for.
    /// </summary>
    /// <param name="input">The file bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>The rendering.</returns>
    /// <exception cref="InvalidOperationException">The node the view names has no such subnode.</exception>
    private static string RenderNodes(byte[] input, RowOptions options)
    {
        using PstFile file = PstFile.Open(new MemoryStream(input, writable: false), options.ToFileOptions());
        var lines = new List<string> { $"#file\t{file.Format}\t{file.CryptMethod}" };

        foreach (PstNodeInfo info in file.EnumerateNodes())
        {
            PstNode node = file.GetNode(info.NodeId);
            string line = $"{NodeIdText(info.NodeId.Value)}\t{info.NodeId.Type}\t{Number(node.DataLength)}\t{Count(node.EnumerateSubnodes().Count())}";
            lines.Add(options.Data ? line + "\t" + Bytes(node.ReadAllBytes()) : line);
        }

        if (options.Node is uint nodeId && options.View is string view)
        {
            PstNode node = file.GetNode(new PstNodeId(nodeId));
            if (options.Subnode is uint subnodeId)
            {
                if (!node.TryGetSubnode(new PstNodeId(subnodeId), out PstNode? subnode))
                    throw new InvalidOperationException($"Node {NodeIdText(nodeId)} has no subnode {NodeIdText(subnodeId)}.");

                node = subnode;
            }

            RenderView(lines, node, view, options.Row);
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Renders one view of a node: its property context, its table context, its row identifiers, one row looked up by
    /// identifier, its subnodes, or its data.
    /// </summary>
    /// <param name="lines">The rendering under construction.</param>
    /// <param name="node">The node.</param>
    /// <param name="view">
    /// The view: <c>Pc</c>, <c>Tc</c>, <c>RowIds</c>, <c>Lookup</c>, <c>Subnodes</c> or <c>Data</c>.
    /// </param>
    /// <param name="rowId">The row identifier the <c>Lookup</c> view looks up.</param>
    private static void RenderView(List<string> lines, PstNode node, string view, uint? rowId)
    {
        string nodeText = NodeIdText(node.NodeId.Value);
        switch (view)
        {
            case "Pc":
                PstPropertyContext context = node.ReadPropertyContext();
                lines.Add($"#pc\t{nodeText}\t{Count(context.Count)}");
                foreach (PstPropertyValue value in context)
                    lines.Add($"0x{value.PropertyId.ToString("X4", CultureInfo.InvariantCulture)}\t0x{value.WireType.ToString("X4", CultureInfo.InvariantCulture)}\t{WireValue(value)}");

                break;

            case "Tc":
                PstTableContext table = node.ReadTableContext();
                string columns = string.Join(" ", table.Columns.Select(column =>
                    $"0x{column.PropertyId.ToString("X4", CultureInfo.InvariantCulture)}/0x{column.WireType.ToString("X4", CultureInfo.InvariantCulture)}/{Count(column.Width)}"));
                lines.Add($"#tc\t{nodeText}\t{Count(table.RowCount)}\t{columns}");
                foreach (PstTableRow row in table.EnumerateRows())
                    lines.Add($"#row\t{NodeIdText(row.RowId)}\t{Cells(row)}");

                break;

            case "RowIds":
                PstTableContext rowTable = node.ReadTableContext();
                string ids = string.Join(" ", rowTable.EnumerateRows().Select(row => NodeIdText(row.RowId)));
                lines.Add($"#rowids\t{nodeText}\t{Count(rowTable.RowCount)}\t{ids}");
                break;

            case "Lookup":
                uint target = rowId.GetValueOrDefault();
                string found = node.ReadTableContext().TryGetRow(target, out PstTableRow? match) ? Cells(match) : "-";
                lines.Add($"#lookup\t{nodeText}\t{NodeIdText(target)}\t{found}");
                break;

            case "Subnodes":
                lines.Add($"#subnodes\t{nodeText}");
                foreach (PstNodeInfo info in node.EnumerateSubnodes())
                    lines.Add($"{NodeIdText(info.NodeId.Value)}\t{info.NodeId.Type}\t{Number(info.DataLength)}\t{Boolean(info.HasSubnodes)}");

                break;

            default:
                lines.Add($"#data\t{nodeText}\t{Bytes(node.ReadAllBytes())}");
                break;
        }
    }

    /// <summary>
    /// Renders a table row's present cells, each <c>0x&lt;id&gt;=&lt;value&gt;</c>, separated by spaces, with a space
    /// inside a value written <c>\x20</c>.
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The rendered cells.</returns>
    private static string Cells(PstTableRow row) =>
        string.Join(" ", row.EnumerateCells().Select(cell =>
            $"0x{cell.PropertyId.ToString("X4", CultureInfo.InvariantCulture)}=" + WireValue(cell).Replace(" ", @"\x20", StringComparison.Ordinal)));

    /// <summary>
    /// Renders a container property value: a Unicode string as its quoted, escaped text, anything else as its raw
    /// bytes.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The rendered value.</returns>
    private static string WireValue(PstPropertyValue value) =>
        value.WireType == 0x001F ? "\"" + EscapeValue(value.GetString()) + "\"" : Bytes(value.RawData.Span);

    /// <summary>
    /// Renders a node, subnode or row identifier as <c>0x</c> and eight hexadecimal digits.
    /// </summary>
    /// <param name="value">The identifier.</param>
    /// <returns>The rendered identifier.</returns>
    private static string NodeIdText(uint value) =>
        "0x" + value.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>
    /// Escapes text inside a rendering.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>
    /// The text with <c>\</c> written <c>\\</c>; LF, CR, tab and NUL written <c>\n</c>, <c>\r</c>, <c>\t</c> and
    /// <c>\0</c>; any other C0 control and DEL written <c>\xHH</c>; and an unpaired surrogate written <c>\u{HHHH}</c>.
    /// Everything else, non-ASCII text included, is kept.
    /// </returns>
    private static string EscapeValue(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            switch (c)
            {
                case '\\':
                    builder.Append(@"\\");
                    break;

                case '\n':
                    builder.Append(@"\n");
                    break;

                case '\r':
                    builder.Append(@"\r");
                    break;

                case '\t':
                    builder.Append(@"\t");
                    break;

                case '\0':
                    builder.Append(@"\0");
                    break;

                default:
                    if (c < 0x20 || c == 0x7F)
                    {
                        builder.Append(@"\x").Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
                    }
                    else if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    {
                        builder.Append(c).Append(text[i + 1]);
                        i++;
                    }
                    else if (char.IsSurrogate(c))
                    {
                        builder.Append(@"\u{").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture)).Append('}');
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Renders binary data: upper-case hexadecimal up to <see cref="MaxHexBytes" /> bytes, otherwise <c>sha256:</c>,
    /// the digest prefix and <c>/</c> and the length.
    /// </summary>
    /// <param name="bytes">The data.</param>
    /// <returns>The rendered data.</returns>
    private static string Bytes(ReadOnlySpan<byte> bytes) =>
        bytes.Length <= MaxHexBytes ? Convert.ToHexString(bytes) : "sha256:" + Hash16(bytes) + "/" + Count(bytes.Length);

    /// <summary>
    /// Computes the first 16 lower-case hexadecimal digits of a SHA-256 digest.
    /// </summary>
    /// <param name="bytes">The data.</param>
    /// <returns>The digest prefix.</returns>
    private static string Hash16(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes))[..16].ToLowerInvariant();
}
