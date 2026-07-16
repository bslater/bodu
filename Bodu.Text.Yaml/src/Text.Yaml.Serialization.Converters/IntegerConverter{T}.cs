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
/// <see cref="YamlNumberHandling.AllowFloatToInteger" />. An unsigned 64-bit value above
/// <see cref="long.MaxValue" /> writes as its invariant text — the scalar re-reads as a string and converts back
/// exactly — because the writer's integer surface is signed.
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
                return (T)Convert.ChangeType(Math.Truncate(d), typeof(T), CultureInfo.InvariantCulture);

            if (!double.IsFinite(d) || Math.Floor(d) != d)
            {
                throw new YamlSerializationException(string.Format(
                    CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_YamlFloatNotIntegral, d.ToString(CultureInfo.InvariantCulture)));
            }
        }

        string text = ScalarCoercion.ReaderScalarText(ref reader);
        try
        {
            return (T)Convert.ChangeType(text, typeof(T), CultureInfo.InvariantCulture);
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
        if (value is ulong unsigned)
        {
            if (unsigned <= long.MaxValue)
                writer.WriteInteger((long)unsigned);
            else
                writer.WriteString(unsigned.ToString(CultureInfo.InvariantCulture));

            return;
        }

        writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
    }
}
