// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResource.PayloadReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Calendar;

public static partial class NotableDateBinaryResource
{
    /// <summary>
    /// Parses the pack payload — string table then body — with every read bounds-checked, so truncated or corrupted
    /// input surfaces as <see cref="NotableDateBinaryFormatException" /> rather than an out-of-range access.
    /// </summary>
    private sealed class PayloadReader
    {
        /// <summary>The payload bytes.</summary>
        private readonly byte[] _payload;

        /// <summary>The decoded string table; body references are 1-based, 0 denoting <see langword="null" />.</summary>
        private readonly string[] _strings;

        /// <summary>The current read position within <see cref="_payload" />.</summary>
        private int _position;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayloadReader" /> class, decoding the string table.
        /// </summary>
        /// <param name="payload">The verified payload bytes.</param>
        public PayloadReader(byte[] payload)
        {
            _payload = payload;

            uint count = ReadVarUInt();
            _strings = new string[count];
            for (var i = 0; i < count; i++)
            {
                var length = (int)ReadVarUInt();
                if (length < 0 || _position + length > _payload.Length)
                    throw Truncated();

                _strings[i] = Encoding.UTF8.GetString(_payload, _position, length);
                _position += length;
            }
        }

        /// <summary>
        /// Gets a value indicating whether every payload byte has been consumed.
        /// </summary>
        public bool AtEnd =>
            _position == _payload.Length;

        /// <summary>
        /// Reads one raw byte.
        /// </summary>
        /// <returns>The byte read.</returns>
        public byte ReadByte()
        {
            if (_position >= _payload.Length)
                throw Truncated();

            return _payload[_position++];
        }

        /// <summary>
        /// Reads a Boolean written as one byte.
        /// </summary>
        /// <returns>The value read.</returns>
        public bool ReadBool() =>
            ReadByte() switch
            {
                0 => false,
                1 => true,
                var other => throw InvalidValue($"boolean byte 0x{other:X2}"),
            };

        /// <summary>
        /// Reads a 7-bit variable-length unsigned integer.
        /// </summary>
        /// <returns>The value read.</returns>
        public uint ReadVarUInt()
        {
            uint value = 0;
            var shift = 0;

            while (true)
            {
                if (shift > 28)
                    throw InvalidValue("variable-length integer longer than five bytes");

                byte b = ReadByte();
                value |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                    return value;

                shift += 7;
            }
        }

        /// <summary>
        /// Reads a ZigZag-encoded signed integer.
        /// </summary>
        /// <returns>The value read.</returns>
        public int ReadInt32()
        {
            uint encoded = ReadVarUInt();
            return (int)(encoded >> 1) ^ -(int)(encoded & 1);
        }

        /// <summary>
        /// Reads an optional signed integer written as a presence byte plus value.
        /// </summary>
        /// <returns>The value read, or <see langword="null" />.</returns>
        public int? ReadNullableInt32() =>
            ReadBool() ? ReadInt32() : null;

        /// <summary>
        /// Reads a required string reference.
        /// </summary>
        /// <returns>The referenced string.</returns>
        public string ReadString() =>
            ReadNullableString() ?? throw InvalidValue("null string where a value is required");

        /// <summary>
        /// Reads an optional string reference, index zero denoting <see langword="null" />.
        /// </summary>
        /// <returns>The referenced string, or <see langword="null" />.</returns>
        public string? ReadNullableString()
        {
            uint index = ReadVarUInt();
            if (index == 0)
                return null;
            if (index > _strings.Length)
                throw InvalidValue($"string index {index} outside the table of {_strings.Length}");

            return _strings[index - 1];
        }

        /// <summary>
        /// Reads a count-prefixed list of required strings.
        /// </summary>
        /// <returns>The strings read.</returns>
        public List<string> ReadStringList()
        {
            var count = (int)ReadVarUInt();
            EnsureRemaining(count);
            List<string> values = new(count);
            for (var i = 0; i < count; i++)
                values.Add(ReadString());

            return values;
        }

        /// <summary>
        /// Reads a count-prefixed list of signed integers.
        /// </summary>
        /// <returns>The values read.</returns>
        public List<int> ReadInt32List()
        {
            var count = (int)ReadVarUInt();
            EnsureRemaining(count);
            List<int> values = new(count);
            for (var i = 0; i < count; i++)
                values.Add(ReadInt32());

            return values;
        }

        /// <summary>
        /// Reads a count-prefixed list of enum values, one byte each.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>The values read.</returns>
        public List<TEnum> ReadEnumList<TEnum>()
            where TEnum : struct, Enum
        {
            var count = (int)ReadVarUInt();
            EnsureRemaining(count);
            List<TEnum> values = new(count);
            for (var i = 0; i < count; i++)
                values.Add(ReadEnum<TEnum>());

            return values;
        }

        /// <summary>
        /// Reads an enum value written as one byte, rejecting values the enum does not define.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>The value read.</returns>
        public TEnum ReadEnum<TEnum>()
            where TEnum : struct, Enum
        {
            byte raw = ReadByte();
            var value = (TEnum)Enum.ToObject(typeof(TEnum), raw);
            if (!Enum.IsDefined(value))
                throw InvalidValue($"{typeof(TEnum).Name} value {raw}");

            return value;
        }

        /// <summary>
        /// Reads an optional enum value written as a presence byte plus value.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>The value read, or <see langword="null" />.</returns>
        public TEnum? ReadNullableEnum<TEnum>()
            where TEnum : struct, Enum =>
            ReadBool() ? ReadEnum<TEnum>() : null;

        /// <summary>
        /// Reads a date written as its day number.
        /// </summary>
        /// <returns>The date read.</returns>
        public DateOnly ReadDate()
        {
            var dayNumber = (int)ReadVarUInt();
            if (dayNumber < DateOnly.MinValue.DayNumber || dayNumber > DateOnly.MaxValue.DayNumber)
                throw InvalidValue($"day number {dayNumber}");

            return DateOnly.FromDayNumber(dayNumber);
        }

        /// <summary>
        /// Reads an optional date written as a presence byte plus day number.
        /// </summary>
        /// <returns>The date read, or <see langword="null" />.</returns>
        public DateOnly? ReadNullableDate() =>
            ReadBool() ? ReadDate() : null;

        /// <summary>
        /// Creates the truncation exception.
        /// </summary>
        /// <returns>The exception to throw.</returns>
        public static NotableDateBinaryFormatException Truncated() =>
            new(CalendarResourceStrings.Format_Invalid_BinaryTruncated);

        /// <summary>
        /// Creates the invalid-value exception naming the offending value.
        /// </summary>
        /// <param name="description">The description of the invalid value.</param>
        /// <returns>The exception to throw.</returns>
        public static NotableDateBinaryFormatException InvalidValue(string description) =>
            new(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Format_Invalid_BinaryValue, description));

        /// <summary>
        /// Rejects a declared element count that cannot possibly fit the remaining payload, before any allocation.
        /// </summary>
        /// <param name="count">The declared element count; every element occupies at least one byte.</param>
        private void EnsureRemaining(int count)
        {
            if (count < 0 || count > _payload.Length - _position)
                throw Truncated();
        }
    }
}
