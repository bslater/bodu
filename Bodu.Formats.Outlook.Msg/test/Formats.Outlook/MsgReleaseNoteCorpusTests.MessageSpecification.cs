// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgReleaseNoteCorpusTests.MessageSpecification.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using Bodu.Formats.Outlook.Msg;

namespace Bodu.Formats.Outlook;

public sealed partial class MsgReleaseNoteCorpusTests
{
    /// <summary>
    /// Reads a <c>gen:Msg(...)</c> message specification, as <c>corpus/msg/README.md</c> defines it, and builds the
    /// <c>.msg</c> file it describes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The specification is a list of items separated by <c>;</c> inside parentheses, and the same list describes the
    /// root message, a recipient, an attachment, an embedded message or any other storage. A value runs to the next
    /// <c>;</c> or <c>)</c>; inside it <c>%HH</c> is the byte <c>HH</c> and every other character is its own byte.
    /// </para>
    /// <para>
    /// The file is built with <see cref="MsgFixtureBuilder" />, then each whole-file transform of the root is applied in
    /// the order written, by <see cref="CompoundImage.Apply" />.
    /// </para>
    /// </remarks>
    private sealed class MessageSpecification
    {
        /// <summary>The specification text after <c>gen:</c>.</summary>
        private readonly string _text;

        /// <summary>The position of the next character to read.</summary>
        private int _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageSpecification" /> class.
        /// </summary>
        /// <param name="text">The specification text after <c>gen:</c>.</param>
        private MessageSpecification(string text) =>
            _text = text;

        /// <summary>
        /// Builds the <c>.msg</c> file a specification describes.
        /// </summary>
        /// <param name="text">The specification text after <c>gen:</c>, such as <c>Msg(0037001F=Hi)</c>.</param>
        /// <returns>The file's bytes.</returns>
        /// <exception cref="FormatException">The specification, or one of its transforms, is not well formed.</exception>
        internal static byte[] Build(string text)
        {
            GeneratedStorage root = new MessageSpecification(text).ParseMessage();
            using MemoryStream stream = root.ToBuilder().Build();
            byte[] file = stream.ToArray();
            foreach ((string kind, string argument) in root.Transforms)
                file = CompoundImage.Apply(file, kind, argument);

            return file;
        }

        /// <summary>
        /// Reads <c>Msg(items)</c>, the whole specification.
        /// </summary>
        /// <returns>The root storage.</returns>
        /// <exception cref="FormatException">The specification is not well formed.</exception>
        private GeneratedStorage ParseMessage()
        {
            Expect("Msg");
            var root = new GeneratedStorage(MsgPropertyStreamKind.Root);
            ParseList(root);
            if (_position != _text.Length)
                throw Error("text follows the closing parenthesis");

            return root;
        }

        /// <summary>
        /// Reads a parenthesized list of items into a storage.
        /// </summary>
        /// <param name="storage">The storage the items describe.</param>
        /// <exception cref="FormatException">The list is not well formed.</exception>
        private void ParseList(GeneratedStorage storage)
        {
            Expect("(");
            if (TryConsume(')'))
                return;

            while (true)
            {
                if (storage.IsNameId)
                    ParseNameIdItem(storage);
                else
                    ParseItem(storage);

                if (TryConsume(';'))
                    continue;

                Expect(")");
                return;
            }
        }

