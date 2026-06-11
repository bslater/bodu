// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DecimalConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml.Serialization.Converters;

/// <summary>
/// Converts a <see cref="decimal" /> value to and from TOML. Because TOML has no native decimal type, the value maps
/// either to a TOML float or to an invariant-culture basic string, selected by
/// <see cref="TomlSerializerOptions.DecimalHandling" />.
/// </summary>
/// <remarks>
/// <para>
/// Under <see cref="TomlDecimalHandling.Float" /> the value is written through <see cref="double" />, TOML's IEEE 754
/// binary64 float, so precision beyond roughly fifteen to seventeen significant digits is lost;
/// <see cref="TomlDecimalHandling.String" /> preserves the value exactly.
/// </para>
/// <para>
/// On read the converter accepts a TOML float, integer, or string regardless of the configured handling, so a document
/// produced under one setting can be read under either. A float outside the <see cref="decimal" /> range — including
/// the <c>nan</c> and <c>inf</c> sentinels — and a string that is not an invariant-culture decimal surface as
/// serialization errors.
/// </para>
/// </remarks>
internal sealed class DecimalConverter
    : TomlConverter<decimal>
{
    /// <inheritdoc />
    public override decimal Read(ref TomlDocumentReader reader, Type typeToConvert, TomlSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case TomlTokenType.Float:
                var floatValue = reader.GetDouble();
                try
                {
                    return (decimal)floatValue;
                }
                catch (OverflowException ex)
                {
                    throw new TomlSerializationException(
                        string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_TypeConversion, floatValue, typeof(decimal)),
                        ex);
                }

            case TomlTokenType.Integer:
                return reader.GetInt64();

            case TomlTokenType.String:
                var text = reader.GetString();
                return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
                    ? value
                    : throw new TomlSerializationException(
                        string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_TypeConversion, text, typeof(decimal)));

            default:
                throw new TomlSerializationException(
                    string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ExpectedDecimal, reader.TokenType));
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8TomlWriter writer, decimal value, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        if (options.DecimalHandling == TomlDecimalHandling.String)
        {
            writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
            return;
        }

        writer.WriteFloat((double)value);
    }
}
