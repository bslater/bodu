// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntegerConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts any fixed-width integer type to and from a TOML integer, using checked conversions so a value outside the
/// target type's range — or outside the signed 64-bit range TOML can store — surfaces as a serialization error rather
/// than silently wrapping.
/// </summary>
/// <typeparam name="T">The integer type, constrained to <see cref="IBinaryInteger{TSelf}" />.</typeparam>
internal sealed class IntegerConverter<T>
    : TomlConverter<T>
    where T : IBinaryInteger<T>
{
    /// <inheritdoc />
    public override T Read(ref Utf8TomlReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.Integer)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedInteger, reader.TokenType));
        }

        var value = reader.GetInt64();
        try
        {
            return T.CreateChecked(value);
        }
        catch (OverflowException ex)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(T)),
                ex);
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, T value, TomlSerializerOptions options)
    {
        try
        {
            writer.WriteInteger(long.CreateChecked(value));
        }
        catch (OverflowException ex)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_IntegerOverflow, value, typeof(long)),
                ex);
        }
    }
}
