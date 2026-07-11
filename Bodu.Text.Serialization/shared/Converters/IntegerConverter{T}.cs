// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

/// <summary>
/// Converts any fixed-width integer type to and from the format's integer token, using checked conversions so a value
/// outside the target type's range — or outside the signed 64-bit surface this converter reads and writes through —
/// surfaces as a serialization error rather than silently wrapping.
/// </summary>
/// <typeparam name="T">The integer type, constrained to <see cref="IBinaryInteger{TSelf}" />.</typeparam>
/// <remarks>
/// The format's integer-converter factory decides which integer types route here; a format with a wider native surface
/// may route some widths to dedicated converters instead.
/// </remarks>
internal sealed class IntegerConverter<T>
    : SharedConverter<T>
    where T : IBinaryInteger<T>
{
    /// <inheritdoc />
    public override T Read(ref FormatReader reader, Type typeToConvert, FormatOptions options)
    {
        if (reader.TokenType != FormatToken.Integer)
            throw SerializationThrowHelper.ExpectedInteger(ref reader);

        long value = reader.GetInt64();
        try
        {
            return T.CreateChecked(value);
        }
        catch (OverflowException ex)
        {
            throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(T)),
                ex);
        }
    }

    /// <inheritdoc />
    public override void Write(FormatWriter writer, T value, FormatOptions options)
    {
        try
        {
            writer.WriteInteger(long.CreateChecked(value));
        }
        catch (OverflowException ex)
        {
            throw new FormatSerializationException(
                string.Format(CultureInfo.CurrentCulture, FormatResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(long)),
                ex);
        }
    }
}
