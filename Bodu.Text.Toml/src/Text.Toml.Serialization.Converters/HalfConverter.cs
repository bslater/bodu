// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HalfConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="Half" /> value to and from a TOML floating-point value, widening to and narrowing from
/// <see cref="double" /> at the format boundary.
/// </summary>
/// <remarks>
/// Widening to <see cref="double" /> on write is exact for every finite <see cref="Half" /> and maps the not-a-number
/// and infinity values to TOML's <c>nan</c>, <c>inf</c>, and <c>-inf</c> forms. Narrowing on read rounds to the nearest
/// <see cref="Half" />, as the <see cref="float" /> converter does, and a finite TOML float outside the
/// <see cref="Half" /> range throws <see cref="TomlSerializationException" /> rather than reading as an infinity.
/// </remarks>
internal sealed class HalfConverter
    : TomlConverter<Half>
{
    /// <inheritdoc />
    public override Half Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        if (reader.TokenType != TomlTokenType.Float)
        {
            throw new TomlSerializationException(
                string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedFloat, reader.TokenType));
        }

        return FloatNarrowing.Narrow<Half>(reader.GetDouble());
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, Half value, TomlSerializerOptions options) =>
        writer.WriteFloat((double)value);
}
