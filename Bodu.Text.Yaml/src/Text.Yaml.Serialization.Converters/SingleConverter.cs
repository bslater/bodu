// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SingleConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="float" /> to and from a YAML float scalar, widening exactly to a double on write. Reading
/// accepts any numeric or string scalar, reads it as a double and narrows it; a null scalar reads as the type default.
/// </summary>
/// <remarks>
/// A finite value outside the range of <see cref="float" />, which would narrow to an infinity, is an overflow and is
/// rejected, as an integer outside its target's range is. Rounding and underflow to zero are accepted, and the
/// infinities and not-a-number read as <see cref="float" />'s own values.
/// </remarks>
internal sealed class SingleConverter
    : YamlConverter<float>
{
    /// <inheritdoc />
    /// <exception cref="YamlSerializationException">
    /// The scalar is a finite value outside the range of <see cref="float" />.
    /// </exception>
    public override float Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return default;

        string text = ScalarCoercion.ReaderScalarText(ref reader);
        double value = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        float narrowed = (float)value;
        if (float.IsInfinity(narrowed) && !double.IsInfinity(value))
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlNumberOutOfRange, text, typeof(float)));
        }

        return narrowed;
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, float value, YamlSerializerOptions options) =>
        writer.WriteDouble(value);
}
