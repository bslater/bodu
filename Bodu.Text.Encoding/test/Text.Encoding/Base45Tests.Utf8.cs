// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45Tests.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base45Tests
{
    /// <summary>
    /// Verifies that <see cref="Base45.EncodeToUtf8(ReadOnlySpan{byte})" /> writes the RFC 9285 section 4.3 examples as
    /// the ASCII bytes of their encoded text.
    /// </summary>
    /// <param name="payload">The ASCII payload of the example.</param>
    /// <param name="expected">The example's encoded text.</param>
    [TestMethod]
    [DataRow("AB", "BB8")]
    [DataRow("Hello!!", "%69 VD92EX0")]
    [DataRow("base-45", "UJCLQE7W581")]
    [DataRow("ietf!", "QED8WEX0")]
    public void EncodeToUtf8_WhenRfc9285Example_ShouldWriteTheEncodedTextAsAscii(string payload, string expected)
    {
        byte[] actual = Base45.EncodeToUtf8(System.Text.Encoding.ASCII.GetBytes(payload));

        Assert.AreEqual(expected, System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Base45.EncodeToUtf8(ReadOnlySpan{byte})" /> returns an empty array for an empty source.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenSourceIsEmpty_ShouldReturnEmptyArray() =>
        Assert.IsEmpty(Base45.EncodeToUtf8([]));

    /// <summary>
    /// Verifies that <see cref="Base45.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes the encoded
    /// text into a destination of exactly <see cref="Base45.GetEncodedLength(int)" /> bytes.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationIsTheEncodedLength_ShouldReturnTrueAndWriteTheText()
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes("Hello!!");
        byte[] destination = new byte[Base45.GetEncodedLength(payload.Length)];

        bool success = Base45.TryEncodeToUtf8(payload, destination, out int bytesWritten);

        Assert.IsTrue(success);
        Assert.AreEqual(destination.Length, bytesWritten);
        Assert.AreEqual("%69 VD92EX0", System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base45.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" /> returns
    /// <see langword="false" /> and reports nothing written when the destination is one byte short.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes("Hello!!");
        byte[] destination = new byte[Base45.GetEncodedLength(payload.Length) - 1];

        bool success = Base45.TryEncodeToUtf8(payload, destination, out int bytesWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base45.DecodeFromUtf8" /> decodes UTF-8 Base45 text and reports the whole source
    /// consumed and every decoded byte written.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenValidInput_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("%69 VD92EX0");
        byte[] destination = new byte[Base45.GetMaxDecodedLength(utf8.Length)];

        OperationStatus status = Base45.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(utf8.Length, bytesConsumed);
        Assert.AreEqual(7, bytesWritten);
        Assert.AreEqual("Hello!!", System.Text.Encoding.ASCII.GetString(destination, 0, bytesWritten));
    }

    /// <summary>
    /// Verifies that <see cref="Base45.DecodeFromUtf8" /> skips tab, carriage return and line feed under
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" />, while keeping the space, which is a Base45 symbol.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenIgnoringWhitespace_ShouldSkipLineBreaksButNotSpaces()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("%69 VD\r\n92EX0\t");
        byte[] destination = new byte[16];

        OperationStatus status = Base45.DecodeFromUtf8(utf8, destination, out _, out int bytesWritten, BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual("Hello!!", System.Text.Encoding.ASCII.GetString(destination, 0, bytesWritten));
    }

    /// <summary>
    /// Verifies that <see cref="Base45.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" />, with no
    /// progress reported, for a lower-case letter, an invalid length, a group above 65535, and the UTF-8 encoding of a
    /// character outside ASCII.
    /// </summary>
    /// <param name="hex">The UTF-8 source, as hexadecimal.</param>
    [TestMethod]
    [DataRow("626238")]
    [DataRow("30")]
    [DataRow("474757")]
    [DataRow("C3844238")]
    public void DecodeFromUtf8_WhenInputIsInvalid_ShouldReturnInvalidData(string hex)
    {
        byte[] destination = new byte[16];

        OperationStatus status = Base45.DecodeFromUtf8(Convert.FromHexString(hex), destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.InvalidData, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base45.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />,
    /// with no progress reported, when the decoded bytes do not fit the destination.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReportNoProgress()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("%69 VD92EX0");
        byte[] destination = new byte[6];

        OperationStatus status = Base45.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base45.DecodeFromUtf8" /> returns <see cref="OperationStatus.Done" /> and writes
    /// nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenSourceIsEmpty_ShouldReturnDone()
    {
        OperationStatus status = Base45.DecodeFromUtf8([], [], out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that the UTF-8 encode and decode paths agree with the character paths for every payload length to 64:
    /// <see cref="Base45.EncodeToUtf8(ReadOnlySpan{byte})" /> writes the ASCII bytes of
    /// <see cref="Base45.Encode(ReadOnlySpan{byte})" />, and <see cref="Base45.DecodeFromUtf8" /> recovers the payload.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenRoundTrippedThroughDecodeFromUtf8_ShouldMatchTheCharacterPaths()
    {
        var random = new Random(9285);
        for (int length = 0; length <= 64; length++)
        {
            byte[] payload = new byte[length];
            random.NextBytes(payload);

            byte[] utf8 = Base45.EncodeToUtf8(payload);
            byte[] destination = new byte[Base45.GetMaxDecodedLength(utf8.Length)];
            OperationStatus status = Base45.DecodeFromUtf8(utf8, destination, out _, out int bytesWritten);

            Assert.AreEqual(Base45.Encode(payload), System.Text.Encoding.ASCII.GetString(utf8), $"Encoded text at length {length}.");
            Assert.AreEqual(OperationStatus.Done, status, $"Status at length {length}.");
            CollectionAssert.AreEqual(payload, destination[..bytesWritten], $"Decoded payload at length {length}.");
        }
    }
}
