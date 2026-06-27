// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the <see cref="Utf8TomlReader" /> constructors, including single- and multi-segment sequence input.
/// </summary>
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

}
