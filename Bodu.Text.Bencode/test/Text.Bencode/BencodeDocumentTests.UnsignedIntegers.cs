// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentTests.UnsignedIntegers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies that <see cref="BencodeDocument" /> supports the full unsigned 64-bit integer range the reader accepts:
/// values above <see cref="long.MaxValue" /> parse, read back through <see cref="BencodeElement.GetUInt64" />, and
/// round-trip, while <see cref="BencodeElement.GetInt64" /> reports them with <see cref="BencodeFormatException" />.
/// </summary>
public partial class BencodeDocumentTests
{
    /// <summary>
    /// Verifies that a document carrying an integer above <see cref="long.MaxValue" /> parses and reads back
    /// losslessly through <see cref="BencodeElement.GetUInt64" />, at both boundary values.
    /// </summary>
    /// <param name="input">The Bencode document.</param>
    /// <param name="expected">The expected unsigned value.</param>
    [TestMethod]
    [DataRow("i9223372036854775808e", 9223372036854775808UL)]
    [DataRow("i18446744073709551615e", ulong.MaxValue)]
    public void GetUInt64_WhenValueExceedsInt64_ShouldReturnValue(string input, ulong expected)
    {
        using var document = BencodeDocument.Parse(Bytes(input));

        Assert.AreEqual(BencodeValueKind.Integer, document.RootElement.ValueKind);
        Assert.AreEqual(expected, document.RootElement.GetUInt64());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetInt64" /> on a value above <see cref="long.MaxValue" /> throws
    /// <see cref="BencodeFormatException" />, mirroring the reader's accessor contract, while
    /// <see cref="BencodeElement.TryGetInt64" /> reports it without throwing.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenValueExceedsInt64_ShouldThrowBencodeFormatException()
    {
        using var document = BencodeDocument.Parse(Bytes("i9223372036854775808e"));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = root.GetInt64();
        });

        Assert.IsFalse(root.TryGetInt64(out long value));
        Assert.AreEqual(0L, value);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeElement.GetUInt64" /> on a negative integer throws
    /// <see cref="BencodeFormatException" /> and <see cref="BencodeElement.TryGetUInt64" /> reports it without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void GetUInt64_WhenValueIsNegative_ShouldThrowBencodeFormatException()
    {
        using var document = BencodeDocument.Parse(Bytes("i-1e"));
        BencodeElement root = document.RootElement;

        _ = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = root.GetUInt64();
        });

        Assert.IsFalse(root.TryGetUInt64(out ulong value));
        Assert.AreEqual(0UL, value);
    }

    /// <summary>
    /// Verifies that both accessor families serve a value within the signed range, the overlap where signed and
    /// unsigned reads agree.
    /// </summary>
    [TestMethod]
    public void TryGetUInt64_WhenValueWithinInt64Range_ShouldAgreeWithTryGetInt64()
    {
        using var document = BencodeDocument.Parse(Bytes("d3:leni42ee"));
        BencodeElement length = document.RootElement.GetProperty("len");

        Assert.IsTrue(length.TryGetInt64(out long signed));
        Assert.AreEqual(42L, signed);
        Assert.IsTrue(length.TryGetUInt64(out ulong unsigned));
        Assert.AreEqual(42UL, unsigned);
        Assert.AreEqual(42UL, length.GetUInt64());
    }

    /// <summary>
    /// Verifies that an element above <see cref="long.MaxValue" /> re-emits byte for byte through
    /// <see cref="BencodeElement.WriteTo" /> and reports its decimal value from
    /// <see cref="BencodeElement.ToString" />.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenValueExceedsInt64_ShouldRoundTripExactBytes()
    {
        byte[] source = Bytes("li18446744073709551615ee");
        using var document = BencodeDocument.Parse(source);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8BencodeWriter(buffer);

        document.WriteTo(writer);

        CollectionAssert.AreEqual(source, buffer.WrittenSpan.ToArray());
        Assert.AreEqual("18446744073709551615", document.RootElement[0].ToString());
    }
}
