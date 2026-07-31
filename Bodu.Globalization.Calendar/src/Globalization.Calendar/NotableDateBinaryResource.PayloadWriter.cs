// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResource.PayloadWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Globalization.Calendar;

public static partial class NotableDateBinaryResource
{
    /// <summary>
    /// Accumulates the pack body while interning every written string into a deduplicating table, so the table can be
    /// emitted ahead of the body it indexes.
    /// </summary>
    private sealed class PayloadWriter
    {
        /// <summary>The body bytes, written before the string table is final.</summary>
        private readonly MemoryStream _body = new();

        /// <summary>The interned strings in first-use order; index 0 is reserved for <see langword="null" />.</summary>
        private readonly List<string> _strings = new();

        /// <summary>The intern lookup from string to its 1-based table index.</summary>
        private readonly Dictionary<string, int> _indexOf = new(StringComparer.Ordinal);

        /// <summary>
        /// Writes a single raw byte.
        /// </summary>
        /// <param name="value">The byte to write.</param>
        public void WriteByte(byte value) =>
            _body.WriteByte(value);

        /// <summary>
        /// Writes a Boolean as one byte.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteBool(bool value) =>
            _body.WriteByte(value ? (byte)1 : (byte)0);

        /// <summary>
        /// Writes an unsigned value as a 7-bit variable-length integer.
        /// </summary>
        /// <param name="value">The non-negative value to write.</param>
        public void WriteVarUInt(uint value)
        {
            while (value >= 0x80)
            {
                _body.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }

            _body.WriteByte((byte)value);
        }

        /// <summary>
        /// Writes a signed value using ZigZag encoding over the variable-length integer form.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void WriteInt32(int value) =>
            WriteVarUInt((uint)((value << 1) ^ (value >> 31)));

        /// <summary>
        /// Writes an optional signed value as a presence byte followed by the value when present.
        /// </summary>
        /// <param name="value">The value to write, or <see langword="null" />.</param>
        public void WriteNullableInt32(int? value)
        {
            WriteBool(value.HasValue);
            if (value.HasValue)
                WriteInt32(value.Value);
        }

        /// <summary>
        /// Writes a required string as its interned table index.
        /// </summary>
        /// <param name="value">The string to intern and reference.</param>
        public void WriteString(string value) =>
            WriteVarUInt((uint)Intern(value));

        /// <summary>
        /// Writes an optional string as its interned table index, with index zero denoting <see langword="null" />.
        /// </summary>
        /// <param name="value">The string to intern and reference, or <see langword="null" />.</param>
        public void WriteNullableString(string? value) =>
            WriteVarUInt(value is null ? 0u : (uint)Intern(value));

        /// <summary>
        /// Writes a count-prefixed list of required strings.
        /// </summary>
        /// <param name="values">The strings to write.</param>
        public void WriteStringList(IReadOnlyList<string> values)
        {
            WriteVarUInt((uint)values.Count);
            foreach (string value in values)
                WriteString(value);
        }

        /// <summary>
        /// Writes a count-prefixed list of signed values.
        /// </summary>
        /// <param name="values">The values to write.</param>
        public void WriteInt32List(IReadOnlyList<int> values)
        {
            WriteVarUInt((uint)values.Count);
            foreach (int value in values)
                WriteInt32(value);
        }

        /// <summary>
        /// Writes a count-prefixed list of enum values, one byte each.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="values">The values to write.</param>
        public void WriteEnumList<TEnum>(IReadOnlyList<TEnum> values)
            where TEnum : struct, Enum
        {
            WriteVarUInt((uint)values.Count);
            foreach (TEnum value in values)
                WriteEnum(value);
        }

        /// <summary>
        /// Writes an enum value as one byte.
        /// </summary>
        /// <typeparam name="TEnum">The enum type; every model enum fits a byte.</typeparam>
        /// <param name="value">The value to write.</param>
        public void WriteEnum<TEnum>(TEnum value)
            where TEnum : struct, Enum =>
            _body.WriteByte((byte)Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture));

        /// <summary>
        /// Writes an optional enum value as a presence byte followed by the value when present.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <param name="value">The value to write, or <see langword="null" />.</param>
        public void WriteNullableEnum<TEnum>(TEnum? value)
            where TEnum : struct, Enum
        {
            WriteBool(value.HasValue);
            if (value.HasValue)
                WriteEnum(value.Value);
        }

        /// <summary>
        /// Writes a date as its day number.
        /// </summary>
        /// <param name="value">The date to write.</param>
        public void WriteDate(DateOnly value) =>
            WriteVarUInt((uint)value.DayNumber);

        /// <summary>
        /// Writes an optional date as a presence byte followed by the day number when present.
        /// </summary>
        /// <param name="value">The date to write, or <see langword="null" />.</param>
        public void WriteNullableDate(DateOnly? value)
        {
            WriteBool(value.HasValue);
            if (value.HasValue)
                WriteDate(value.Value);
        }

        /// <summary>
        /// Assembles the final payload: the string table followed by the body.
        /// </summary>
        /// <returns>The payload bytes the header's digest covers.</returns>
        public byte[] ToPayload()
        {
            using MemoryStream payload = new();

            // String table: count, then each entry as a length-prefixed UTF-8 run. Body string references are
            // 1-based; index 0 is the null sentinel and has no table entry.
            WriteVarUIntTo(payload, (uint)_strings.Count);
            foreach (string value in _strings)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                WriteVarUIntTo(payload, (uint)bytes.Length);
                payload.Write(bytes);
            }

            _body.Position = 0;
            _body.CopyTo(payload);

            return payload.ToArray();
        }

        /// <summary>
        /// Writes a 7-bit variable-length integer to an arbitrary stream.
        /// </summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="value">The value to write.</param>
        private static void WriteVarUIntTo(Stream stream, uint value)
        {
            while (value >= 0x80)
            {
                stream.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }

            stream.WriteByte((byte)value);
        }

        /// <summary>
        /// Interns a string, returning its stable 1-based table index.
        /// </summary>
        /// <param name="value">The string to intern.</param>
        /// <returns>The 1-based index of the string in the table.</returns>
        private int Intern(string value)
        {
            if (_indexOf.TryGetValue(value, out int index))
                return index;

            _strings.Add(value);
            index = _strings.Count;
            _indexOf[value] = index;

            return index;
        }
    }
}
