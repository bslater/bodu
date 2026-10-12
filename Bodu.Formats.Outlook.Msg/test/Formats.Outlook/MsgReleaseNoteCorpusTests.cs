// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgReleaseNoteCorpusTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Bodu.Formats.Outlook.Msg;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Holds Bodu.Formats.Outlook.Msg to the defect fixes other <c>.msg</c> libraries list in their release notes, as
/// catalogued in <c>corpus/msg/fixes/</c> and embedded under <c>Fixtures/ReleaseNotes/</c>.
/// </summary>
/// <remarks>
/// <para>
/// A row's input is <c>gen:</c> and a message specification, built by <see cref="MessageSpecification" />, or
/// <c>bytes:</c> and the input bytes themselves. Both are defined in <c>corpus/msg/README.md</c>.
/// </para>
/// <para>
/// The <c>Surface</c> option selects the rendering. <c>Message</c> (the default) opens the input with
/// <see cref="OutlookMessage" /> and writes one line per property, scoped by where it lives, then the decoded bodies,
/// recipients, attachments and nested messages. <c>Conveniences</c> writes the convenience accessors instead.
/// <c>CompressedRtf</c> runs the MS-OXRTFCP decoder over the input bytes and returns the decompressed bytes unchanged.
/// A property line is <c>scope TAB key TAB type TAB value</c>, a body or convenience line <c>scope TAB #name TAB
/// value</c>, and every line ends with a line feed.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class MsgReleaseNoteCorpusTests
{
    /// <summary>The corpus area whose catalogues this class runs.</summary>
    private const string Area = "msg";

    /// <summary>The longest string or body value, in UTF-8 bytes, rendered in full; a longer value renders as a hash.</summary>
    private const int MaxTextBytes = 256;

    /// <summary>The longest binary value, in bytes, rendered as hexadecimal; a longer value renders as a hash.</summary>
    private const int MaxBinaryBytes = 32;

    /// <summary>The option names a catalogue row may use, each mapped by <see cref="RowOptions.Parse" />.</summary>
    private static readonly string[] s_optionNames =
    [
        "Surface", "ValidationLevel", "ReadStrategy", "DecompressRtf", "MaxEmbeddedMessageDepth",
        "MaxDecompressedRtfBytes", "MaxInlineAttachmentBytes",
    ];

    /// <summary>The catalogues embedded in this test assembly, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<ReleaseNoteCatalog>> s_catalogs =
        new(() => ReleaseNoteCatalog.LoadEmbedded(typeof(MsgReleaseNoteCorpusTests).Assembly));

    /// <summary>
    /// Gets the runnable rows of kind <c>parse</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> ParseRows =>
        RunnableRows("parse");

    /// <summary>
    /// Gets the runnable rows of kind <c>reject</c>.
    /// </summary>
    /// <value>One single-element argument array per row.</value>
    public static IEnumerable<object[]> RejectRows =>
        RunnableRows("reject");

    /// <summary>
    /// Gets every row of every embedded catalogue.
    /// </summary>
    /// <value>The rows, catalogue by catalogue in file order.</value>
    private static IEnumerable<ReleaseNoteFix> AllRows =>
        s_catalogs.Value.SelectMany(catalog => catalog.Rows);

    /// <summary>
    /// Selects the runnable rows of one kind as test data.
    /// </summary>
    /// <param name="kind">The row kind.</param>
    /// <returns>One single-element argument array per <c>applies</c> or <c>dialect</c> row of <paramref name="kind" />.</returns>
    private static IEnumerable<object[]> RunnableRows(string kind) =>
        AllRows.Where(fix => fix.IsRunnable && fix.Kind == kind).Select(fix => new object[] { fix });

    /// <summary>
    /// Resolves a row's input field to the bytes it stands for.
    /// </summary>
    /// <param name="field">The escaped input field.</param>
    /// <returns>The built message for a <c>gen:</c> input, or the bytes after <c>bytes:</c>.</returns>
    /// <exception cref="FormatException">
    /// The field is not well formed, has neither prefix, or holds a message specification that is not well formed.
    /// </exception>
    /// <remarks>
    /// The field is decoded with <see cref="CorpusEscapes" /> first; the specification after <c>gen:</c> is then read one
    /// byte to one character (Latin-1), as <c>corpus/msg/README.md</c> defines.
    /// </remarks>
    private static byte[] ResolveInput(string field)
    {
        byte[] decoded = CorpusEscapes.Decode(field);
        ReadOnlySpan<byte> span = decoded;

        if (span.StartsWith("gen:"u8))
            return MessageSpecification.Build(Encoding.Latin1.GetString(span["gen:".Length..]));

        if (span.StartsWith("bytes:"u8))
            return span["bytes:".Length..].ToArray();

        throw new FormatException("The input is neither gen: nor bytes:.");
    }

    /// <summary>
    /// Renders an input through the surface the row's options select.
    /// </summary>
    /// <param name="input">The input bytes.</param>
    /// <param name="options">The row options.</param>
    /// <returns>
    /// The rendering's bytes: UTF-8 text for the <c>Message</c> and <c>Conveniences</c> surfaces, and the decompressed
    /// RTF bytes for <c>CompressedRtf</c>.
    /// </returns>
    /// <exception cref="OutlookMsgFormatException">The reader or the decoder rejects the input.</exception>
    /// <remarks>
    /// The message surfaces read everything they render, recipients, attachments, nested messages and bodies included,
    /// so a failure that the reader raises lazily surfaces here as well.
    /// </remarks>
    private static byte[] Render(byte[] input, RowOptions options)
    {
        if (options.Surface == "CompressedRtf")
            return CompressedRtf.Decompress(input, options.Reader.MaxDecompressedRtfBytes);

        using var stream = new MemoryStream(input, writable: false);
        using var message = OutlookMessage.Open(stream, options.Reader, leaveOpen: true);
        var builder = new StringBuilder();
        if (options.Surface == "Conveniences")
            RenderConveniences(builder, message, "message", string.Empty);
        else
            RenderMessage(builder, message, message, "message", string.Empty);

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Renders a message's properties and bodies, then its recipients and attachments, nested messages included.
    /// </summary>
    /// <param name="builder">The rendering.</param>
    /// <param name="root">The root message, whose name map resolves named tags at every depth.</param>
    /// <param name="message">The message to render.</param>
    /// <param name="scope">The scope of the message's own lines.</param>
    /// <param name="prefix">The prefix of its recipients' and attachments' scopes.</param>
    private static void RenderMessage(StringBuilder builder, OutlookMessage root, OutlookMessage message, string scope, string prefix)
    {
        RenderProperties(builder, root, message.Properties, scope);

        string? text = message.BodyText;
        string? html = message.BodyHtml;
        string? rtf = message.BodyRtf;
        if (text is not null)
            Line(builder, scope, "#body.text", FormatText(text, list: false));

        if (html is not null)
            Line(builder, scope, "#body.html", FormatText(html, list: false));

        if (rtf is not null)
            Line(builder, scope, "#body.rtf", FormatText(rtf, list: false));

        for (int i = 0; i < message.Recipients.Count; i++)
            RenderProperties(builder, root, message.Recipients[i].Properties, prefix + "recipient[" + Index(i) + "]");

        for (int i = 0; i < message.Attachments.Count; i++)
        {
            OutlookAttachment attachment = message.Attachments[i];
            string attachmentScope = prefix + "attachment[" + Index(i) + "]";
            RenderProperties(builder, root, attachment.Properties, attachmentScope);
            if (attachment.Method == OutlookAttachmentMethod.EmbeddedMessage)
            {
                using OutlookMessage nested = attachment.OpenMessage();
                RenderMessage(builder, root, nested, attachmentScope + "/message", attachmentScope + "/message/");
            }
        }
    }

    /// <summary>
    /// Renders a property collection, one line per property in ascending tag order.
    /// </summary>
    /// <param name="builder">The rendering.</param>
    /// <param name="root">The root message, whose name map resolves named tags.</param>
    /// <param name="properties">The properties.</param>
    /// <param name="scope">The scope of the lines.</param>
    private static void RenderProperties(StringBuilder builder, OutlookMessage root, MapiPropertyCollection properties, string scope)
    {
        foreach (MapiProperty property in properties.OrderBy(static p => p.Tag.Value))
        {
            builder.Append(scope).Append('\t').Append(FormatKey(root, property.Tag)).Append('\t')
                .Append(FormatType(property.Tag)).Append('\t').Append(FormatValue(property.Value, list: false)).Append('\n');
        }
    }

    /// <summary>
    /// Renders the convenience accessors of a message, its recipients and its attachments, nested messages included.
    /// </summary>
    /// <param name="builder">The rendering.</param>
    /// <param name="message">The message to render.</param>
    /// <param name="scope">The scope of the message's own lines.</param>
    /// <param name="prefix">The prefix of its recipients' and attachments' scopes.</param>
    private static void RenderConveniences(StringBuilder builder, OutlookMessage message, string scope, string prefix)
    {
        Convenience(builder, scope, "#subject", message.Subject);
        Convenience(builder, scope, "#sender.name", message.SenderName);
        Convenience(builder, scope, "#sender.email", message.SenderEmailAddress);
        Convenience(builder, scope, "#class", message.MessageClass);
        Convenience(builder, scope, "#message-id", message.InternetMessageId);
        Convenience(builder, scope, "#headers", message.TransportMessageHeaders);
        Convenience(builder, scope, "#sent", message.SentTime);
        Convenience(builder, scope, "#received", message.ReceivedTime);
        Convenience(builder, scope, "#depth", message.EmbeddedDepth);
        Convenience(builder, scope, "#recipients", message.Recipients.Count);
        Convenience(builder, scope, "#attachments", message.Attachments.Count);

        for (int i = 0; i < message.Recipients.Count; i++)
        {
            OutlookRecipient recipient = message.Recipients[i];
            string recipientScope = prefix + "recipient[" + Index(i) + "]";
            Convenience(builder, recipientScope, "#type", recipient.RecipientType?.ToString());
            Convenience(builder, recipientScope, "#name", recipient.DisplayName);
            Convenience(builder, recipientScope, "#email", recipient.EmailAddress);
            Convenience(builder, recipientScope, "#addrtype", recipient.AddressType);
        }

        for (int i = 0; i < message.Attachments.Count; i++)
        {
            OutlookAttachment attachment = message.Attachments[i];
            string attachmentScope = prefix + "attachment[" + Index(i) + "]";
            Convenience(builder, attachmentScope, "#method", attachment.Method.ToString());
            Convenience(builder, attachmentScope, "#filename", attachment.FileName);
            Convenience(builder, attachmentScope, "#cid", attachment.ContentId);
            Convenience(builder, attachmentScope, "#mime", attachment.MimeTag);
            Convenience(builder, attachmentScope, "#size", attachment.Size);
            if (attachment.Method == OutlookAttachmentMethod.ByValue)
            {
                using Stream content = attachment.OpenContentStream();
                using var copy = new MemoryStream();
                content.CopyTo(copy);
                Convenience(builder, attachmentScope, "#content", copy.ToArray());
            }
            else if (attachment.Method == OutlookAttachmentMethod.EmbeddedMessage)
            {
                using OutlookMessage nested = attachment.OpenMessage();
                RenderConveniences(builder, nested, attachmentScope + "/message", attachmentScope + "/message/");
            }
        }
    }

    /// <summary>
    /// Appends a convenience line when its value is not <see langword="null" />; a missing line means a null value.
    /// </summary>
    /// <param name="builder">The rendering.</param>
    /// <param name="scope">The line's scope.</param>
    /// <param name="name">The convenience's name, starting with <c>#</c>.</param>
    /// <param name="value">The convenience's value.</param>
    private static void Convenience(StringBuilder builder, string scope, string name, object? value)
    {
        if (value is not null)
            Line(builder, scope, name, value is string text ? FormatText(text, list: false) : FormatValue(value, list: false));
    }

    /// <summary>
    /// Appends a body or convenience line.
    /// </summary>
    /// <param name="builder">The rendering.</param>
    /// <param name="scope">The line's scope.</param>
    /// <param name="name">The line's name, starting with <c>#</c>.</param>
    /// <param name="value">The formatted value.</param>
    private static void Line(StringBuilder builder, string scope, string name, string value) =>
        builder.Append(scope).Append('\t').Append(name).Append('\t').Append(value).Append('\n');

    /// <summary>
    /// Formats a zero-based index for a scope.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The index in invariant decimal digits.</returns>
    private static string Index(int index) =>
        index.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a property's key: its tag in eight uppercase hexadecimal digits or, for a named tag the root message's
    /// name map resolves, <c>name:</c>, the property set in <c>D</c> form, a colon, and the numeric identifier as
    /// <c>0x</c> and at least four uppercase hexadecimal digits or the string name in double quotes.
    /// </summary>
    /// <param name="root">The root message, whose name map is shared by every nested message.</param>
    /// <param name="tag">The property tag.</param>
    /// <returns>The key.</returns>
    private static string FormatKey(OutlookMessage root, MapiPropertyTag tag)
    {
        if (tag.IsNamed && root.TryGetPropertyName(tag, out MapiNamedProperty name))
        {
            string id = name.Id is uint lid
                ? "0x" + lid.ToString("X4", CultureInfo.InvariantCulture)
                : "\"" + EscapeText(name.Name!, list: false) + "\"";
            return "name:" + name.PropertySetId.ToString("D") + ":" + id;
        }

        return tag.Value.ToString("X8", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats a property's type: the <see cref="MapiPropertyType" /> name, or <c>0x</c> and four uppercase hexadecimal
    /// digits for a code the enumeration does not define, followed by <c>[]</c> when the tag is multi-valued.
    /// </summary>
    /// <param name="tag">The property tag.</param>
    /// <returns>The type.</returns>
    private static string FormatType(MapiPropertyTag tag)
    {
        string name = Enum.IsDefined(tag.Type)
            ? tag.Type.ToString()
            : "0x" + ((ushort)tag.Type).ToString("X4", CultureInfo.InvariantCulture);
        return tag.IsMultiValued ? name + "[]" : name;
    }

    /// <summary>
    /// Formats a decoded value, as a whole value or as an element of a multi-valued property.
    /// </summary>
    /// <param name="value">The decoded value.</param>
    /// <param name="list">Whether the value is an element of a multi-valued property.</param>
    /// <returns>
    /// <c>null</c>; escaped text; <c>true</c> or <c>false</c>; binary data in hexadecimal; a number in the invariant
    /// culture; a time in the round-trip <c>o</c> form; a GUID in the <c>D</c> form; or the elements of an array in
    /// square brackets, separated by commas.
    /// </returns>
    /// <exception cref="InvalidOperationException">The value has a type this rendering does not define.</exception>
    private static string FormatValue(object? value, bool list) =>
        value switch
        {
            null => "null",
            string s => FormatText(s, list),
            bool b => b ? "true" : "false",
            byte[] bytes => FormatBinary(bytes),
            ReadOnlyMemory<byte> memory => FormatBinary(memory.ToArray()),
            float f => f.ToString("R", CultureInfo.InvariantCulture),
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            DateTimeOffset t => t.ToString("o", CultureInfo.InvariantCulture),
            Guid g => g.ToString("D"),
            short or int or long or ushort or uint or ulong or byte or sbyte => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
            Array array => "[" + string.Join(",", array.Cast<object?>().Select(element => FormatValue(element, list: true))) + "]",
            _ => throw new InvalidOperationException($"There is no rendering for a value of type {value.GetType()}."),
        };

    /// <summary>
    /// Formats text: escaped in full up to <see cref="MaxTextBytes" /> UTF-8 bytes, otherwise as a hash.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="list">Whether the text is an element of a multi-valued property.</param>
    /// <returns>The formatted text.</returns>
    private static string FormatText(string text, bool list)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        return utf8.Length > MaxTextBytes ? Hash(utf8) : EscapeText(text, list);
    }

    /// <summary>
    /// Formats binary data: lowercase hexadecimal up to <see cref="MaxBinaryBytes" /> bytes, otherwise as a hash.
    /// </summary>
    /// <param name="bytes">The data.</param>
    /// <returns>The formatted data.</returns>
    private static string FormatBinary(byte[] bytes) =>
        bytes.Length > MaxBinaryBytes ? Hash(bytes) : Convert.ToHexString(bytes).ToLowerInvariant();

    /// <summary>
    /// Formats a hash: <c>sha256:</c>, the first sixteen lowercase hexadecimal digits of the SHA-256 digest, a colon and
    /// the length in bytes.
    /// </summary>
    /// <param name="bytes">The hashed bytes.</param>
    /// <returns>The formatted hash.</returns>
    private static string Hash(byte[] bytes) =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(bytes))[..16].ToLowerInvariant() + ":" + bytes.Length.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Escapes text inside a rendered value.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="list">Whether the text is an element of a multi-valued property.</param>
    /// <returns>
    /// The text with a backslash doubled; tab, line feed, carriage return and NUL as <c>\t</c>, <c>\n</c>, <c>\r</c>
    /// and <c>\0</c>; other C0 controls and DEL as <c>\xHH</c>; an unpaired surrogate as <c>\uHHHH</c>; in a list
    /// element a comma and square brackets as <c>\x2C</c>, <c>\x5B</c> and <c>\x5D</c>; and every other character as
    /// itself.
    /// </returns>
    private static string EscapeText(string text, bool list)
    {
        var builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                builder.Append(c).Append(text[++i]);
                continue;
            }

            switch (c)
            {
                case '\\':
                    builder.Append(@"\\");
                    break;

                case '\t':
                    builder.Append(@"\t");
                    break;

                case '\n':
                    builder.Append(@"\n");
                    break;

                case '\r':
                    builder.Append(@"\r");
                    break;

                case '\0':
                    builder.Append(@"\0");
                    break;

                case ',' or '[' or ']' when list:
                    builder.Append(@"\x").Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
                    break;

                default:
                    if (c < 0x20 || c == 0x7F)
                        builder.Append(@"\x").Append(((int)c).ToString("X2", CultureInfo.InvariantCulture));
                    else if (char.IsSurrogate(c))
                        builder.Append(@"\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(c);

                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Describes an exception for a failure message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>Its type name and message, followed by those of its inner exception when it has one.</returns>
    private static string Describe(Exception exception) =>
        $"{exception.GetType().Name}: {exception.Message}"
        + (exception.InnerException is null ? string.Empty : $" <- {exception.InnerException.GetType().Name}: {exception.InnerException.Message}");
}
