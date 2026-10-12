// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumIntegerConversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if YAML
using System.Globalization;
#endif

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Converts an integer token to an enumeration value, rejecting an integer outside the range of the enumeration's
/// underlying type rather than narrowing it into that type.
/// </summary>
/// <remarks>
/// <see cref="Enum.ToObject(Type, long)" /> keeps only the bits of its argument that the underlying type holds, so it
/// would bind 256 to an enumeration based on <see cref="byte" /> as 0, and -1 to one based on <see cref="ulong" /> as
/// <see cref="ulong.MaxValue" />. Every enumeration converter that accepts an integer reads it through
/// <see cref="ReadInteger{T}(ref FormatReader)" />, which first checks it against the range of the underlying type that
/// <see cref="Enum.GetUnderlyingType(Type)" /> reports. An integer inside that range binds as that value whether or not
/// it names a member.
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
