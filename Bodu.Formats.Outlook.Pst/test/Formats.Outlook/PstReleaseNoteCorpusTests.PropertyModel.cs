// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.PropertyModel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one property a generator writes: a record of a property context, or a cell of a table row.
    /// </summary>
    /// <remarks>
    /// A value of type 0x0000 to 0x0004, 0x000A or 0x000B is held inline in its record (<see cref="Inline" />); any
    /// other value is held as bytes (<see cref="Data" />), stored where <see cref="Placement" /> says.
    /// </remarks>
    private sealed class PropertyModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyModel" /> class.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="type">The wire type, with the multi-valued flag when it applies.</param>
        public PropertyModel(ushort id, ushort type)
        {
            Id = id;
            Type = type;
        }

        /// <summary>
        /// Gets the property identifier.
        /// </summary>
        public ushort Id { get; }

        /// <summary>
        /// Gets the wire type, with the multi-valued flag when it applies.
        /// </summary>
        public ushort Type { get; }

        /// <summary>
        /// Gets or sets the inline value of a 1-, 2- or 4-byte type.
        /// </summary>
        public uint Inline { get; set; }

        /// <summary>
        /// Gets or sets the value bytes of every other type.
        /// </summary>
        public byte[] Data { get; set; } = [];

        /// <summary>
        /// Gets or sets where a value that is not inline is stored.
        /// </summary>
        public ValuePlacement Placement { get; set; }

        /// <summary>
        /// Gets or sets the value reference written for <see cref="ValuePlacement.Raw" />.
        /// </summary>
        public uint RawReference { get; set; }

        /// <summary>
        /// Gets the property tag: the identifier in the high word and the type in the low word.
        /// </summary>
        public uint Tag =>
            ((uint)Id << 16) | Type;

        /// <summary>
        /// Gets a value indicating whether the value is held inline in its record.
        /// </summary>
        public bool IsInline =>
            Type is 0x0000 or 0x0001 or 0x0002 or 0x0003 or 0x0004 or 0x000A or 0x000B;

        /// <summary>
        /// Gets the width of the cell a table column of this type uses.
        /// </summary>
        /// <value>
        /// 1 for a Boolean, 2 for a 16-bit integer, 8 for the 8-byte fixed types, and 4 for every other type.
        /// </value>
        public int CellWidth =>
            Type switch
            {
                0x000B => 1,
                0x0002 => 2,
                0x0005 or 0x0006 or 0x0007 or 0x0014 or 0x0040 => 8,
                _ => 4,
            };

        /// <summary>
        /// Creates a 32-bit integer property.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="value">The value.</param>
        /// <returns>The property.</returns>
        public static PropertyModel Int32(ushort id, int value) =>
            new(id, 0x0003) { Inline = (uint)value };

        /// <summary>
        /// Creates a Boolean property.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="value">The value.</param>
        /// <returns>The property.</returns>
        public static PropertyModel Boolean(ushort id, bool value) =>
            new(id, 0x000B) { Inline = value ? 1u : 0u };

        /// <summary>
        /// Creates a Unicode string property, stored as UTF-16LE without a terminator.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="value">The value.</param>
        /// <returns>The property.</returns>
        public static PropertyModel Unicode(ushort id, string value) =>
            new(id, 0x001F) { Data = Encoding.Unicode.GetBytes(value) };

        /// <summary>
        /// Creates an 8-bit string property, stored in the given encoding without a terminator.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="value">The value.</param>
        /// <param name="encoding">The encoding of the object that owns the property.</param>
        /// <returns>The property.</returns>
        public static PropertyModel String8(ushort id, string value, Encoding encoding) =>
            new(id, 0x001E) { Data = encoding.GetBytes(value) };

        /// <summary>
        /// Creates a binary property.
        /// </summary>
        /// <param name="id">The property identifier.</param>
        /// <param name="value">The value.</param>
        /// <returns>The property.</returns>
        public static PropertyModel Binary(ushort id, byte[] value) =>
            new(id, 0x0102) { Data = value };

        /// <summary>
        /// Reads a <c>prop</c> specification: <c>&lt;tag&gt;[/heap|/subnode|/null|/raw=v]:&lt;value&gt;</c>.
        /// </summary>
        /// <param name="spec">The specification.</param>
        /// <param name="encoding">The encoding of the object that owns the property, for an 8-bit string value.</param>
        /// <returns>The property.</returns>
        /// <exception cref="FormatException">The specification or its value is malformed.</exception>
        /// <remarks>
        /// The tag is eight hexadecimal digits. The value is read by the wire type: an integer for an inline type;
        /// UTF-16LE text for 0x001F; text in <paramref name="encoding" /> for 0x001E; hexadecimal digits,
        /// <c>utf8:&lt;text&gt;</c> or <c>fill:n</c> for 0x0102 and 0x000D; a GUID for 0x0048; an ISO 8601 date and
        /// time (stored as the UTC FILETIME) or an integer for 0x0040; <c>d:&lt;double&gt;</c> or an integer for 0x0005
        /// and 0x0007; an integer for 0x0006 and 0x0014; and hexadecimal digits for any other type.
        /// <c>raw:&lt;hex&gt;</c> gives the exact bytes of any value that is not inline. A multi-valued value lists its
        /// items separated by <c>;</c>: a variable-size type as a count, offsets and the items (MS-PST 2.3.3.4.2), and
        /// a fixed-size type packed.
        /// </remarks>
        public static PropertyModel Parse(string spec, Encoding encoding)
        {
            int colon = spec.IndexOf(':', StringComparison.Ordinal);
            if (colon < 0) throw new FormatException($"gen: the property '{spec}' is not <tag>[/placement]:<value>.");

            string head = spec[..colon];
            string value = spec[(colon + 1)..];
            int slash = head.IndexOf('/', StringComparison.Ordinal);
            string tagText = slash < 0 ? head : head[..slash];
            if (tagText.Length != 8 || !uint.TryParse(tagText, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint tag))
                throw new FormatException($"gen: the property tag '{tagText}' is not eight hexadecimal digits.");

            var property = new PropertyModel((ushort)(tag >> 16), (ushort)tag);
            var elementType = (ushort)(property.Type & ~0x1000);
            bool multiValued = (property.Type & 0x1000) != 0;
            if (property.IsInline)
                property.Inline = value.Length == 0 ? 0 : (uint)GeneratorCall.ParseInteger(value);
            else if (value.StartsWith("raw:", StringComparison.Ordinal))
                property.Data = GeneratorCall.ParseHex(value[4..]);
            else if (!multiValued)
                property.Data = ParseScalar(elementType, value, encoding);
            else if (elementType is 0x001F or 0x001E or 0x0102)
                property.Data = PackVariable([.. Items(value).Select(item => ParseScalar(elementType, item, encoding))]);
            else
                property.Data = [.. Items(value).SelectMany(item => ParsePacked(elementType, item))];

            string? placement = slash < 0 ? null : head[(slash + 1)..];
            switch (placement)
            {
                case null:
                    break;

                case "heap":
                    property.Placement = ValuePlacement.Heap;
                    break;

                case "subnode":
                    property.Placement = ValuePlacement.Subnode;
                    break;

                case "null":
                    property.Placement = ValuePlacement.Null;
                    break;

                default:
                    if (!placement.StartsWith("raw=", StringComparison.Ordinal))
                        throw new FormatException($"gen: the placement '{placement}' is not heap, subnode, null or raw=v.");

                    property.Placement = ValuePlacement.Raw;
                    property.RawReference = (uint)GeneratorCall.ParseInteger(placement[4..]);
                    break;
            }

            return property;
        }

        /// <summary>
        /// Splits a multi-valued value into its items.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The items, separated by <c>;</c>; none for an empty value.</returns>
        private static string[] Items(string value) =>
            value.Length == 0 ? [] : value.Split(';');

        /// <summary>
        /// Lays out the items of a variable-size multi-valued value: the count, one offset per item, then the items
        /// (MS-PST 2.3.3.4.2).
        /// </summary>
        /// <param name="items">The item bytes.</param>
        /// <returns>The value bytes.</returns>
        private static byte[] PackVariable(byte[][] items)
        {
            var payload = new byte[4 + (items.Length * 4) + items.Sum(item => item.Length)];
            BinaryPrimitives.WriteInt32LittleEndian(payload, items.Length);
            int offset = 4 + (items.Length * 4);
            for (int i = 0; i < items.Length; i++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4 + (i * 4)), offset);
                items[i].CopyTo(payload, offset);
                offset += items[i].Length;
            }

            return payload;
        }

        /// <summary>
        /// Reads a single value that is not inline by its type.
        /// </summary>
        /// <param name="type">The wire type, without the multi-valued flag.</param>
        /// <param name="value">The value text.</param>
        /// <param name="encoding">The encoding of an 8-bit string value.</param>
        /// <returns>The value bytes.</returns>
        /// <exception cref="FormatException">The value text does not fit the type.</exception>
        private static byte[] ParseScalar(ushort type, string value, Encoding encoding) =>
            type switch
            {
                0x001F => Encoding.Unicode.GetBytes(value),
                0x001E => encoding.GetBytes(value),
                0x0102 or 0x000D when value.StartsWith("utf8:", StringComparison.Ordinal) => Encoding.UTF8.GetBytes(value[5..]),
                0x0102 or 0x000D when value.StartsWith("fill:", StringComparison.Ordinal) => InputGenerator.Fill((int)GeneratorCall.ParseInteger(value[5..])),
                0x0048 => Guid.Parse(value, CultureInfo.InvariantCulture).ToByteArray(),
                0x0040 when value.Contains('T', StringComparison.Ordinal) =>
                    Little64(DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).UtcDateTime.ToFileTimeUtc()),
                0x0005 or 0x0007 when value.StartsWith("d:", StringComparison.Ordinal) =>
                    Little64(BitConverter.DoubleToInt64Bits(double.Parse(value[2..], NumberStyles.Float, CultureInfo.InvariantCulture))),
                0x0005 or 0x0006 or 0x0007 or 0x0014 or 0x0040 => Little64(GeneratorCall.ParseInteger(value)),
                _ => GeneratorCall.ParseHex(value),
            };

        /// <summary>
        /// Reads one item of a fixed-size multi-valued value.
        /// </summary>
        /// <param name="type">The wire type, without the multi-valued flag.</param>
        /// <param name="item">The item text.</param>
        /// <returns>The item bytes.</returns>
        /// <exception cref="FormatException">The item text does not fit the type.</exception>
        private static byte[] ParsePacked(ushort type, string item)
        {
            switch (type)
            {
                case 0x0002:
                    var int16 = new byte[2];
                    BinaryPrimitives.WriteInt16LittleEndian(int16, (short)GeneratorCall.ParseInteger(item));
                    return int16;

                case 0x0003:
                    var int32 = new byte[4];
                    BinaryPrimitives.WriteInt32LittleEndian(int32, (int)GeneratorCall.ParseInteger(item));
                    return int32;

                default:
                    return ParseScalar(type, item, Encoding.Latin1);
            }
        }

        /// <summary>
        /// Writes a 64-bit value little-endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The eight bytes.</returns>
        private static byte[] Little64(long value)
        {
            var bytes = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(bytes, value);

            return bytes;
        }
    }
}
