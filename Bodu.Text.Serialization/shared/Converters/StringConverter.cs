// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

/// <summary>
/// Converts a <see cref="string" /> value to and from the format's string token, encoding and decoding the text as
/// UTF-8.
/// </summary>
/// <remarks>
/// Decoding follows <see cref="FormatReader.GetString" />: byte sequences that are not valid UTF-8 are replaced with
/// U+FFFD rather than rejected. In a format whose string token can carry arbitrary binary content (such as a Bencode
/// byte string), binding binary data to a <see cref="string" /> member therefore corrupts it silently; map binary
/// content to a <see langword="byte" /> array member instead, which round-trips losslessly.
/// </remarks>
internal sealed class StringConverter
    : SharedConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref FormatReader reader, Type typeToConvert, FormatOptions options)
    {
        if (reader.TokenType != FormatToken.String)
            throw SerializationThrowHelper.ExpectedString(ref reader);

        return reader.GetString();
    }

    /// <inheritdoc />
    public override void Write(FormatWriter writer, string value, FormatOptions options) =>
        writer.WriteString(value);
}
