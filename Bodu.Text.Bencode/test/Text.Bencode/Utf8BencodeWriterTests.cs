// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="Utf8BencodeWriter" /> emits canonical Bencode bytes for every value kind and structure,
/// sorts dictionary keys into ascending bytewise order, and reports lifecycle misuse.
/// </summary>
[TestClass]
public partial class Utf8BencodeWriterTests
{
    /// <summary>
    /// Encapsulates a program that drives a <see cref="Utf8BencodeWriter" />, expressed as a named delegate because
    /// the writer is a <see langword="ref struct" /> and therefore cannot be a generic type argument such as
    /// <see cref="Action{T}" />.
    /// </summary>
    /// <param name="writer">The writer to drive.</param>
    private delegate void WriterAction(Utf8BencodeWriter writer);

    /// <summary>
    /// Writes a payload through a fresh writer and returns the emitted bytes decoded as Latin-1 text.
    /// </summary>
    /// <param name="write">The callback that drives the writer.</param>
    /// <returns>The emitted bytes, decoded as Latin-1.</returns>
    private static string Write(WriterAction write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        write(writer);
        return Encoding.Latin1.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Writes a payload through a writer configured with the supplied options and returns the emitted Latin-1 text.
    /// </summary>
    /// <param name="optionsFactory">The factory producing the writer options.</param>
    /// <param name="write">The callback that drives the writer.</param>
    /// <returns>The emitted bytes, decoded as Latin-1.</returns>
    private static string Write(Func<BencodeWriterOptions> optionsFactory, WriterAction write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer, optionsFactory());
        write(writer);
        return Encoding.Latin1.GetString(buffer.WrittenSpan);
    }

}
