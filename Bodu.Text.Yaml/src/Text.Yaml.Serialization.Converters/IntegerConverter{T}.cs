// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a fixed-width integer type to and from a YAML integer scalar, rejecting a non-integral or out-of-range
/// source rather than silently truncating it. A null scalar reads as the type default.
/// </summary>
/// <typeparam name="T">The integral type.</typeparam>
/// <remarks>
/// A float source is accepted only when it carries an integral value, or unconditionally (with truncation) under
/// <see cref="YamlNumberHandling.AllowFloatToInteger" />. A value above <see cref="long.MaxValue" /> — an unsigned
/// 64-bit, native-sized unsigned, or 128-bit value — writes as its invariant text, and a 128-bit value below
/// <see cref="long.MinValue" /> does the same: the scalar re-reads as a string and converts back exactly, because the
/// writer's integer surface is signed 64-bit.
/// </remarks>
internal sealed class IntegerConverter<T>
    : YamlConverter<T>
    where T : struct
{
    /// <inheritdoc />
    public override T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return default;

        if (reader.TokenType == YamlTokenType.Float)
        {
            double d = reader.GetDouble();
            if (options.NumberHandling == YamlNumberHandling.AllowFloatToInteger)
                return FromDouble(Math.Truncate(d));

            if (!double.IsFinite(d) || Math.Floor(d) != d)
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlFloatNotIntegral, d.ToString(CultureInfo.InvariantCulture)));
            }
        }

        string text = ScalarCoercion.ReaderScalarText(ref reader);
        try
        {
            return FromText(text);
        }
        catch (OverflowException ex)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlNumberOutOfRange, text, typeof(T)), ex);
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    {
        // The widths whose range exceeds the writer's signed 64-bit integer surface fall back to invariant text,
        // and the native-sized types are not IConvertible, so both route around Convert.ToInt64.
        switch (value)
        {
            case ulong unsigned:
                WriteInt64OrText(writer, unsigned <= long.MaxValue, (long)unsigned, unsigned.ToString(CultureInfo.InvariantCulture));
                return;

            case nuint nativeUnsigned:
                WriteInt64OrText(writer, nativeUnsigned <= long.MaxValue, (long)nativeUnsigned, nativeUnsigned.ToString(CultureInfo.InvariantCulture));
                return;

            case nint native:
                writer.WriteInteger(native);
                return;

            case Int128 wide:
                WriteInt64OrText(writer, wide >= long.MinValue && wide <= long.MaxValue, (long)wide, wide.ToString(CultureInfo.InvariantCulture));
                return;

            case UInt128 wideUnsigned:
                WriteInt64OrText(writer, wideUnsigned <= (ulong)long.MaxValue, (long)wideUnsigned, wideUnsigned.ToString(CultureInfo.InvariantCulture));
                return;

            default:
                writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                return;
        }
    }

    /// <summary>
    /// Writes the value as a YAML integer when it fits the writer's signed 64-bit surface, or as its invariant text
    /// otherwise.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="fits">Whether the value fits the signed 64-bit range.</param>
    /// <param name="integer">The value narrowed to 64 bits, meaningful only when <paramref name="fits" /> is <see langword="true" />.</param>
    /// <param name="text">The value's invariant text, written when the value does not fit.</param>
    private static void WriteInt64OrText(Utf8YamlWriter writer, bool fits, long integer, string text)
    {
        if (fits)
            writer.WriteInteger(integer);
        else
            writer.WriteString(text);
    }

    /// <summary>
    /// Converts the scalar's invariant text to <typeparamref name="T" />, routing the widths
    /// <see cref="Convert.ChangeType(object, Type)" /> cannot produce through their own parsers.
    /// </summary>
    /// <param name="text">The scalar text.</param>
    /// <returns>The converted value.</returns>
    private static T FromText(string text)
    {
        if (typeof(T) == typeof(Int128))
            return (T)(object)Int128.Parse(text, CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(UInt128))
            return (T)(object)UInt128.Parse(text, CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(nint))
            return (T)(object)nint.Parse(text, CultureInfo.InvariantCulture);
        if (typeof(T) == typeof(nuint))
            return (T)(object)nuint.Parse(text, CultureInfo.InvariantCulture);

        return (T)Convert.ChangeType(text, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a truncated float source to <typeparamref name="T" /> under
    /// <see cref="YamlNumberHandling.AllowFloatToInteger" />, using checked generic-math creation for the widths
    /// <see cref="Convert.ChangeType(object, Type)" /> cannot produce.
    /// </summary>
    /// <param name="truncated">The truncated float value.</param>
    /// <returns>The converted value.</returns>
    private static T FromDouble(double truncated)
    {
        if (typeof(T) == typeof(Int128))
            return (T)(object)Int128.CreateChecked(truncated);
        if (typeof(T) == typeof(UInt128))
            return (T)(object)UInt128.CreateChecked(truncated);
        if (typeof(T) == typeof(nint))
            return (T)(object)nint.CreateChecked(truncated);
        if (typeof(T) == typeof(nuint))
            return (T)(object)nuint.CreateChecked(truncated);

        return (T)Convert.ChangeType(truncated, typeof(T), CultureInfo.InvariantCulture);
    }
}