        /// <summary>
        /// Reads one item of a message, recipient, attachment or other storage.
        /// </summary>
        /// <param name="storage">The storage the item describes.</param>
        /// <exception cref="FormatException">The item is not well formed.</exception>
        private void ParseItem(GeneratedStorage storage)
        {
            string word = ReadWord();
            switch (word)
            {
                case "recip":
                case "attach":
                {
                    string prefix = word == "recip" ? MsgStreamNames.RecipientStoragePrefix : MsgStreamNames.AttachmentStoragePrefix;
                    var child = new GeneratedStorage(MsgPropertyStreamKind.RecipientOrAttachment);
                    ParseList(child);
                    storage.Children.Add((prefix + storage.CountChildren(prefix).ToString("X8", CultureInfo.InvariantCulture), child));
                    return;
                }

                case "embed":
                {
                    var child = new GeneratedStorage(MsgPropertyStreamKind.EmbeddedMessage);
                    ParseList(child);
                    storage.Records.Add(new MsgPropertyEntry(0x37050003, GeneratedStorage.DefaultFlags, 5));
                    storage.Records.Add(new MsgPropertyEntry(0x3701000D, GeneratedStorage.DefaultFlags, 0));
                    storage.Children.Add((MsgStreamNames.EmbeddedMessageStorageName, child));
                    return;
                }

                case "nameid":
                {
                    var child = new GeneratedStorage(MsgPropertyStreamKind.RecipientOrAttachment, isNameId: true);
                    ParseList(child);
                    storage.Children.Add((MsgStreamNames.NameIdStorageName, child));
                    return;
                }

                case "storage":
                {
                    Expect(":");
                    string name = ReadUntil('(');
                    MsgPropertyStreamKind kind = string.Equals(name, MsgStreamNames.EmbeddedMessageStorageName, StringComparison.OrdinalIgnoreCase)
                        ? MsgPropertyStreamKind.EmbeddedMessage
                        : MsgPropertyStreamKind.RecipientOrAttachment;
                    var child = new GeneratedStorage(kind);
                    ParseList(child);
                    storage.Children.Add((name, child));
                    return;
                }

                case "stream":
                {
                    Expect(":");
                    string name = ReadUntil('=');
                    Expect("=");
                    storage.SetStream(name, ParseBytes(ReadValue()));
                    return;
                }

                case "rec":
                {
                    Expect(":");
                    uint tag = ParseTag(ReadUntil('='));
                    Expect("=");
                    string[] parts = Encoding.Latin1.GetString(Unescape(ReadValue())).Split('/');
                    ulong raw = ulong.Parse(parts[0], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                    uint flags = parts.Length > 1
                        ? uint.Parse(parts[1], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
                        : GeneratedStorage.DefaultFlags;
                    storage.Records.Add(new MsgPropertyEntry(tag, flags, raw));
                    return;
                }

                case "size":
                case "flags":
                {
                    Expect(":");
                    uint tag = ParseTag(ReadUntil('='));
                    Expect("=");
                    uint number = (uint)ParseInteger(Encoding.Latin1.GetString(Unescape(ReadValue())));
                    if (word == "size")
                        storage.SetSize(tag, number);
                    else
                        storage.SetFlags(tag, number);

                    return;
                }

                case "cut":
                case "unallocated":
                {
                    if (storage.Kind != MsgPropertyStreamKind.Root)
                        throw Error($"{word}= applies to the whole file and is only allowed at the top level");

                    Expect("=");
                    storage.Transforms.Add((word, Encoding.Latin1.GetString(Unescape(ReadValue()))));
                    return;
                }

                case "reverse":
                case "move":
                {
                    if (storage.Kind != MsgPropertyStreamKind.Root)
                        throw Error($"{word}: applies to the whole file and is only allowed at the top level");

                    Expect(":");
                    storage.Transforms.Add((word, Encoding.Latin1.GetString(Unescape(ReadValue()))));
                    return;
                }

                case "noprops":
                    storage.NoProps = true;
                    return;

                case "props":
                case "hdr":
                case "tail":
                {
                    Expect("=");
                    byte[] bytes = ParseBytes(ReadValue());
                    if (word == "props")
                        storage.Props = bytes;
                    else if (word == "hdr")
                        storage.Header = bytes;
                    else
                        storage.Tail = bytes;

                    return;
                }

                default:
                    if (word.Length == 8 && word.All(char.IsAsciiHexDigit))
                    {
                        Expect("=");
                        AddProperty(storage, ParseTag(word), Unescape(ReadValue()));
                        return;
                    }

                    throw Error($"unknown item '{word}'");
            }
        }

        /// <summary>
        /// Reads one entry of a named-property mapping storage.
        /// </summary>
        /// <param name="storage">The named-property mapping storage.</param>
        /// <exception cref="FormatException">The entry is not well formed.</exception>
        private void ParseNameIdItem(GeneratedStorage storage)
        {
            string word = ReadWord();
            if (word == "stream")
            {
                Expect(":");
                string name = ReadUntil('=');
                Expect("=");
                storage.SetStream(name, ParseBytes(ReadValue()));
                return;
            }

            Expect("=");
            string value = word == "name"
                ? Encoding.UTF8.GetString(Unescape(ReadValue()))
                : Encoding.Latin1.GetString(Unescape(ReadValue()));
            switch (word)
            {
                case "guid":
                    storage.AddGuid(Guid.Parse(value, CultureInfo.InvariantCulture));
                    return;

                case "id":
                case "name":
                {
                    int hash = value.LastIndexOf('#');
                    int? propertyIndex = null;
                    if (hash >= 0)
                    {
                        propertyIndex = (int)ParseInteger(value[(hash + 1)..]);
                        value = value[..hash];
                    }

                    int at = value.LastIndexOf('@');
                    if (at < 0)
                        throw Error($"{word}= needs @ and a GUID index");

                    int guidIndex = value[(at + 1)..] switch
                    {
                        "PS_MAPI" => 1,
                        "PS_PUBLIC_STRINGS" => 2,
                        string index => (int)ParseInteger(index),
                    };
                    string head = value[..at];
                    if (word == "id")
                        storage.AddNumericEntry((uint)ParseInteger(head), guidIndex, propertyIndex);
                    else
                        storage.AddStringEntry(head, guidIndex, propertyIndex);

                    return;
                }

                default:
                    throw Error($"unknown nameid entry '{word}'");
            }
        }

        /// <summary>
        /// Adds a typed property: its record and, for the variable-length and multi-valued types, its streams.
        /// </summary>
        /// <param name="storage">The storage the property belongs to.</param>
        /// <param name="tag">The property tag.</param>
        /// <param name="value">The value's bytes, after <c>%HH</c> decoding.</param>
        /// <exception cref="FormatException">The value is not valid for the tag's type, or the type has no layout.</exception>
        private static void AddProperty(GeneratedStorage storage, uint tag, byte[] value)
        {
            ushort type = (ushort)(tag & 0xFFFF);
            bool multiValued = (type & 0x1000) != 0;
            ushort baseType = (ushort)(type & ~0x1000);
            string streamName = MsgStreamNames.GetSubstgStreamName(tag);
            string text = Encoding.Latin1.GetString(value);

            if (!multiValued)
            {
                AddSingleValue(storage, tag, baseType, value, text, streamName);
                return;
            }

            string[] elements = text.Length == 0 ? [] : text.Split('|');
            if (baseType is 0x001F or 0x001E or 0x0102)
            {
                int width = baseType == 0x0102 ? 8 : 4;
                var lengths = new byte[elements.Length * width];
                for (int i = 0; i < elements.Length; i++)
                {
                    byte[] raw = Encoding.Latin1.GetBytes(elements[i]);
                    byte[] bytes = baseType switch
                    {
                        0x001F => Encoding.Unicode.GetBytes(StrictUtf8(raw)),
                        0x001E => raw,
                        _ => Convert.FromHexString(elements[i]),
                    };
                    int counted = bytes.Length + (baseType == 0x001F ? 2 : baseType == 0x001E ? 1 : 0);
                    BinaryPrimitives.WriteUInt32LittleEndian(lengths.AsSpan(i * width), (uint)counted);
                    storage.SetStream(MsgStreamNames.GetMultiValueElementStreamName(tag, i), bytes);
                }

                storage.Records.Add(new MsgPropertyEntry(tag, GeneratedStorage.DefaultFlags, (ulong)lengths.Length));
                storage.SetStream(streamName, lengths);
                return;
            }

            int size = baseType switch
            {
                0x0002 => 2,
                0x0003 or 0x0004 or 0x000A => 4,
                0x0005 or 0x0006 or 0x0007 or 0x0014 or 0x0040 => 8,
                0x0048 => 16,
                _ => throw new FormatException($"There is no packed layout for the multi-valued type {baseType:X4}."),
            };
            var packed = new byte[elements.Length * size];
            for (int i = 0; i < elements.Length; i++)
            {
                if (baseType == 0x0048)
                {
                    Guid.Parse(elements[i], CultureInfo.InvariantCulture).ToByteArray().CopyTo(packed, i * size);
                    continue;
                }

                ulong raw = FixedRaw(baseType, elements[i]);
                for (int b = 0; b < size; b++)
                    packed[(i * size) + b] = (byte)(raw >> (8 * b));
            }

            storage.Records.Add(new MsgPropertyEntry(tag, GeneratedStorage.DefaultFlags, (ulong)packed.Length));
            storage.SetStream(streamName, packed);
        }

        /// <summary>
        /// Adds a single-valued property's record and, for a variable-length type, its value stream.
        /// </summary>
        /// <param name="storage">The storage the property belongs to.</param>
        /// <param name="tag">The property tag.</param>
        /// <param name="type">The tag's type.</param>
        /// <param name="value">The value's bytes, after <c>%HH</c> decoding.</param>
        /// <param name="text">The value's bytes read as Latin-1 text.</param>
        /// <param name="streamName">The name of the property's value stream.</param>
        /// <exception cref="FormatException">The value is not valid for the type, or the type has no inline layout.</exception>
        private static void AddSingleValue(GeneratedStorage storage, uint tag, ushort type, byte[] value, string text, string streamName)
        {
            switch (type)
            {
                case 0x001F:
                {
                    byte[] bytes = Encoding.Unicode.GetBytes(StrictUtf8(value));
                    storage.Records.Add(new MsgPropertyEntry(tag, GeneratedStorage.DefaultFlags, (ulong)(bytes.Length + 2)));
                    storage.SetStream(streamName, bytes);
                    return;
                }

                case 0x001E:
                    storage.Records.Add(new MsgPropertyEntry(tag, GeneratedStorage.DefaultFlags, (ulong)(value.Length + 1)));
                    storage.SetStream(streamName, value);
                    return;

                case 0x0102:
                {
                    byte[] bytes = Convert.FromHexString(text);
                    storage.Records.Add(new MsgPropertyEntry(tag, GeneratedStorage.DefaultFlags, (ulong)bytes.Length));
                    storage.SetStream(streamName, bytes);
                    return;
                }

                case 0x0048:
                    storage.Records.Add(new MsgPropertyEntry(tag, GeneratedStorage.DefaultFlags, 16));
                    storage.SetStream(streamName, Guid.Parse(text, CultureInfo.InvariantCulture).ToByteArray());
                    return;

                case 0x000D:
                    storage.Records.Add(new MsgPropertyEntry(tag, GeneratedStorage.DefaultFlags, text.Length == 0 ? 0 : (ulong)ParseInteger(text)));
                    return;

                default:
                    storage.Records.Add(new MsgPropertyEntry(tag, GeneratedStorage.DefaultFlags, FixedRaw(type, text)));
                    return;
            }
        }

        /// <summary>
        /// Encodes a fixed-length value into its 8-byte little-endian value field.
        /// </summary>
        /// <param name="type">The value's type.</param>
        /// <param name="text">The value as written: <c>0x</c> and up to 16 hexadecimal digits for the whole field, or the value in the type's notation.</param>
        /// <returns>The value field as a 64-bit integer.</returns>
        /// <exception cref="FormatException">The text is not valid for the type, or the type has no inline layout.</exception>
        private static ulong FixedRaw(ushort type, string text)
        {
            if (text.StartsWith("0x", StringComparison.Ordinal))
                return ulong.Parse(text.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

            return type switch
            {
                0x0002 => (ushort)(short)ParseInteger(text),
                0x0003 or 0x000A => (uint)(int)ParseInteger(text),
                0x0014 or 0x0000 or 0x0001 => (ulong)ParseInteger(text),
                0x000B => text switch
                {
                    "true" => 1UL,
                    "false" => 0UL,
                    _ => (ulong)ParseInteger(text),
                },
                0x0004 => (uint)BitConverter.SingleToInt32Bits(float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture)),
                0x0005 or 0x0007 => (ulong)BitConverter.DoubleToInt64Bits(double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture)),
                0x0006 => (ulong)(long)(decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture) * 10000m),
                0x0040 => (ulong)DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime.ToFileTimeUtc(),
                _ => throw new FormatException($"There is no inline layout for type {type:X4}; use rec:."),
            };
        }

