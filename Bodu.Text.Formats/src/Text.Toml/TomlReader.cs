// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bodu.Text.Toml;

/// <summary>
/// Deserializes TOML text into a <see cref="TomlTable" /> document model. The reader is the deserialization half of the
/// read/write pair; <see cref="TomlWriter" /> is its serialization counterpart, and <see cref="TomlTable" /> holds the
/// resulting structure.
/// </summary>
/// <remarks>
/// <para>
/// The reader enforces strict TOML v1.0.0 by default; pass a <see cref="TomlReaderOptions" /> with
/// <see cref="TomlReaderOptions.SpecVersion" /> set to <see cref="TomlSpecVersion.V1_1" /> to additionally accept the
/// TOML v1.1.0 features (the <c>\e</c> and <c>\xHH</c> escapes, time values without seconds, and multi-line and
/// trailing-comma inline tables).
/// </para>
/// <para>
/// The grammar covers bare, quoted, and dotted keys; the four string forms; decimal, hexadecimal, octal, and binary
/// integers; floats including <c>inf</c> and <c>nan</c>; the four RFC 3339 date-time types; arrays; inline tables;
/// standard tables; and arrays of tables, with the specification's redefinition and ordering rules enforced. Stream
/// input must be valid UTF-8, and character input must consist of valid Unicode scalar values — a lone, unpaired
/// surrogate half is rejected because no valid UTF-8 document can contain one.
/// </para>
/// <para>
/// The class is unsealed so that consumers — such as a configuration provider — may inherit its read capability without
/// gaining a mutation surface.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var reader = new TomlReader();
/// TomlTable document = reader.Read("title = \"example\"\n[owner]\nname = \"Tom\"");
/// using var stream = File.OpenRead("config.toml");
/// TomlTable fromFile = reader.Read(stream);
///]]>
/// </example>
public partial class TomlReader
{
    /// <summary>
    /// The UTF-8 encoding used when reading from a stream; a leading byte-order mark is stripped separately. Invalid
    /// byte sequences are rejected rather than replaced.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// The options that govern parsing, including the TOML specification version.
    /// </summary>
    private readonly TomlReaderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlReader" /> class that enforces strict TOML v1.0.0.
    /// </summary>
    public TomlReader()
        : this(new TomlReaderOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlReader" /> class with the specified options.
    /// </summary>
    /// <param name="options">The options that govern parsing.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    public TomlReader(TomlReaderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        _options = options;
    }

    /// <summary>
    /// Deserializes a TOML document from the specified character span.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="source" /> is not a valid TOML document.
    /// </exception>
    public TomlTable Read(ReadOnlySpan<char> source)
    {
        EnsureValidScalarValues(source);

        Parser parser = new(source, _options.SpecVersion);
        return parser.Parse();
    }

    /// <summary>
    /// Deserializes a TOML document from the specified string.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="source" /> is not a valid TOML document.
    /// </exception>
    public TomlTable Read(string source)
    {
        ThrowHelper.ThrowIfNull(source);
        return Read(source.AsSpan());
    }

    /// <summary>
    /// Deserializes a TOML document by reading <paramref name="reader" /> to its end.
    /// </summary>
    /// <param name="reader">The text reader supplying the TOML source.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlFormatException">Thrown when the contents are not a valid TOML document.</exception>
    public TomlTable Read(TextReader reader)
    {
        ThrowHelper.ThrowIfNull(reader);
        return Read(reader.ReadToEnd().AsSpan());
    }

    /// <summary>
    /// Deserializes a TOML document by reading <paramref name="source" /> to its end as UTF-8 text.
    /// </summary>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <returns>The root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <remarks>
    /// The stream is read to its end and a leading byte-order mark is ignored. The stream is not closed.
    /// </remarks>
    public TomlTable Read(Stream source)
    {
        ThrowHelper.ThrowIfNull(source);
        Bodu.Text.Formats.TextThrowHelper.ThrowIfStreamNotReadable(source);

        using MemoryStream buffer = new();
        source.CopyTo(buffer);
        return Read(Decode(buffer).AsSpan());
    }

