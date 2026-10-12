// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.PayloadBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Text;

using Bodu.IO.Biff;

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Builds a record payload from little-endian values, byte runs and strings.
    /// </summary>
    private sealed class PayloadBuilder
    {
        /// <summary>The bytes written so far.</summary>
        private readonly ArrayBufferWriter<byte> _bytes = new();

        /// <summary>
        /// Gets the number of bytes written so far.
        /// </summary>
        /// <value>The payload length.</value>
        public int Length =>
            _bytes.WrittenCount;

        /// <summary>
        /// Appends one byte.
        /// </summary>
        /// <param name="value">The byte.</param>
        /// <returns>This builder.</returns>
        public PayloadBuilder U8(byte value)
        {
            _bytes.GetSpan(1)[0] = value;
            _bytes.Advance(1);
            return this;
        }

        /// <summary>
        /// Appends a 16-bit unsigned integer, little-endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>This builder.</returns>
        public PayloadBuilder U16(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_bytes.GetSpan(2), value);
            _bytes.Advance(2);
            return this;
        }

        /// <summary>
        /// Appends a 32-bit unsigned integer, little-endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>This builder.</returns>
        public PayloadBuilder U32(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_bytes.GetSpan(4), value);
            _bytes.Advance(4);
            return this;
        }

        /// <summary>
        /// Appends an IEEE 754 double, little-endian.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>This builder.</returns>
        public PayloadBuilder F64(double value)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(_bytes.GetSpan(8), value);
            _bytes.Advance(8);
            return this;
        }

        /// <summary>
        /// Appends a run of zero bytes.
        /// </summary>
        /// <param name="count">The number of bytes.</param>
        /// <returns>This builder.</returns>
        /// <exception cref="FormatException"><paramref name="count" /> is negative.</exception>
        public PayloadBuilder Zeros(int count)
        {
            if (count < 0)
                throw new FormatException($"A run of {count} zero bytes is not valid.");

            _bytes.GetSpan(count)[..count].Clear();
            _bytes.Advance(count);
            return this;
        }

        /// <summary>
        /// Appends bytes as they are.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>This builder.</returns>
        public PayloadBuilder Bytes(ReadOnlySpan<byte> bytes)
        {
            _bytes.Write(bytes);
            return this;
        }

        /// <summary>
        /// Appends a BIFF8 XLUnicodeString or ShortXLUnicodeString.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="wideLength">
        /// <see langword="true" /> for a 16-bit character count (XLUnicodeString); <see langword="false" /> for an
        /// 8-bit one (ShortXLUnicodeString).
        /// </param>
        /// <returns>This builder.</returns>
        /// <remarks>
        /// The character count is followed by the flags byte and the characters: one byte each when every character is
        /// at most U+00FF and the text is not <see cref="TextArgument.Wide" />, and UTF-16LE with flag 0x01 otherwise.
        /// </remarks>
        public PayloadBuilder Unicode(TextArgument text, bool wideLength)
        {
            bool wide = text.Wide || text.Text.Any(c => c > 0xFF);
            if (wideLength)
                U16((ushort)text.Text.Length);
            else
                U8((byte)text.Text.Length);

            U8((byte)(wide ? 0x01 : 0x00));
            if (wide)
                return Bytes(Encoding.Unicode.GetBytes(text.Text));

            foreach (char c in text.Text)
                U8((byte)c);

            return this;
        }

        /// <summary>
        /// Appends the string layout of the generator's version.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="context">The generator context, which gives the version and the default code page.</param>
        /// <param name="wideLength">
        /// <see langword="true" /> for a 16-bit length; <see langword="false" /> for an 8-bit one.
        /// </param>
        /// <returns>This builder.</returns>
        /// <remarks>
        /// A BIFF8 text without a code page is written by <see cref="Unicode" />; any other text as its length in bytes
        /// followed by its bytes in its own code page, or else the context's.
        /// </remarks>
        /// <exception cref="FormatException">The code page has no encoding.</exception>
        public PayloadBuilder Text(TextArgument text, GeneratorContext context, bool wideLength)
        {
            if (context.Version == BiffVersion.Biff8 && text.CodePage == 0)
                return Unicode(text, wideLength);

            byte[] bytes = RecordBuilder.EncodeCodePage(text.CodePage != 0 ? text.CodePage : context.CodePage, text.Text);
            if (wideLength)
                U16((ushort)bytes.Length);
            else
                U8((byte)bytes.Length);

            return Bytes(bytes);
        }

        /// <summary>
        /// Copies the bytes written so far.
        /// </summary>
        /// <returns>The payload.</returns>
        public byte[] ToArray() =>
            _bytes.WrittenSpan.ToArray();
    }
}
