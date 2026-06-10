// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

namespace Bodu.Text.Serialization.Converters;

/// <summary>
/// Converts any fixed-width integer type to and from a 64-bit integer token, using checked conversions so a value
/// outside the target type's range — or outside the signed 64-bit range a format can store — surfaces as a
/// serialization error rather than silently wrapping.
/// </summary>
/// <typeparam name="T">The integer type, constrained to <see cref="IBinaryInteger{TSelf}" />.</typeparam>
internal sealed class IntegerConverter<T>
    : FormatConverter<T>
    where T : IBinaryInteger<T>
{
    /// <inheritdoc />
    public override T Read(ISerializationReader reader, Type typeToConvert, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);

        var value = reader.GetInt64();
        try
        {
            return T.CreateChecked(value);
        }
        catch (OverflowException ex)
        {
            throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(T)),
                ex);
        }
    }

    /// <inheritdoc />
    public override void Write(ISerializationWriter writer, T value, FormatSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        try
        {
            writer.WriteInt64(long.CreateChecked(value));
        }
        catch (OverflowException ex)
        {
            throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, SerializationResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(long)),
                ex);
        }
    }
}