        /// <summary>
        /// Parses bytes written as parts joined by <c>+</c>: <c>x:</c> and hexadecimal digits, <c>t:</c> and text,
        /// <c>u:</c> and UTF-8 text written as UTF-16LE, or <c>z:</c> and a count of zero bytes.
        /// </summary>
        /// <param name="raw">The bytes as written; empty for no bytes.</param>
        /// <returns>The bytes.</returns>
        /// <exception cref="FormatException">A part has no known prefix or is not valid for its prefix.</exception>
        private static byte[] ParseBytes(string raw)
        {
            var output = new List<byte>();
            if (raw.Length == 0)
                return [];

            foreach (string part in raw.Split('+'))
            {
                if (part.Length < 2 || part[1] != ':')
                    throw new FormatException($"The bytes '{part}' do not start with x:, t:, u: or z:.");

                byte[] body = Unescape(part[2..]);
                switch (part[0])
                {
                    case 'x':
                        output.AddRange(Convert.FromHexString(Encoding.Latin1.GetString(body)));
                        break;

                    case 't':
                        output.AddRange(body);
                        break;

                    case 'u':
                        output.AddRange(Encoding.Unicode.GetBytes(StrictUtf8(body)));
                        break;

                    case 'z':
                        output.AddRange(new byte[ParseInteger(Encoding.Latin1.GetString(body))]);
                        break;

                    default:
                        throw new FormatException($"The bytes '{part}' do not start with x:, t:, u: or z:.");
                }
            }

            return [.. output];
        }

