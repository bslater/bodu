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
/// outside the target type's range - or outside the signed 64-bit surface this converter reads and writes through -
/// surfaces as a serialization error rather than silently wrapping.
/// </summary>
/// <typeparam name="T">The integer type, constrained to <see cref="IBinaryInteger{TSelf}" />.</typeparam>
/// <remarks>
/// The format's integer-converter factory decides which integer types route here; a format with a wider native surface
/// may route some widths to dedicated converters instead. An integer outside the target type's range on read throws
/// through <see cref="SerializationThrowHelper.IntegerOutOfRange" />, with the checked conversion's
/// <see cref="OverflowException" /> as the inner exception. So does a Bencode integer above
/// <see cref="long.MaxValue" />, which the reader accepts as well-formed but which lies beyond the signed 64-bit
/// surface, with no inner exception, since no conversion is attempted; when the target type could hold such an integer,
/// as <see cref="Int128" /> can, the message names the signed 64-bit range in which the member is read rather than the
/// type's range. Bencode's exception carries the integer token's start offset, and TOML's takes its position and path
/// from the converter that encloses the member.
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

#if BENCODE
        // A Bencode integer reaches ulong.MaxValue, so one beyond the signed 64-bit surface this converter reads
        // through is well-formed: it is a value outside the target's range rather than malformed bytes, or, for a
        // target that could hold it, such as Int128, a value outside only the surface, and the message says which.
        if (!reader.TryGetInt64(out long value))
        {
            ulong unsignedValue = reader.GetUInt64();
            if (CanHold(unsignedValue))
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    FormatResourceStrings.Op_Invalid_IntegerOutsideSignedSurface,
                    unsignedValue,
                    typeof(T));
                throw new FormatSerializationException(message, reader.TokenStartIndex);
            }

            throw SerializationThrowHelper.IntegerOutOfRange(ref reader, unsignedValue, typeof(T));
        }
#else
        long value = reader.GetInt64();
#endif

        try
        {
            return T.CreateChecked(value);
        }
        catch (OverflowException ex)
        {
            throw SerializationThrowHelper.IntegerOutOfRange(ref reader, value, typeof(T), ex);
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

#if BENCODE
    /// <summary>
    /// Determines whether <typeparamref name="T" /> holds an unsigned 64-bit integer exactly.
    /// </summary>
    /// <param name="value">The integer.</param>
    /// <returns>
    /// <see langword="true" /> when <typeparamref name="T" /> holds <paramref name="value" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// A saturating conversion clamps a value <typeparamref name="T" /> cannot hold to its nearest bound, so the value
    /// survives the round trip through <typeparamref name="T" /> unchanged only when <typeparamref name="T" /> holds
    /// it.
    /// </remarks>
    private static bool CanHold(ulong value) =>
        ulong.CreateSaturating(T.CreateSaturating(value)) == value;
#endif
}
