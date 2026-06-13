// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.Resumable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// A document exercising every construct the resumable protocol must survive splitting: headers, dotted keys,
    /// quoted keys, every scalar kind, escapes, multi-line strings (with line-ending backslashes and CRLF content),
    /// comments, nested arrays, inline tables, arrays of tables, underscored numbers, and multi-byte UTF-8.
    /// </summary>
    private const string TortureDocument =
        "# header comment\r\n" +
        "title = \"TOML Example\" # trailing\n" +
        "\n" +
        "[owner]\n" +
        "name = \"Tom \\u00e9 \\n end\"\n" +
        "dob = 1979-05-27T07:32:00-08:00\n" +
        "\n" +
        "[database]\r\n" +
        "ports = [ 8001, 8001, 8002 ] # ports\n" +
        "data = [ [\"delta\", \"phi\"], [3.14] ]\n" +
        "temp_targets = { cpu = 79.5, case = 72.0 }\n" +
        "\n" +
        "[servers.alpha]\n" +
        "ip = '10.0.0.1'\n" +
        "multiline = \"\"\"\r\nline1 \\\n   joined \"q\" here\"\"\"\n" +
        "mll = '''\nraw \\n text'''\n" +
        "\n" +
        "[[products]]\n" +
        "name = \"Hammer\"\n" +
        "sku = 738_594_937\n" +
        "flt = 1e6\n" +
        "neg = -0.01\n" +
        "big = 9223372036854775807\n" +
        "hex = 0xDEADBEEF\n" +
        "oct = 0o755\n" +
        "bin = 0b11010110\n" +
        "bool1 = true\n" +
        "bool2 = false\n" +
        "ld = 1979-05-27\n" +
        "lt = 07:32:00.999999\n" +
        "ldt = 1979-05-27 07:32:00\n" +
        "odt = 1979-05-27T07:32:00Z\n" +
        "inf1 = inf\n" +
        "nan1 = nan\n" +
        "\"quoted key\" = 'literal'\n" +
        "unicode = \"héllo wörld ☃\"\n" +
        "\n" +
        "[[products]]\n" +
        "dotted.path.key = 42\n";

    /// <summary>
    /// Verifies that feeding the torture document through the multi-block protocol in tiny chunks produces exactly
    /// the token stream of a single-pass read, for every chunk size.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(7)]
    [DataRow(16)]
    [DataRow(64)]
    public void Read_WhenChunked_ShouldMatchSinglePassTokenStream(int chunkSize)
    {
        Utf8TomlReader single = Create(TortureDocument);
        List<string> expected = Drain(ref single);

        CollectionAssert.AreEqual(expected, DrainChunked(TortureDocument, chunkSize), $"chunk size {chunkSize}");
    }

    /// <summary>
    /// Verifies that the chunked protocol matches the single pass for the TOML v1.1.0 grammar features.
    /// </summary>
    [TestMethod]
    public void Read_WhenChunkedV11Document_ShouldMatchSinglePassTokenStream()
    {
        var toml = "p = {\n  x = \"\\e\\x41\", # esc\n  t = 07:32,\n}\nbom = 1\n";

        Utf8TomlReader single = Create(toml, TomlSpecVersion.V1_1);
        List<string> expected = Drain(ref single);

        CollectionAssert.AreEqual(expected, DrainChunked(toml, 1, TomlSpecVersion.V1_1));
    }

    /// <summary>
    /// Verifies that a buffer ending mid-value makes <see cref="Utf8TomlReader.Read" /> return
    /// <see langword="false" /> with the reader restored: the previous token survives and
    /// <see cref="Utf8TomlReader.BytesConsumed" /> covers only fully tokenized input.
    /// </summary>
    [TestMethod]
    public void Read_WhenBufferEndsMidToken_ShouldReturnFalseAndRestoreReader()
    {
        var reader = new Utf8TomlReader("a = \"trunc"u8, isFinalBlock: false, new TomlReaderState());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(TomlTokenType.Key, reader.TokenType);

        // The key read consumes through the equals sign: a(0) space(1) =(2).
        Assert.AreEqual(3, reader.BytesConsumed);

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(TomlTokenType.Key, reader.TokenType);
        Assert.IsTrue(reader.ValueTextEquals("a"u8));
        Assert.AreEqual(3, reader.BytesConsumed);
    }

    /// <summary>
    /// Verifies that line numbers and columns remain accurate across blocks via the carried state.
    /// </summary>
    [TestMethod]
    public void CurrentState_WhenResumedOnLaterLine_ShouldPreserveLinePositions()
    {
        var doc = Encoding.UTF8.GetBytes("a = 1\nb = 2\nlong");
        var reader = new Utf8TomlReader(doc, isFinalBlock: false, new TomlReaderState());
        while (reader.Read())
        {
        }

        // The two complete expressions are consumed up to the second newline; the dangling key "long" — and the
        // newline read in the same incomplete pass — roll back.
        Assert.AreEqual(11, reader.BytesConsumed);

        byte[] rest = [.. doc[11..], .. "er = 3\n"u8.ToArray()];
        var next = new Utf8TomlReader(rest, isFinalBlock: true, reader.CurrentState);
        Assert.IsTrue(next.Read());
        Assert.AreEqual(TomlTokenType.Key, next.TokenType);
        Assert.IsTrue(next.ValueTextEquals("longer"u8));
        Assert.AreEqual(3, next.LineNumber);
        Assert.AreEqual(1, next.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a truncated final block still raises <see cref="TomlFormatException" /> from
    /// <see cref="Utf8TomlReader.Read" />, exactly as a single-block read does.
    /// </summary>
    [TestMethod]
    public void Read_WhenFinalBlockTruncated_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            var reader = new Utf8TomlReader("a = \"trunc"u8, isFinalBlock: true, new TomlReaderState());
            while (reader.Read())
            {
            }
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.Skip" /> requires the final block, mirroring
    /// <see cref="System.Text.Json.Utf8JsonReader.Skip" />.
    /// </summary>
    [TestMethod]
    public void Skip_WhenNotFinalBlock_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8TomlReader("v = [1, 2]\n"u8, isFinalBlock: false, new TomlReaderState());
            _ = reader.Read();
            _ = reader.Read();
            reader.Skip();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.TrySkip" /> on an incomplete value returns <see langword="false" />
    /// and restores the reader, so reading can continue token by token.
    /// </summary>
    [TestMethod]
    public void TrySkip_WhenValueIncomplete_ShouldReturnFalseAndRestoreReader()
    {
        var reader = new Utf8TomlReader("v = [1, 2"u8, isFinalBlock: false, new TomlReaderState());

        _ = reader.Read();
        _ = reader.Read();
        Assert.AreEqual(TomlTokenType.StartArray, reader.TokenType);

        Assert.IsFalse(reader.TrySkip());
        Assert.AreEqual(TomlTokenType.StartArray, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(TomlTokenType.Integer, reader.TokenType);
        Assert.AreEqual(1L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that a byte-order mark split across blocks is skipped once completed.
    /// </summary>
    [TestMethod]
    public void Read_WhenByteOrderMarkSplitAcrossBlocks_ShouldSkipIt()
    {
        var first = new Utf8TomlReader([0xEF], isFinalBlock: false, new TomlReaderState());
        Assert.IsFalse(first.Read());
        Assert.AreEqual(0, first.BytesConsumed);

        var second = new Utf8TomlReader([0xEF, 0xBB, 0xBF, .. "a = 1\n"u8.ToArray()], isFinalBlock: true, first.CurrentState);
        Assert.IsTrue(second.Read());
        Assert.IsTrue(second.ValueTextEquals("a"u8));
        Assert.AreEqual(1, second.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a multi-byte UTF-8 character split across blocks is rejected only once the data proves invalid,
    /// not at the block boundary.
    /// </summary>
    [TestMethod]
    public void Read_WhenMultiByteCharacterSplitAcrossBlocks_ShouldResumeCleanly()
    {
        var e1 = Encoding.UTF8.GetBytes("s = \"é\"\n");

        var first = new Utf8TomlReader(e1.AsSpan(0, 6), isFinalBlock: false, new TomlReaderState());
        Assert.IsTrue(first.Read());
        Assert.IsFalse(first.Read());

        var second = new Utf8TomlReader(e1.AsSpan((int)first.BytesConsumed), isFinalBlock: true, first.CurrentState);
        Assert.IsTrue(second.Read());
        Assert.AreEqual("é", second.GetString());
    }

    /// <summary>
    /// Verifies that an empty non-final buffer simply reports no token.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyNonFinalBuffer_ShouldReturnFalse()
    {
        var reader = new Utf8TomlReader(ReadOnlySpan<byte>.Empty, isFinalBlock: false, new TomlReaderState());

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(0, reader.BytesConsumed);
    }

    /// <summary>
    /// Lexes <paramref name="toml" /> through the multi-block protocol, feeding <paramref name="chunkSize" /> bytes
    /// at a time and stitching unconsumed bytes forward, and returns the formatted token stream.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <param name="chunkSize">The number of new bytes supplied per block.</param>
    /// <param name="specVersion">The specification version to enforce.</param>
    /// <returns>The formatted token entries in read order.</returns>
    private static List<string> DrainChunked(string toml, int chunkSize, TomlSpecVersion specVersion = TomlSpecVersion.V1_0)
    {
        var data = Encoding.UTF8.GetBytes(toml);
        var tokens = new List<string>();
        var state = new TomlReaderState(new TomlReaderOptions { SpecVersion = specVersion });

        byte[] buffer = [];
        var fed = 0;
        while (true)
        {
            var take = Math.Min(chunkSize, data.Length - fed);
            buffer = [.. buffer, .. data.AsSpan(fed, take)];
            fed += take;
            var isFinal = fed == data.Length;

            var reader = new Utf8TomlReader(buffer, isFinal, state);
            while (reader.Read())
                tokens.Add(FormatToken(ref reader));

            state = reader.CurrentState;
            buffer = buffer[(int)reader.BytesConsumed..];

            if (isFinal)
                return tokens;
        }
    }
}
