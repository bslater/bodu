// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumIntegerConversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Converts between an enumeration value and the integer that carries its underlying value, rejecting an integer
/// outside the range of the enumeration's underlying type rather than narrowing it into that type.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Enum.ToObject(Type, long)" /> keeps only the bits of its argument that the underlying type holds, so it
/// would bind 256 to an enumeration based on <see cref="byte" /> as 0, and -1 to one based on <see cref="ulong" /> as
/// <see cref="ulong.MaxValue" />. Every enumeration converter that accepts an integer reads it through
/// <see cref="ReadInteger{T}(ref FormatReader)" />, which first checks it against the range of the underlying type that
/// <see cref="Enum.GetUnderlyingType(Type)" /> reports. An integer inside that range binds as that value whether or not
/// it names a member.
/// </para>
/// <para>
/// Every enumeration converter that writes an integer writes it through
/// <see cref="WriteInteger{T}(ref FormatWriter, T)" />, which carries a value of an enumeration based on
/// <see cref="ulong" /> above <see cref="long.MaxValue" /> as the format's <see cref="ulong" /> converter carries such
/// a value.
/// </para>
/// </remarks>
internal static class EnumIntegerConversion
{
    /// <summary>
    /// Reads the integer token under the reader as a value of the enumeration <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="reader">The reader positioned on an integer token.</param>
    /// <returns>The value of <typeparamref name="T" /> whose underlying value is the integer.</returns>
    /// <exception cref="FormatSerializationException">
    /// The integer is outside the range of the underlying type of <typeparamref name="T" />.
    /// </exception>
    /// <remarks>
    /// A Bencode integer may exceed <see cref="long.MaxValue" />, up to <see cref="ulong.MaxValue" />, and only an
    /// enumeration based on <see cref="ulong" /> holds such a value. A TOML or YAML integer token is a signed 64-bit
    /// value. The exception carries the format's diagnostics: Bencode's names the token's start offset, and TOML's and
    /// YAML's take their position and path from the converter that encloses the member.
    /// </remarks>
    internal static T ReadInteger<T>(ref FormatReader reader)
        where T : struct, Enum
    {
#if BENCODE
        if (!reader.TryGetInt64(out long value))
        {
            ulong unsignedValue = reader.GetUInt64();
            if (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) != TypeCode.UInt64)
                throw SerializationThrowHelper.EnumValueOutOfRange(ref reader, unsignedValue, typeof(T));

            return (T)Enum.ToObject(typeof(T), unsignedValue);
        }
#else
        long value = reader.GetInt64();
#endif

        if (!IsInUnderlyingRange(typeof(T), value))
        {
#if YAML
            throw new FormatSerializationException(string.Format(
                CultureInfo.CurrentCulture,
                FormatResourceStrings.Op_Invalid_YamlNumberOutOfRange,
                value.ToString(CultureInfo.InvariantCulture),
                typeof(T)));
#else
            throw SerializationThrowHelper.EnumValueOutOfRange(ref reader, value, typeof(T));
#endif
        }

        return (T)Enum.ToObject(typeof(T), value);
    }

    /// <summary>
    /// Writes the underlying value of an enumeration value as an integer.
    /// </summary>
    /// <typeparam name="T">The enumeration type.</typeparam>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value to write.</param>
    /// <exception cref="FormatSerializationException">
    /// The format is TOML, and <paramref name="value" />, of an enumeration based on <see cref="ulong" />, is above
    /// <see cref="long.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// Only an enumeration based on <see cref="ulong" /> has values above <see cref="long.MaxValue" />, and each format
    /// writes one as it writes such a <see cref="ulong" /> member. Bencode writes it through the unsigned integer
    /// surface, since a Bencode integer reaches <see cref="ulong.MaxValue" />. YAML, whose writer has only a signed
    /// 64-bit integer surface, writes its invariant text, which reads back as a plain integer. TOML, whose integers are
    /// signed 64-bit, rejects it, with the <see cref="OverflowException" /> as the inner exception.
    /// </remarks>
    internal static void WriteInteger<T>(ref FormatWriter writer, T value)
        where T : struct, Enum
    {
        if (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) != TypeCode.UInt64)
        {
            writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
            return;
        }

        ulong unsignedValue = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
#if BENCODE
        writer.WriteInteger(unsignedValue);
#elif YAML
        if (unsignedValue > long.MaxValue)
        {
            writer.WriteString(unsignedValue.ToString(CultureInfo.InvariantCulture));
            return;
        }

        writer.WriteInteger((long)unsignedValue);
#else
        try
        {
            writer.WriteInteger(long.CreateChecked(unsignedValue));
        }
        catch (OverflowException ex)
        {
            string message = string.Format(
                CultureInfo.CurrentCulture,
                FormatResourceStrings.Op_Invalid_IntegerOverflow,
                unsignedValue,
                typeof(long));
            throw new FormatSerializationException(message, ex);
        }
#endif
    }

    /// <summary>
    /// Determines whether a signed 64-bit integer lies within the range of an enumeration's underlying type.
    /// </summary>
    /// <param name="enumType">The enumeration type.</param>
    /// <param name="value">The integer.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> lies within the range of the underlying type of
    /// <paramref name="enumType" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// An enumeration declared in C# has one of the eight integral types as its underlying type. Any other underlying
    /// type, which only another language can declare, holds no integer here, as System.Text.Json treats it.
    /// </remarks>
    private static bool IsInUnderlyingRange(Type enumType, long value) =>
        Type.GetTypeCode(Enum.GetUnderlyingType(enumType)) switch
        {
            TypeCode.SByte => value is >= sbyte.MinValue and <= sbyte.MaxValue,
            TypeCode.Byte => value is >= byte.MinValue and <= byte.MaxValue,
            TypeCode.Int16 => value is >= short.MinValue and <= short.MaxValue,
            TypeCode.UInt16 => value is >= ushort.MinValue and <= ushort.MaxValue,
            TypeCode.Int32 => value is >= int.MinValue and <= int.MaxValue,
            TypeCode.UInt32 => value is >= uint.MinValue and <= uint.MaxValue,
            TypeCode.Int64 => true,
            TypeCode.UInt64 => value >= 0,
            _ => false,
        };
}