        /// <summary>
        /// Reads bytes as strict UTF-8 text.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>The text.</returns>
        /// <exception cref="DecoderFallbackException">The bytes are not valid UTF-8.</exception>
        private static string StrictUtf8(byte[] bytes) =>
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes);

        /// <summary>
        /// Decodes <c>%HH</c> escapes into bytes; every other character is its Latin-1 byte.
        /// </summary>
        /// <param name="text">The value as written.</param>
        /// <returns>The value's bytes.</returns>
        /// <exception cref="FormatException">A <c>%</c> is not followed by two hexadecimal digits.</exception>
        /// <exception cref="OverflowException">A character is above U+00FF.</exception>
        private static byte[] Unescape(string text)
        {
            var output = new List<byte>(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '%')
                {
                    if (i + 2 >= text.Length || !char.IsAsciiHexDigit(text[i + 1]) || !char.IsAsciiHexDigit(text[i + 2]))
                        throw new FormatException("A % is not followed by two hexadecimal digits.");

                    output.Add(byte.Parse(text.AsSpan(i + 1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
                    i += 2;
                    continue;
                }

                output.Add(checked((byte)text[i]));
            }

            return [.. output];
        }

        /// <summary>
        /// Parses an integer written in decimal, with an optional sign, or as <c>0x</c> and hexadecimal digits.
        /// </summary>
        /// <param name="text">The integer as written.</param>
        /// <returns>The integer.</returns>
        /// <exception cref="FormatException">The text is not an integer.</exception>
        private static long ParseInteger(string text) =>
            text.StartsWith("0x", StringComparison.Ordinal)
                ? (long)ulong.Parse(text.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
                : long.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

        /// <summary>
        /// Parses a property tag written as eight hexadecimal digits.
        /// </summary>
        /// <param name="text">The tag as written.</param>
        /// <returns>The tag.</returns>
        /// <exception cref="FormatException">The text is not eight hexadecimal digits.</exception>
        private static uint ParseTag(string text) =>
            text.Length == 8 && text.All(char.IsAsciiHexDigit)
                ? uint.Parse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
                : throw new FormatException($"'{text}' is not an eight-digit hexadecimal tag.");

        /// <summary>
        /// Reads a word of ASCII letters, digits and underscores.
        /// </summary>
        /// <returns>The word.</returns>
        /// <exception cref="FormatException">There is no word at the current position.</exception>
        private string ReadWord()
        {
            int start = _position;
            while (_position < _text.Length && (char.IsAsciiLetterOrDigit(_text[_position]) || _text[_position] == '_'))
                _position++;

            if (_position == start)
                throw Error("expected a word");

            return _text[start.._position];
        }

        /// <summary>
        /// Reads the text up to, but not including, the next occurrence of a character.
        /// </summary>
        /// <param name="stop">The character that ends the text.</param>
        /// <returns>The text, taken literally.</returns>
        /// <exception cref="FormatException">The character does not occur.</exception>
        private string ReadUntil(char stop)
        {
            int end = _text.IndexOf(stop, _position);
            if (end < 0)
                throw Error($"expected '{stop}'");

            string value = _text[_position..end];
            _position = end;
            return value;
        }

        /// <summary>
        /// Reads a value up to the next <c>;</c> or <c>)</c>.
        /// </summary>
        /// <returns>The value as written.</returns>
        private string ReadValue()
        {
            int start = _position;
            while (_position < _text.Length && _text[_position] is not (';' or ')'))
                _position++;

            return _text[start.._position];
        }

        /// <summary>
        /// Consumes a character when it is the next one.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns><see langword="true" /> when the character was consumed.</returns>
        private bool TryConsume(char c)
        {
            if (_position < _text.Length && _text[_position] == c)
            {
                _position++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Consumes a token that must come next.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <exception cref="FormatException">The token does not come next.</exception>
        private void Expect(string token)
        {
            if (string.CompareOrdinal(_text, _position, token, 0, token.Length) != 0)
                throw Error($"expected '{token}'");

            _position += token.Length;
        }

        /// <summary>
        /// Creates the exception for a specification that is not well formed.
        /// </summary>
        /// <param name="message">What is wrong.</param>
        /// <returns>The exception, naming the position and the specification.</returns>
        private FormatException Error(string message) =>
            new($"gen: {message} at position {_position} of '{_text}'.");
    }
}
