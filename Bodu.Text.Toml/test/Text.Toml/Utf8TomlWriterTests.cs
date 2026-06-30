// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the behaviour of <see cref="Utf8TomlWriter" />, the forward-only canonical TOML writer.
/// </summary>
[TestClass]
public sealed partial class Utf8TomlWriterTests
{
    /// <summary>
    /// The UTF-8 encoding used to decode the writer output for text assertions; it omits a byte-order mark.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Builds a TOML document by invoking <paramref name="build" /> against a fresh writer, passed by reference so the
    /// caller can drive the <see langword="ref struct" /> writer.
    /// </summary>
    /// <param name="writer">The writer to drive.</param>
    private delegate void WriterBuild(ref Utf8TomlWriter writer);

    /// <summary>
    /// Writes a document with the supplied build callback and returns the emitted canonical UTF-8 text.
    /// </summary>
    /// <param name="build">The callback that drives the writer.</param>
    /// <returns>The decoded canonical TOML text.</returns>
    private static string WriteDocument(WriterBuild build)
    {
        ArrayBufferWriter<byte> buffer = new();
        Utf8TomlWriter writer = new(buffer);
        build(ref writer);
        return s_utf8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Advances the reader and asserts that it landed on a <see cref="TomlTokenType.PropertyName" /> carrying the
    /// expected key.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="expected">The expected property name.</param>
    private static void ExpectProperty(ref TomlDocumentReader reader, string expected)
    {
        ExpectToken(ref reader, TomlTokenType.PropertyName);
        Assert.AreEqual(expected, reader.GetString());
    }

    /// <summary>
    /// Advances the reader and asserts that it landed on the expected token type.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="expected">The expected token type.</param>
    private static void ExpectToken(ref TomlDocumentReader reader, TomlTokenType expected)
    {
        Assert.IsTrue(reader.Read(), $"Expected {expected} but the reader reported end of document.");
        Assert.AreEqual(expected, reader.TokenType);
    }

}
