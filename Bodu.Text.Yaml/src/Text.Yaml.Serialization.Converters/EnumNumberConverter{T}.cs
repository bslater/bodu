// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumNumberConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts an enumeration value to and from a YAML integer scalar carrying its underlying numeric value. A null scalar
/// reads as the type default.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// The numeric value is read from an integer token, a signed 64-bit value, through
/// <see cref="EnumIntegerConversion.ReadInteger{T}(ref Utf8YamlReader)" />, so an integer outside the range of the
/// enumeration's underlying type throws <see cref="YamlSerializationException" /> rather than being narrowed into that
/// type. YAML resolves a plain integer above <see cref="long.MaxValue" /> as a string, so for an enumeration based on
/// <see cref="ulong" /> a plain, untagged scalar of decimal digits also reads, as a <see cref="ulong" /> member reads
/// it; a quoted or tagged string does not. The value is written through
/// <see cref="EnumIntegerConversion.WriteInteger{T}(ref Utf8YamlWriter, T)" />, which writes a value above
/// <see cref="long.MaxValue" />, beyond the writer's signed 64-bit integer surface, as that plain text, as a
/// <see cref="ulong" /> member is written.
/// </remarks>
internal sealed class EnumNumberConverter<T>
    : YamlConverter<T>
    where T : struct, Enum
{
    /// <inheritdoc />
    public override T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return default;

        if (reader.TokenType == YamlTokenType.String && TryReadUnsignedText(ref reader, out T unsignedValue))
            return unsignedValue;

        if (reader.TokenType != YamlTokenType.Integer)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType));
        }

        return EnumIntegerConversion.ReadInteger<T>(ref reader);
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options) =>
        EnumIntegerConversion.WriteInteger(ref writer, value);

    /// <summary>
    /// Attempts to read a plain, untagged string scalar of decimal digits, the form YAML resolves an integer above
    /// <see cref="long.MaxValue" /> to, as a value of an enumeration based on <see cref="ulong" />.
    /// </summary>
    /// <param name="reader">The reader positioned on a string scalar.</param>
    /// <param name="value">When this method returns <see langword="true" />, the value the digits name.</param>
    /// <returns>
    /// <see langword="true" /> when <typeparamref name="T" /> is based on <see cref="ulong" /> and the scalar is a
    /// plain, untagged run of decimal digits with an optional plus sign; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="YamlSerializationException">
    /// The digits name a value above <see cref="ulong.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// The digits read as a <see cref="ulong" /> member reads them, so a value above <see cref="ulong.MaxValue" /> lies
    /// outside the range of the underlying type and is rejected rather than narrowed. A quoted or tagged scalar is a
    /// string by the document's own choice and never reads as a number.
    /// </remarks>
    private static bool TryReadUnsignedText(ref Utf8YamlReader reader, out T value)
    {
        value = default;
        if (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) != TypeCode.UInt64 || !reader.IsPlainUntaggedScalar)
            return false;

        string text = reader.GetString();
        ReadOnlySpan<char> digits = text.AsSpan(text.StartsWith('+') ? 1 : 0);
        if (digits.IsEmpty || digits.ContainsAnyExceptInRange('0', '9'))
            return false;

        try
        {
            value = (T)Enum.ToObject(typeof(T), ulong.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture));
            return true;
        }
        catch (OverflowException ex)
        {
            throw new YamlSerializationException(
                string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlNumberOutOfRange, text, typeof(T)),
                ex);
        }
    }
}