    /// <summary>
    /// Asynchronously deserializes a TOML document by reading <paramref name="source" /> to its end as UTF-8 text.
    /// </summary>
    /// <param name="source">The readable stream containing the UTF-8 TOML bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the read.</param>
    /// <returns>A task that completes with the root <see cref="TomlTable" /> of the parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="source" /> does not support reading.</exception>
    /// <exception cref="TomlFormatException">Thrown when the stream contents are not a valid TOML document.</exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the read completes.
    /// </exception>
    /// <remarks>
    /// The stream is read to its end and a leading byte-order mark is ignored. The stream is not closed.
    /// </remarks>
    public async ValueTask<TomlTable> ReadAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);
        Bodu.Text.Formats.TextThrowHelper.ThrowIfStreamNotReadable(source);

        await using MemoryStream buffer = new();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return Read(Decode(buffer).AsSpan());
    }

    /// <summary>
    /// Attempts to deserialize a TOML document from the specified character span without throwing.
    /// </summary>
    /// <param name="source">The TOML source text.</param>
    /// <param name="document">
    /// When this method returns <see langword="true" />, the parsed document; otherwise <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    public bool TryRead(ReadOnlySpan<char> source, [NotNullWhen(true)] out TomlTable? document)
    {
        try
        {
            document = Read(source);
            return true;
        }
        catch (TomlFormatException)
        {
            document = null;
            return false;
        }
    }

    /// <summary>
    /// Decodes the contents of <paramref name="buffer" /> as UTF-8 text, ignoring a leading byte-order mark.
    /// </summary>
    /// <param name="buffer">The buffered bytes.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not valid UTF-8.</exception>
    private static string Decode(MemoryStream buffer)
    {
        ReadOnlySpan<byte> bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            bytes = bytes[3..];

        try
        {
            return s_utf8.GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            throw new TomlFormatException(FormatsResourceStrings.Format_Invalid_TomlInvalidUtf8, ex);
        }
    }

    /// <summary>
    /// Validates that <paramref name="source" /> contains no unpaired UTF-16 surrogate before parsing begins.
    /// </summary>
    /// <param name="source">The source text to validate.</param>
    /// <exception cref="TomlFormatException">
    /// Thrown when <paramref name="source" /> contains an unpaired surrogate.
    /// </exception>
    /// <remarks>
    /// A TOML document is valid UTF-8, and an unpaired surrogate is not a Unicode scalar value that UTF-8 can represent.
    /// Stream input is already guaranteed well-formed by the strict UTF-8 decoder, so this guard exists for the
    /// <see cref="string" />, <see cref="ReadOnlySpan{T}" />, and <see cref="TextReader" /> paths, whose .NET character
    /// data can carry a lone surrogate half that no real UTF-8 source could have produced. Validating once up front
    /// applies the rule uniformly to every place source text appears — string and key values, comments, and the text
    /// between tokens — and keeps reader and writer surrogate policy aligned.
    /// </remarks>
    private static void EnsureValidScalarValues(ReadOnlySpan<char> source)
    {
        var line = 1;
        for (var i = 0; i < source.Length; i++)
        {
            var c = source[i];
            if (c == '\n')
            {
                line++;
                continue;
            }

            if (char.IsHighSurrogate(c))
            {
                // A high surrogate is only valid when immediately followed by a low surrogate to form a scalar value.
                if (i + 1 >= source.Length || !char.IsLowSurrogate(source[i + 1]))
                    throw new TomlFormatException(FormatsResourceStrings.Format_Invalid_TomlUnpairedSurrogate, line);

                i++;
                continue;
            }

            if (char.IsLowSurrogate(c))
                throw new TomlFormatException(FormatsResourceStrings.Format_Invalid_TomlUnpairedSurrogate, line);
        }
    }
}
