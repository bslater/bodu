// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.RecordBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

using Bodu.IO.Biff;
using Bodu.Test.Corpus;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Lays out the records a generator's constructors describe, byte by byte from [MS-XLS], independently of
    /// <see cref="BiffWriter" />.
    /// </summary>
    /// <remarks>
    /// <c>corpus/biff/README.md</c> lists the constructors and their layouts. A BIFF8 string is written as an
    /// XLUnicodeString, compressed when every character is at most U+00FF and 16-bit otherwise; a BIFF5 string, and any
    /// <c>cpN'...'</c> text, as a byte string in its code page.
    /// </remarks>
    private static class RecordBuilder
    {
        /// <summary>The largest payload a record of the shared string table holds before it continues.</summary>
        private const int MaximumSstPayload = 8_224;

        /// <summary>
        /// Builds the bytes of one record constructor.
        /// </summary>
        /// <param name="item">The constructor.</param>
        /// <param name="context">The generator context.</param>
        /// <param name="substreamStarts">
        /// The offset of each substream, or <see langword="null" /> while the records are being measured.
        /// </param>
        /// <returns>
        /// The record, header included, or for <c>HDR</c>, <c>RAW</c> and <c>SSTN</c> the bytes they stand for.
        /// </returns>
        /// <exception cref="FormatException">The constructor is unknown, or an argument is malformed.</exception>
        public static byte[] Build(GeneratorItem item, GeneratorContext context, long[]? substreamStarts)
        {
            IReadOnlyList<string> a = item.Arguments;
            bool biff8 = context.Version == BiffVersion.Biff8;
            switch (item.Name)
            {
                case "BOF":
                {
                    Require(item, 1, 1);
                    PayloadBuilder bof = new PayloadBuilder().U16((ushort)(biff8 ? 0x0600 : 0x0500)).U16(ParseSubstreamType(a[0])).U16(0x0DBB).U16(0x07CC);
                    if (biff8)
                        bof.U32(0x000040C1).U32(0x00000206);

                    return Frame(BiffRecordType.Bof, bof);
                }

                case "EOF":
                    Require(item, 0, 0);
                    return Frame(BiffRecordType.Eof, new PayloadBuilder());

                case "SHEET":
                case "BOUNDSHEET":
                {
                    Require(item, 2, 4);
                    long offset = item.Name == "SHEET" ? ResolveSubstream(a[0], substreamStarts) : ParseInt64(a[0]);
                    PayloadBuilder sheet = new PayloadBuilder().U32((uint)offset);
                    sheet.U8(a.Count > 2 ? ParseSheetState(a[2]) : (byte)0).U8(a.Count > 3 ? ParseSheetType(a[3]) : (byte)0);
                    sheet.Text(ParseText(a[1]), context, wideLength: false);
                    return Frame(BiffRecordType.BoundSheet, sheet);
                }

                case "CODEPAGE":
                    Require(item, 1, 1);
                    return Frame(BiffRecordType.CodePage, new PayloadBuilder().U16((ushort)ParseInt32(a[0])));

                case "DATEMODE":
                    Require(item, 1, 1);
                    return Frame(BiffRecordType.DateMode, new PayloadBuilder().U16((ushort)ParseInt32(a[0])));

                case "DIMENSIONS":
                {
                    Require(item, 4, 4);
                    var dimensions = new PayloadBuilder();
                    if (biff8)
                        dimensions.U32((uint)ParseInt64(a[0])).U32((uint)ParseInt64(a[1]));
                    else
                        dimensions.U16((ushort)ParseInt32(a[0])).U16((ushort)ParseInt32(a[1]));

                    dimensions.U16((ushort)ParseInt32(a[2])).U16((ushort)ParseInt32(a[3])).U16(0);
                    return Frame(BiffRecordType.Dimensions, dimensions);
                }

                case "XF":
                    Require(item, 2, 2);
                    return Frame(BiffRecordType.Xf, new PayloadBuilder().U16((ushort)ParseInt32(a[0])).U16((ushort)ParseInt32(a[1])).Zeros(biff8 ? 16 : 12));

                case "FORMAT":
                    Require(item, 2, 2);
                    return Frame(BiffRecordType.Format, new PayloadBuilder().U16((ushort)ParseInt32(a[0])).Text(ParseText(a[1]), context, wideLength: biff8));

                case "SST":
                {
                    PayloadBuilder table = new PayloadBuilder().U32((uint)a.Count).U32((uint)a.Count);
                    foreach (string text in a)
                        table.Unicode(ParseText(text), wideLength: true);

                    return Frame(BiffRecordType.Sst, table);
                }

                case "SSTN":
                    Require(item, 1, 2);
                    return BuildNumberedSst(ParseInt32(a[0]), a.Count > 1 ? ParseText(a[1]).Text : "s");

                case "NUMBER":
                    Require(item, 3, 4);
                    return Frame(BiffRecordType.Number, StartCell(a, 3).F64(ParseDouble(a[2])));

                case "RK":
                    Require(item, 3, 4);
                    return Frame(BiffRecordType.Rk, StartCell(a, 3).U32((uint)ParseInt64(a[2])));

                case "MULRK":
                {
                    if (a.Count < 3)
                        throw new FormatException("MULRK(r,c,xf:rk,...) needs at least one cell.");

                    int first = ParseInt32(a[1]);
                    PayloadBuilder cells = new PayloadBuilder().U16((ushort)ParseInt32(a[0])).U16((ushort)first);
                    for (var i = 2; i < a.Count; i++)
                    {
                        string[] pair = a[i].Split(':');
                        if (pair.Length != 2)
                            throw new FormatException($"The MULRK cell '{a[i]}' is not written xf:rk.");

                        cells.U16((ushort)ParseInt32(pair[0])).U32((uint)ParseInt64(pair[1]));
                    }

                    return Frame(BiffRecordType.MulRk, cells.U16((ushort)(first + a.Count - 3)));
                }

                case "BLANK":
                    Require(item, 2, 3);
                    return Frame(BiffRecordType.Blank, StartCell(a, 2));

                case "MULBLANK":
                {
                    if (a.Count < 3)
                        throw new FormatException("MULBLANK(r,c,xf,...) needs at least one cell.");

                    int first = ParseInt32(a[1]);
                    PayloadBuilder cells = new PayloadBuilder().U16((ushort)ParseInt32(a[0])).U16((ushort)first);
                    for (var i = 2; i < a.Count; i++)
                        cells.U16((ushort)ParseInt32(a[i]));

                    return Frame(BiffRecordType.MulBlank, cells.U16((ushort)(first + a.Count - 3)));
                }

                case "BOOLERR":
                    Require(item, 4, 5);
                    return Frame(BiffRecordType.BoolErr, StartCell(a, 4).U8((byte)ParseInt32(a[2])).U8((byte)ParseInt32(a[3])));

                case "LABEL":
                    Require(item, 3, 4);
                    return Frame(BiffRecordType.Label, StartCell(a, 3).Text(ParseText(a[2]), context, wideLength: true));

                case "LABELSST":
                    Require(item, 3, 4);
                    return Frame(BiffRecordType.LabelSst, StartCell(a, 3).U32((uint)ParseInt64(a[2])));

                case "RSTRING":
                {
                    Require(item, 4, 5);
                    TextArgument text = ParseText(a[2]);
                    byte[] bytes = EncodeCodePage(text.CodePage != 0 ? text.CodePage : context.CodePage, text.Text);
                    int runs = ParseInt32(a[3]);
                    PayloadBuilder label = StartCell(a, 4).U16((ushort)bytes.Length).Bytes(bytes).U8((byte)runs);
                    for (var i = 0; i < runs; i++)
                        label.U8((byte)i).U8(0);

                    return Frame(BiffRecordType.RString, label);
                }

                case "FORMULA":
                {
                    Require(item, 3, 5);
                    byte[] tokens = a.Count > 4 ? ParsePayload(a[4]) : [];
                    PayloadBuilder formula = StartCell(a, 3).Bytes(ParseFormulaResult(a[2]));
                    return Frame(BiffRecordType.Formula, formula.U16(0).U32(0).U16((ushort)tokens.Length).Bytes(tokens));
                }

                case "STRING":
                    Require(item, 1, 1);
                    return Frame(BiffRecordType.String, new PayloadBuilder().Text(ParseText(a[0]), context, wideLength: true));

                case "ROW":
                    Require(item, 3, 3);
                    return Frame(BiffRecordType.Row, new PayloadBuilder().U16((ushort)ParseInt32(a[0])).U16((ushort)ParseInt32(a[1])).U16((ushort)ParseInt32(a[2])).Zeros(10));

                case "REC":
                    Require(item, 1, 2);
                    return Frame(ParseRecordId(a[0]), new PayloadBuilder().Bytes(a.Count > 1 ? ParsePayload(a[1]) : []));

                case "HDR":
                {
                    Require(item, 2, 2);
                    var header = new byte[4];
                    BinaryPrimitives.WriteUInt16LittleEndian(header, ParseRecordId(a[0]));
                    BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2), (ushort)ParseInt32(a[1]));
                    return header;
                }

                case "RAW":
                    Require(item, 1, 1);
                    return ParsePayload(a[0]);

                default:
                    throw new FormatException($"The record constructor '{item.Name}' is not one the generators know.");
            }
        }

        /// <summary>
        /// Parses a 32-bit integer argument: decimal, or hexadecimal after <c>0x</c>, with an optional leading minus.
        /// </summary>
        /// <param name="text">The argument.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">
        /// <paramref name="text" /> is not an integer, or is out of range.
        /// </exception>
        public static int ParseInt32(string text)
        {
            long value = ParseInt64(text);
            return value is >= int.MinValue and <= int.MaxValue
                ? (int)value
                : throw new FormatException($"The value '{text}' is outside the range of a 32-bit integer.");
        }

        /// <summary>
        /// Parses a 64-bit integer argument: decimal, or hexadecimal after <c>0x</c>, with an optional leading minus.
        /// </summary>
        /// <param name="text">The argument.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">
        /// <paramref name="text" /> is not an integer, or is out of range.
        /// </exception>
        public static long ParseInt64(string text)
        {
            ReadOnlySpan<char> digits = text.AsSpan().Trim();
            bool negative = digits is ['-', ..];
            if (negative)
                digits = digits[1..];

            bool hexadecimal = digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            long value;
            bool parsed = hexadecimal
                ? long.TryParse(digits[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value)
                : long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
            if (!parsed)
                throw new FormatException($"The value '{text}' is not a decimal or 0x-prefixed hexadecimal integer.");

            return negative ? -value : value;
        }

        /// <summary>
        /// Parses a payload: hexadecimal runs, typed little-endian values, zero runs and quoted text items.
        /// </summary>
        /// <param name="text">The payload items, separated by white space.</param>
        /// <returns>The bytes.</returns>
        /// <exception cref="FormatException">An item is malformed.</exception>
        /// <remarks>
        /// An item is an even run of hexadecimal digits; <c>u8:N</c>, <c>u16:N</c>, <c>i16:N</c>, <c>u32:N</c>,
        /// <c>i32:N</c> or <c>f64:X</c>; <c>z:N</c> for <c>N</c> zero bytes; or a quoted text item, which writes
        /// characters only: <c>'...'</c> one byte per character (each at most U+00FF), <c>w'...'</c> UTF-16LE and
        /// <c>cpN'...'</c> the bytes of code page <c>N</c>. A quoted item may be followed by <c>*N</c> to repeat its
        /// text <c>N</c> times.
        /// </remarks>
        public static byte[] ParsePayload(string text)
        {
            var payload = new PayloadBuilder();
            var i = 0;
            while (true)
            {
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                    i++;

                if (i >= text.Length)
                    return payload.ToArray();

                int start = i;
                var quoted = false;
                while (i < text.Length && !char.IsWhiteSpace(text[i]))
                {
                    if (text[i] == '\'')
                    {
                        quoted = true;
                        int end = text.IndexOf('\'', i + 1);
                        if (end < 0)
                            throw new FormatException("A quote in a payload is not closed.");

                        i = end + 1;
                        break;
                    }

                    i++;
                }

                string token = text[start..i];
                if (quoted)
                {
                    var repeat = 1;
                    if (i < text.Length && text[i] == '*')
                    {
                        int digits = ++i;
                        while (i < text.Length && char.IsAsciiDigit(text[i]))
                            i++;

                        repeat = ParseInt32(text[digits..i]);
                    }

                    AppendTextItem(payload, ParseText(token), repeat);
                    continue;
                }

                int colon = token.IndexOf(':', StringComparison.Ordinal);
                if (colon > 0)
                {
                    AppendTypedItem(payload, token[..colon], token[(colon + 1)..]);
                    continue;
                }

                if (token.Length % 2 != 0 || !token.All(char.IsAsciiHexDigit))
                    throw new FormatException($"The payload item '{token}' is not an even run of hexadecimal digits.");

                payload.Bytes(Convert.FromHexString(token));
            }
        }

        /// <summary>
        /// Encodes text in a code page, without registering a code-page provider for the whole process.
        /// </summary>
        /// <param name="codePage">The code page.</param>
        /// <param name="text">The text.</param>
        /// <returns>The bytes of <paramref name="text" /> in <paramref name="codePage" />.</returns>
        /// <exception cref="FormatException">No encoding is available for <paramref name="codePage" />.</exception>
        public static byte[] EncodeCodePage(int codePage, string text)
        {
            Encoding encoding;
            try
            {
                encoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.GetEncoding(codePage);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                throw new FormatException($"The code page {codePage} has no encoding.", ex);
            }

            return encoding.GetBytes(text);
        }

        /// <summary>
        /// Appends a quoted text item of a payload.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="text">The text item.</param>
        /// <param name="repeat">The number of times its text is written.</param>
        /// <exception cref="FormatException">A one-byte text holds a character above U+00FF.</exception>
        private static void AppendTextItem(PayloadBuilder payload, TextArgument text, int repeat)
        {
            string value = string.Concat(Enumerable.Repeat(text.Text, repeat));
            if (text.Wide)
            {
                payload.Bytes(Encoding.Unicode.GetBytes(value));
            }
            else if (text.CodePage != 0)
            {
                payload.Bytes(EncodeCodePage(text.CodePage, value));
            }
            else
            {
                foreach (char c in value)
                {
                    if (c > 0xFF)
                        throw new FormatException($"The text item '{text.Text}' holds a character above U+00FF; write it w'...'.");

                    payload.U8((byte)c);
                }
            }
        }

        /// <summary>
        /// Appends a typed payload item.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="kind">
        /// The item kind: <c>u8</c>, <c>u16</c>, <c>i16</c>, <c>u32</c>, <c>i32</c>, <c>f64</c> or <c>z</c>.
        /// </param>
        /// <param name="value">The value text.</param>
        /// <exception cref="FormatException">The kind is unknown, or the value is malformed.</exception>
        private static void AppendTypedItem(PayloadBuilder payload, string kind, string value)
        {
            switch (kind)
            {
                case "u8":
                    payload.U8((byte)ParseInt32(value));
                    break;

                case "u16":
                    payload.U16((ushort)ParseInt32(value));
                    break;

                case "i16":
                    payload.U16(unchecked((ushort)(short)ParseInt32(value)));
                    break;

                case "u32":
                    payload.U32((uint)ParseInt64(value));
                    break;

                case "i32":
                    payload.U32(unchecked((uint)(int)ParseInt64(value)));
                    break;

                case "f64":
                    payload.F64(ParseDouble(value));
                    break;

                case "z":
                    payload.Zeros(ParseInt32(value));
                    break;

                default:
                    throw new FormatException($"The payload item kind '{kind}' is not one the generators know.");
            }
        }

        /// <summary>
        /// Builds an SST of numbered compressed strings, continued in CONTINUE records.
        /// </summary>
        /// <param name="count">The number of strings.</param>
        /// <param name="prefix">The text each string starts with, before its index.</param>
        /// <returns>
        /// The SST record and its CONTINUE records, holding the strings <c>prefix0</c> to <c>prefix(count-1)</c>; no
        /// record holds more than 8,224 bytes of payload, and no string is split across records.
        /// </returns>
        /// <exception cref="FormatException"><paramref name="prefix" /> holds a character above U+00FF.</exception>
        private static byte[] BuildNumberedSst(int count, string prefix)
        {
            if (prefix.Any(c => c > 0xFF))
                throw new FormatException($"The SSTN prefix '{prefix}' holds a character above U+00FF.");

            using var output = new MemoryStream();
            BiffRecordType recordType = BiffRecordType.Sst;
            PayloadBuilder block = new PayloadBuilder().U32((uint)count).U32((uint)count);
            for (var i = 0; i < count; i++)
            {
                string text = prefix + i.ToString(CultureInfo.InvariantCulture);
                if (block.Length + 3 + text.Length > MaximumSstPayload)
                {
                    output.Write(Frame(recordType, block));
                    recordType = BiffRecordType.Continue;
                    block = new PayloadBuilder();
                }

                // A compressed XLUnicodeRichExtendedString: the character count, flags zero, one byte per character.
                block.U16((ushort)text.Length).U8(0);
                foreach (char c in text)
                    block.U8((byte)c);
            }

            output.Write(Frame(recordType, block));
            return output.ToArray();
        }

        /// <summary>
        /// Starts a cell record's payload: its row, its column and its XF index.
        /// </summary>
        /// <param name="a">The constructor's arguments, the row and column first.</param>
        /// <param name="xfArgument">The position of the optional XF index argument.</param>
        /// <returns>
        /// The payload, holding the row, the column and the XF index, zero when the argument is absent.
        /// </returns>
        /// <exception cref="FormatException">An argument is malformed.</exception>
        private static PayloadBuilder StartCell(IReadOnlyList<string> a, int xfArgument) =>
            new PayloadBuilder().U16((ushort)ParseInt32(a[0])).U16((ushort)ParseInt32(a[1])).U16((ushort)(a.Count > xfArgument ? ParseInt32(a[xfArgument]) : 0));

        /// <summary>
        /// Frames a payload as a record of a known type.
        /// </summary>
        /// <param name="recordType">The record type.</param>
        /// <param name="payload">The payload.</param>
        /// <returns>The record: the identifier and the payload length, little-endian, then the payload.</returns>
        private static byte[] Frame(BiffRecordType recordType, PayloadBuilder payload) =>
            Frame((ushort)recordType, payload);

        /// <summary>
        /// Frames a payload as a record.
        /// </summary>
        /// <param name="id">The record identifier.</param>
        /// <param name="payload">The payload.</param>
        /// <returns>The record: the identifier and the payload length, little-endian, then the payload.</returns>
        private static byte[] Frame(ushort id, PayloadBuilder payload)
        {
            byte[] body = payload.ToArray();
            var record = new byte[4 + body.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(record, id);
            BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(2), (ushort)body.Length);
            body.CopyTo(record.AsSpan(4));
            return record;
        }

        /// <summary>
        /// Checks a constructor's argument count.
        /// </summary>
        /// <param name="item">The constructor.</param>
        /// <param name="minimum">The fewest arguments it takes.</param>
        /// <param name="maximum">The most arguments it takes.</param>
        /// <exception cref="FormatException">The constructor has too few or too many arguments.</exception>
        private static void Require(GeneratorItem item, int minimum, int maximum)
        {
            if (item.Arguments.Count < minimum || item.Arguments.Count > maximum)
                throw new FormatException($"{item.Name} takes {minimum} to {maximum} arguments, not {item.Arguments.Count}.");
        }

        /// <summary>
        /// Resolves the offset of a <c>SHEET(k, ...)</c> constructor.
        /// </summary>
        /// <param name="text">The substream number <c>k</c>, from zero.</param>
        /// <param name="substreamStarts">
        /// The offset of each substream, or <see langword="null" /> while measuring.
        /// </param>
        /// <returns>The offset of substream <c>k</c>; zero while measuring.</returns>
        /// <exception cref="FormatException">The stream has no substream <c>k</c>.</exception>
        private static long ResolveSubstream(string text, long[]? substreamStarts)
        {
            int k = ParseInt32(text);
            if (substreamStarts is null)
                return 0;

            return k >= 0 && k < substreamStarts.Length ? substreamStarts[k] : throw new FormatException($"SHEET({k}) names no substream.");
        }

        /// <summary>
        /// Parses a record identifier.
        /// </summary>
        /// <param name="text">A <see cref="BiffRecordType" /> name, in any case, or a number.</param>
        /// <returns>The identifier.</returns>
        /// <exception cref="FormatException">
        /// <paramref name="text" /> is neither a record name nor a number.
        /// </exception>
        private static ushort ParseRecordId(string text)
        {
            if (text.Length > 0 && char.IsAsciiDigit(text[0]))
                return (ushort)ParseInt32(text);

            foreach (BiffRecordType recordType in Enum.GetValues<BiffRecordType>())
            {
                if (string.Equals(recordType.ToString(), text, StringComparison.OrdinalIgnoreCase))
                    return (ushort)recordType;
            }

            throw new FormatException($"The record name '{text}' is not a BiffRecordType name.");
        }

        /// <summary>
        /// Parses a BOF substream type.
        /// </summary>
        /// <param name="text">
        /// <c>globals</c>, <c>vb</c>, <c>sheet</c>, <c>chart</c>, <c>macro</c>, <c>workspace</c>, or a number.
        /// </param>
        /// <returns>The substream type value.</returns>
        /// <exception cref="FormatException"><paramref name="text" /> is neither a name nor a number.</exception>
        private static ushort ParseSubstreamType(string text) =>
            text switch
            {
                "globals" => 0x0005,
                "vb" => 0x0006,
                "sheet" => 0x0010,
                "chart" => 0x0020,
                "macro" => 0x0040,
                "workspace" => 0x0100,
                _ => (ushort)ParseInt32(text),
            };

        /// <summary>
        /// Parses a BOUNDSHEET sheet state.
        /// </summary>
        /// <param name="text"><c>visible</c>, <c>hidden</c>, <c>veryhidden</c>, or a number.</param>
        /// <returns>The state byte.</returns>
        /// <exception cref="FormatException"><paramref name="text" /> is neither a name nor a number.</exception>
        private static byte ParseSheetState(string text) =>
            text switch
            {
                "visible" => 0,
                "hidden" => 1,
                "veryhidden" => 2,
                _ => (byte)ParseInt32(text),
            };

        /// <summary>
        /// Parses a BOUNDSHEET sheet type.
        /// </summary>
        /// <param name="text"><c>worksheet</c>, <c>macro</c>, <c>chart</c>, <c>vb</c>, or a number.</param>
        /// <returns>The sheet type byte.</returns>
        /// <exception cref="FormatException"><paramref name="text" /> is neither a name nor a number.</exception>
        private static byte ParseSheetType(string text) =>
            text switch
            {
                "worksheet" => 0,
                "macro" => 1,
                "chart" => 2,
                "vb" => 6,
                _ => (byte)ParseInt32(text),
            };

        /// <summary>
        /// Parses the cached result of a FORMULA constructor.
        /// </summary>
        /// <param name="text">
        /// <c>num:X</c>, <c>bool:B</c>, <c>err:E</c>, <c>str</c>, <c>empty</c>, or <c>raw:</c> followed by eight
        /// payload bytes.
        /// </param>
        /// <returns>The eight bytes of the FormulaValue.</returns>
        /// <exception cref="FormatException"><paramref name="text" /> is not a cached result.</exception>
        private static byte[] ParseFormulaResult(string text)
        {
            if (text.StartsWith("num:", StringComparison.Ordinal))
            {
                var result = new byte[8];
                BinaryPrimitives.WriteDoubleLittleEndian(result, ParseDouble(text[4..]));
                return result;
            }

            if (text.StartsWith("bool:", StringComparison.Ordinal))
                return [1, 0, (byte)ParseInt32(text[5..]), 0, 0, 0, 0xFF, 0xFF];

            if (text.StartsWith("err:", StringComparison.Ordinal))
                return [2, 0, (byte)ParseInt32(text[4..]), 0, 0, 0, 0xFF, 0xFF];

            if (text.StartsWith("raw:", StringComparison.Ordinal))
            {
                byte[] raw = ParsePayload(text[4..]);
                return raw.Length == 8 ? raw : throw new FormatException("A raw: cached result holds eight bytes.");
            }

            return text switch
            {
                "str" => [0, 0, 0, 0, 0, 0, 0xFF, 0xFF],
                "empty" => [3, 0, 0, 0, 0, 0, 0xFF, 0xFF],
                _ => throw new FormatException($"The cached result '{text}' is not one FORMULA takes."),
            };
        }

        /// <summary>
        /// Parses a text argument.
        /// </summary>
        /// <param name="text">The argument: <c>'...'</c>, <c>w'...'</c> or <c>cpN'...'</c>.</param>
        /// <returns>The text argument, its catalog escapes decoded.</returns>
        /// <exception cref="FormatException"><paramref name="text" /> is not a quoted text argument.</exception>
        private static TextArgument ParseText(string text)
        {
            int quote = text.IndexOf('\'', StringComparison.Ordinal);
            if (quote < 0 || quote == text.Length - 1 || text[^1] != '\'')
                throw new FormatException($"The argument '{text}' is not a quoted text argument.");

            string prefix = text[..quote];
            string value = CorpusEscapes.DecodeText(text[(quote + 1)..^1]);
            return prefix switch
            {
                "" => new TextArgument(value, Wide: false, CodePage: 0),
                "w" => new TextArgument(value, Wide: true, CodePage: 0),
                _ when prefix.StartsWith("cp", StringComparison.Ordinal) => new TextArgument(value, Wide: false, CodePage: ParseInt32(prefix[2..])),
                _ => throw new FormatException($"The text prefix '{prefix}' is not w or cpN."),
            };
        }

        /// <summary>
        /// Parses a floating-point argument in invariant form.
        /// </summary>
        /// <param name="text">The argument; <c>NaN</c>, <c>Infinity</c> and <c>-Infinity</c> are accepted.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException"><paramref name="text" /> is not a number.</exception>
        private static double ParseDouble(string text) =>
            double.Parse(text.AsSpan().Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
