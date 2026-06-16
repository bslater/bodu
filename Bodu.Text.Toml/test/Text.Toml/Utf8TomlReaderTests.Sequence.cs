// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.Sequence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that a single-segment sequence produces the same token stream as the span constructor.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSingleSegmentSequence_ShouldMatchSpanTokenStream()
    {
        byte[] data = Encoding.UTF8.GetBytes(TortureDocument);

        Utf8TomlReader spanReader = Create(TortureDocument);
        List<string> expected = Drain(ref spanReader);

        var sequenceReader = new Utf8TomlReader(new ReadOnlySequence<byte>(data));
        CollectionAssert.AreEqual(expected, Drain(ref sequenceReader));
    }

    /// <summary>
    /// Verifies that a multi-segment sequence produces the same token stream as the span constructor, with values
    /// remaining contiguous through <see cref="Utf8TomlReader.ValueSpan" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMultiSegmentSequence_ShouldMatchSpanTokenStream()
    {
        byte[] data = Encoding.UTF8.GetBytes(TortureDocument);

        Utf8TomlReader spanReader = Create(TortureDocument);
        List<string> expected = Drain(ref spanReader);

        var sequenceReader = new Utf8TomlReader(BuildMultiSegmentSequence(data, 17));
        CollectionAssert.AreEqual(expected, Drain(ref sequenceReader));
    }

    /// <summary>
    /// Verifies that <see cref="TomlDocumentReader" /> accepts single- and multi-segment sequences and yields the
    /// normalized stream.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSequence_ForTomlDocumentReader_ShouldYieldNormalizedStream()
    {
        byte[] data = Encoding.UTF8.GetBytes("[a]\nb = 1\n");

        foreach (int segmentSize in new[] { int.MaxValue, 3 })
        {
            ReadOnlySequence<byte> sequence = segmentSize == int.MaxValue
                ? new ReadOnlySequence<byte>(data)
                : BuildMultiSegmentSequence(data, segmentSize);

            var reader = new TomlDocumentReader(sequence);
            var tokens = new List<TomlTokenType>();
            while (reader.Read())
                tokens.Add(reader.TokenType);

            CollectionAssert.AreEqual(
                new[]
                {
                    TomlTokenType.StartTable,
                    TomlTokenType.PropertyName,
                    TomlTokenType.StartTable,
                    TomlTokenType.PropertyName,
                    TomlTokenType.Integer,
                    TomlTokenType.EndTable,
                    TomlTokenType.EndTable,
                },
                tokens);
        }
    }

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
}
