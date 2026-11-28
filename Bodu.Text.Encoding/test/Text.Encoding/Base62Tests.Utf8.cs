// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62Tests.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base62Tests
{
    /// <summary>
    /// Verifies that <see cref="Base62.EncodeToUtf8(ReadOnlySpan{byte})" /> writes a one-byte value as the ASCII byte
    /// of its digit in the GMP-style alphabet.
    /// </summary>
    /// <param name="value">The byte value.</param>
    /// <param name="expected">The expected encoded text.</param>
    [TestMethod]
    [DataRow(1, "1")]
    [DataRow(10, "A")]
    [DataRow(36, "a")]
    [DataRow(61, "z")]
    public void EncodeToUtf8_WhenSingleByte_ShouldWriteItsDigitAsAscii(int value, string expected)
    {
        byte[] actual = Base62.EncodeToUtf8([(byte)value]);

        Assert.AreEqual(expected, System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Base62.EncodeToUtf8(ReadOnlySpan{byte})" /> keeps each leading zero byte as a leading
    /// <c>0</c>, as the character path does.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenSourceHasLeadingZeros_ShouldKeepThemAsLeadingZeroDigits() =>
        Assert.AreEqual("001", System.Text.Encoding.ASCII.GetString(Base62.EncodeToUtf8([0x00, 0x00, 0x01])));

    /// <summary>
    /// Verifies that <see cref="Base62.EncodeToUtf8(ReadOnlySpan{byte})" /> returns an empty array for an empty source.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenSourceIsEmpty_ShouldReturnEmptyArray() =>
        Assert.IsEmpty(Base62.EncodeToUtf8([]));

    /// <summary>
    /// Verifies that <see cref="Base62.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes the encoded
    /// text into a destination of <see cref="Base62.GetMaxEncodedLength(int)" /> bytes.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationIsLargeEnough_ShouldReturnTrueAndWriteTheText()
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes("Hello");
        byte[] destination = new byte[Base62.GetMaxEncodedLength(payload.Length)];

        bool success = Base62.TryEncodeToUtf8(payload, destination, out int bytesWritten);

        Assert.IsTrue(success);
        Assert.AreEqual(Base62.Encode(payload), System.Text.Encoding.ASCII.GetString(destination, 0, bytesWritten));
    }

    /// <summary>
    /// Verifies that <see cref="Base62.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int)" /> returns
    /// <see langword="false" /> and reports nothing written when the destination is one byte shorter than the text.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes("Hello");
        byte[] destination = new byte[Base62.Encode(payload).Length - 1];

        bool success = Base62.TryEncodeToUtf8(payload, destination, out int bytesWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base62.DecodeFromUtf8" /> decodes UTF-8 Base62 text and reports the whole source
    /// consumed and every decoded byte written.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenValidInput_ShouldReturnDoneWithCounts()
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes("Hello");
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(Base62.Encode(payload));
        byte[] destination = new byte[Base62.GetMaxDecodedLength(utf8.Length)];

        OperationStatus status = Base62.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(utf8.Length, bytesConsumed);
        CollectionAssert.AreEqual(payload, destination[..bytesWritten]);
    }

    /// <summary>
    /// Verifies that <see cref="Base62.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" />, with no
    /// progress reported, for a symbol outside the alphabet and for the UTF-8 encoding of a character outside ASCII.
    /// </summary>
    /// <param name="hex">The UTF-8 source, as hexadecimal.</param>
    [TestMethod]
    [DataRow("412B42")]
    [DataRow("C3A4")]
    public void DecodeFromUtf8_WhenInputIsInvalid_ShouldReturnInvalidData(string hex)
    {
        byte[] destination = new byte[16];

        OperationStatus status = Base62.DecodeFromUtf8(Convert.FromHexString(hex), destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.InvalidData, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base62.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />,
    /// with no progress reported, when the decoded bytes do not fit the destination.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReportNoProgress()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(Base62.Encode(System.Text.Encoding.ASCII.GetBytes("Hello")));
        byte[] destination = new byte[4];

        OperationStatus status = Base62.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base62.DecodeFromUtf8" /> returns <see cref="OperationStatus.Done" /> and writes
    /// nothing for an empty source.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenSourceIsEmpty_ShouldReturnDone()
    {
        OperationStatus status = Base62.DecodeFromUtf8([], [], out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that the UTF-8 encode and decode paths agree with the character paths for every payload length to 64,
    /// leading zero bytes included: <see cref="Base62.EncodeToUtf8(ReadOnlySpan{byte})" /> writes the ASCII bytes of
    /// <see cref="Base62.Encode(ReadOnlySpan{byte})" />, and <see cref="Base62.DecodeFromUtf8" /> recovers the payload.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenRoundTrippedThroughDecodeFromUtf8_ShouldMatchTheCharacterPaths()
    {
        var random = new Random(62);
        for (int length = 0; length <= 64; length++)
        {
            byte[] payload = new byte[length];
            random.NextBytes(payload);
            if (length > 1)
                payload[0] = 0;

            byte[] utf8 = Base62.EncodeToUtf8(payload);
            byte[] destination = new byte[Base62.GetMaxDecodedLength(utf8.Length)];
            OperationStatus status = Base62.DecodeFromUtf8(utf8, destination, out _, out int bytesWritten);

            Assert.AreEqual(Base62.Encode(payload), System.Text.Encoding.ASCII.GetString(utf8), $"Encoded text at length {length}.");
            Assert.AreEqual(OperationStatus.Done, status, $"Status at length {length}.");
            CollectionAssert.AreEqual(payload, destination[..bytesWritten], $"Decoded payload at length {length}.");
        }
    }
}
