// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderWriterTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the end-to-end interplay of the low-level <see cref="Utf8BencodeReader" /> and
/// <see cref="Utf8BencodeWriter" /> ref structs. Token-level reading is covered by
/// <see cref="Utf8BencodeReaderTests" /> and emission by <see cref="Utf8BencodeWriterTests" />; this class hosts the
/// shared happy-path smoke test and the write-then-read round-trip suite.
/// </summary>
[TestClass]
public partial class Utf8BencodeReaderWriterTests
{
    /// <summary>
    /// Verifies that the writer emits canonical bytes for a scalar and that the reader reads the value back,
    /// exercising the most important happy path across both ref structs.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Writer_WhenWritingScalar_ShouldEmitCanonicalBytesReadableByReader()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);
        writer.WriteInteger(42);

        Assert.AreEqual("i42e", Encoding.Latin1.GetString(buffer.WrittenSpan));

        var reader = new Utf8BencodeReader(buffer.WrittenSpan);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BencodeTokenType.Integer, reader.TokenType);
        Assert.AreEqual(42L, reader.GetInt64());
        Assert.IsFalse(reader.Read());
    }
}
