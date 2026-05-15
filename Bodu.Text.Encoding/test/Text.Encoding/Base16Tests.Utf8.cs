// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Utf8.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{
    /// <summary>
    /// Verifies that <see cref="Base16.EncodeToUtf8(ReadOnlySpan{byte})" /> returns the ASCII byte representation of
    /// the lower case hex form.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ShouldReturnAsciiBytesOfLowerCaseHex()
    {
        byte[] actual = Base16.EncodeToUtf8(CanonicalBytes.AsSpan());

        Assert.AreEqual(CanonicalHexLower, System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.EncodeToUtf8(ReadOnlySpan{byte})" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenEmptyInput_ShouldReturnEmptyArray()
    {
        byte[] actual = Base16.EncodeToUtf8(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, BaseFormattingOptions)" />
    /// writes the ASCII bytes into the destination and reports the count.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationLargeEnough_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[8];

        bool ok = Base16.TryEncodeToUtf8(CanonicalBytes.AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, bytesWritten);
        Assert.AreEqual(CanonicalHexLower, System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, BaseFormattingOptions)" />
    /// honours <see cref="BaseFormattingOptions.UpperCase" />.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenUpperCaseFlag_ShouldWriteUpperCaseDigits()
    {
        byte[] destination = new byte[8];

        bool ok = Base16.TryEncodeToUtf8(CanonicalBytes.AsSpan(), destination, out _, BaseFormattingOptions.UpperCase);

        Assert.IsTrue(ok);
        Assert.AreEqual(CanonicalHexUpper, System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, BaseFormattingOptions)" />
    /// returns <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        byte[] destination = new byte[1];

        bool ok = Base16.TryEncodeToUtf8(CanonicalBytes.AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncodeToUtf8(ReadOnlySpan{byte}, Span{byte}, out int, BaseFormattingOptions)" />
    /// rejects unsupported formatting flags.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenUnsupportedFlagsRequested_ShouldThrowArgumentException()
    {
        byte[] destination = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.TryEncodeToUtf8(CanonicalBytes.AsSpan(), destination, out _, BaseFormattingOptions.IncludePrefix);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.DecodeFromUtf8" /> recovers the original bytes from UTF-8 hex input and
    /// reports the consumed and written counts.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenStrictValidInput_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(CanonicalHexUpper);
        byte[] destination = new byte[4];

        OperationStatus status = Base16.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(8, bytesConsumed);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.DecodeFromUtf8" /> returns <see cref="OperationStatus.NeedMoreData" /> when an
    /// odd trailing character appears in non-final-block mode.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenOddTrailingCharAndNotFinal_ShouldReturnNeedMoreData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("deadb"); // odd length
        byte[] destination = new byte[4];

        OperationStatus status = Base16.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

        Assert.AreEqual(OperationStatus.NeedMoreData, status);
        Assert.AreEqual(4, bytesConsumed);
        Assert.AreEqual(2, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> when an
    /// odd trailing character appears in final-block mode.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenOddTrailingCharAndFinal_ShouldReturnInvalidData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("deadb");
        byte[] destination = new byte[4];

        OperationStatus status = Base16.DecodeFromUtf8(utf8, destination, out int _, out int _, isFinalBlock: true);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.DecodeFromUtf8" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> tolerates a leading <c>0x</c> in UTF-8 input.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenAllowPrefix_ShouldSkipPrefix()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("0xDEADBEEF");
        byte[] destination = new byte[4];

        OperationStatus status = Base16.DecodeFromUtf8(
            utf8,
            destination,
            out int bytesConsumed,
            out int bytesWritten,
            BaseFormatStyles.AllowPrefix);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(10, bytesConsumed);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />
    /// when the destination cannot accommodate the result.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReturnDestinationTooSmall()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(CanonicalHexLower);
        byte[] destination = new byte[1];

        OperationStatus status = Base16.DecodeFromUtf8(utf8, destination, out int _, out int _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that round-trip via the UTF-8 path recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_EncodeAndDecodeUtf8_ShouldRecoverOriginal()
    {
        byte[] encoded = Base16.EncodeToUtf8(CanonicalBytes.AsSpan());
        byte[] destination = new byte[4];

        OperationStatus status = Base16.DecodeFromUtf8(encoded, destination, out int _, out int _);

        Assert.AreEqual(OperationStatus.Done, status);
        CollectionAssert.AreEqual(CanonicalBytes, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncodeToUtf8" /> succeeds when the destination is exactly the required
    /// size.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationExactlyRequiredSize_ShouldFillAndReportCount()
    {
        byte[] bytes = new byte[4];
        byte[] destination = new byte[8];

        bool ok = Base16.TryEncodeToUtf8(bytes.AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncodeToUtf8" /> returns <see langword="false" /> when the destination is
    /// exactly one byte short of the required size.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationOneByteShort_ShouldReturnFalse()
    {
        byte[] bytes = new byte[4];
        byte[] destination = new byte[7];

        bool ok = Base16.TryEncodeToUtf8(bytes.AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.DecodeFromUtf8" /> in non-final-block mode reports the partial-pair byte
    /// boundary correctly across input lengths.
    /// </summary>
    /// <param name="inputLength">The UTF-8 source length under test.</param>
    /// <param name="expectedBytesConsumed">The expected <c>bytesConsumed</c> count.</param>
    /// <param name="expectedBytesWritten">The expected <c>bytesWritten</c> count.</param>
    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(1, 0, 0)]
    [DataRow(2, 2, 1)]
    [DataRow(3, 2, 1)]
    [DataRow(4, 4, 2)]
    [DataRow(5, 4, 2)]
    [DataRow(8, 8, 4)]
    [DataRow(9, 8, 4)]
    public void DecodeFromUtf8_WhenNonFinalBlock_ShouldReportPairBoundary(int inputLength, int expectedBytesConsumed, int expectedBytesWritten)
    {
        byte[] utf8 = new byte[inputLength];
        for (int i = 0; i < inputLength; i++)
        {
            utf8[i] = (byte)'a'; // 'a' is a valid hex digit
        }

        byte[] destination = new byte[8];
        OperationStatus status = Base16.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

        Assert.AreEqual(expectedBytesConsumed, bytesConsumed);
        Assert.AreEqual(expectedBytesWritten, bytesWritten);

        OperationStatus expectedStatus = (inputLength & 1) == 0
            ? OperationStatus.Done
            : OperationStatus.NeedMoreData;
        Assert.AreEqual(expectedStatus, status);
    }
}
