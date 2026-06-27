// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the behaviour of <see cref="Utf8TomlReader" />, the source-order UTF-8 TOML lexer: its token vocabulary,
/// lexical validation, and lazy decoding.
/// </summary>
[TestClass]
public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Creates a lexer over the UTF-8 encoding of <paramref name="toml" />.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <param name="specVersion">The specification version to enforce.</param>
    /// <returns>A lexer positioned before the first token.</returns>
    private static Utf8TomlReader Create(string toml, TomlSpecVersion specVersion = TomlSpecVersion.V1_0) =>
        new(Encoding.UTF8.GetBytes(toml), new TomlReaderOptions { SpecVersion = specVersion });

    /// <summary>
    /// Drains the lexer to the end of the document, formatting each token as <c>TokenType(value)</c> — with a
    /// trailing <c>!</c> marking a final key segment — so a test can assert the exact source-order token sequence.
    /// </summary>
    /// <param name="lexer">The lexer to drain.</param>
    /// <returns>The formatted token entries in read order.</returns>
    private static List<string> Drain(ref Utf8TomlReader lexer)
    {
        var tokens = new List<string>();
        while (lexer.Read())
            tokens.Add(FormatToken(ref lexer));

        return tokens;
    }

    /// <summary>
    /// Formats the reader's current token as <c>TokenType(value)</c> — with a trailing <c>!</c> marking a final key
    /// segment — for exact token-sequence assertions.
    /// </summary>
    /// <param name="lexer">The reader positioned on the token to format.</param>
    /// <returns>The formatted token entry.</returns>
    private static string FormatToken(ref Utf8TomlReader lexer) =>
        lexer.TokenType switch
        {
            TomlTokenType.Key => $"Key({lexer.GetString()}){(lexer.IsFinalKeySegment ? "!" : string.Empty)}",
            TomlTokenType.String => $"String({lexer.GetString()})",
            TomlTokenType.Comment => $"Comment({lexer.GetString()})",
            TomlTokenType.Integer => $"Integer({lexer.GetInt64().ToString(CultureInfo.InvariantCulture)})",
            TomlTokenType.Float => $"Float({lexer.GetDouble().ToString("R", CultureInfo.InvariantCulture)})",
            TomlTokenType.Boolean => $"Boolean({(lexer.GetBoolean() ? "true" : "false")})",
            TomlTokenType.OffsetDateTime => $"OffsetDateTime({lexer.GetDateTimeOffset().ToString("o", CultureInfo.InvariantCulture)})",
            TomlTokenType.LocalDateTime => $"LocalDateTime({lexer.GetDateTime().ToString("o", CultureInfo.InvariantCulture)})",
            TomlTokenType.LocalDate => $"LocalDate({lexer.GetDateOnly().ToString("o", CultureInfo.InvariantCulture)})",
            TomlTokenType.LocalTime => $"LocalTime({lexer.GetTimeOnly().ToString("o", CultureInfo.InvariantCulture)})",
            _ => lexer.TokenType.ToString(),
        };

    /// <summary>
    /// Builds a <see cref="ReadOnlySequence{T}" /> over <paramref name="data" /> split into segments of
    /// <paramref name="segmentSize" /> bytes.
    /// </summary>
    /// <param name="data">The bytes to wrap.</param>
    /// <param name="segmentSize">The maximum size of each segment.</param>
    /// <returns>The multi-segment sequence.</returns>
    private static ReadOnlySequence<byte> BuildMultiSegmentSequence(byte[] data, int segmentSize)
    {
        var first = new ByteSegment(data.AsMemory(0, Math.Min(segmentSize, data.Length)));
        ByteSegment last = first;
        for (int offset = first.Memory.Length; offset < data.Length; offset += segmentSize)
            last = last.Append(data.AsMemory(offset, Math.Min(segmentSize, data.Length - offset)));

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    /// <summary>
    /// A minimal <see cref="ReadOnlySequenceSegment{T}" /> for building multi-segment test sequences.
    /// </summary>
    private sealed class ByteSegment
        : ReadOnlySequenceSegment<byte>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ByteSegment" /> class over the supplied memory.
        /// </summary>
        /// <param name="memory">The segment bytes.</param>
        internal ByteSegment(ReadOnlyMemory<byte> memory)
        {
            Memory = memory;
        }

        /// <summary>
        /// Appends a segment holding <paramref name="memory" /> after this one.
        /// </summary>
        /// <param name="memory">The next segment's bytes.</param>
        /// <returns>The appended segment.</returns>
        internal ByteSegment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new ByteSegment(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = next;
            return next;
        }
    }

    /// <summary>
    /// Advances the lexer <paramref name="count" /> tokens, asserting each read succeeds.
    /// </summary>
    /// <param name="lexer">The lexer to advance.</param>
    /// <param name="count">The number of tokens to read.</param>
    private static void Advance(ref Utf8TomlReader lexer, int count)
    {
        for (int i = 0; i < count; i++)
            Assert.IsTrue(lexer.Read(), $"Expected a token at position {i + 1} but the lexer reported end of document.");
    }

}
