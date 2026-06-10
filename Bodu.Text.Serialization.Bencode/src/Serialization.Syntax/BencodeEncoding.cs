// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeEncoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Text;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Provides the canonical byte-level encoding primitives shared by the Bencode syntax nodes and the writer adapter.
/// </summary>
internal static class BencodeEncoding
{
    /// <summary>
    /// Writes an integer value in canonical Bencode form (<c>i&lt;value&gt;e</c>).
    /// </summary>
    /// <param name="writer">The destination buffer writer.</param>
    /// <param name="value">The integer value.</param>
    internal static void WriteInteger(IBufferWriter<byte> writer, long value)
    {
        writer.Write("i"u8);
        WriteAsciiInt64(writer, value);
        writer.Write("e"u8);
    }

    /// <summary>
    /// Writes an unsigned integer value in canonical Bencode form (<c>i&lt;value&gt;e</c>).
    /// </summary>
    /// <param name="writer">The destination buffer writer.</param>
    /// <param name="value">The unsigned integer value.</param>
    internal static void WriteInteger(IBufferWriter<byte> writer, ulong value)
    {
        writer.Write("i"u8);
        Span<byte> buffer = stackalloc byte[20];
        _ = Utf8Formatter.TryFormat(value, buffer, out var written);
        writer.Write(buffer[..written]);
        writer.Write("e"u8);
    }

    /// <summary>
    /// Writes a byte string in canonical Bencode form (<c>&lt;length&gt;:&lt;bytes&gt;</c>).
    /// </summary>
    /// <param name="writer">The destination buffer writer.</param>
    /// <param name="content">The raw bytes of the string.</param>
    internal static void WriteByteString(IBufferWriter<byte> writer, ReadOnlySpan<byte> content)
    {
        WriteAsciiInt64(writer, content.Length);
        writer.Write(":"u8);
        writer.Write(content);
    }

    /// <summary>
    /// Writes the base-ten ASCII representation of a signed integer.
    /// </summary>
    /// <param name="writer">The destination buffer writer.</param>
    /// <param name="value">The value to write.</param>
    private static void WriteAsciiInt64(IBufferWriter<byte> writer, long value)
    {
        Span<byte> buffer = stackalloc byte[20];
        _ = Utf8Formatter.TryFormat(value, buffer, out var written);
        writer.Write(buffer[..written]);
    }
}
