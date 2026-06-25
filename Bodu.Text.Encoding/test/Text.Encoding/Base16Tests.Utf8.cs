// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

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
    /// Verifies that the lenient UTF-8 decode path with <see cref="BaseFormatStyles.AllowPrefix" /> still works when
    /// the source does not start with the prefix (the prefix-skip branch is a no-op).
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenAllowPrefixButPrefixMissing_ShouldDecodeWithoutSkip()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("DEADBEEF");
        byte[] destination = new byte[4];

        OperationStatus status = Base16.DecodeFromUtf8(
            utf8,
            destination,
            out int bytesConsumed,
            out int bytesWritten,
            BaseFormatStyles.AllowPrefix);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(utf8.Length, bytesConsumed);
        Assert.AreEqual(4, bytesWritten);
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
    /// Verifies that the lenient UTF-8 decode path returns a <c>bytesConsumed</c> that points at the source position
    /// where decoding stopped — mapped through the kept-character projection so the caller can resume cleanly. The
    /// previous implementation hard-coded <c>bytesConsumed = 0</c> on partial outcomes.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenLenientStylesAndDestinationTooSmall_ShouldMapKeptPositionBackToSourceIndex()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("DE AD BE EF");
        byte[] destination = new byte[2];

        OperationStatus status = Base16.DecodeFromUtf8(
            utf8,
            destination,
            out int bytesConsumed,
            out int bytesWritten,
            BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(2, bytesWritten);

        // Kept positions [0..3] = source [0,1,3,4]; next unconsumed kept position 4 maps to source index 6 ("B" after
        // the third space).
        Assert.AreEqual(6, bytesConsumed);
        Assert.AreEqual(0xDE, destination[0]);
        Assert.AreEqual(0xAD, destination[1]);
    }

    /// <summary>
    /// Verifies that the lenient UTF-8 decode path reports <see cref="OperationStatus.Done" /> with the full source
    /// length consumed when the input is valid and the destination is sized to fit.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenLenientStylesAndDone_ShouldConsumeEntireSource()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("0xDE AD");
        byte[] destination = new byte[2];

        OperationStatus status = Base16.DecodeFromUtf8(
            utf8,
            destination,
            out int bytesConsumed,
            out int bytesWritten,
            BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(7, bytesConsumed);
        Assert.AreEqual(2, bytesWritten);
    }

    /// <summary>
    /// Verifies that the lenient UTF-8 decode path returns <see cref="OperationStatus.InvalidData" /> when an invalid
    /// hex character remains after whitespace stripping. The partial-bytes-written counter reflects the strict
    /// decoder's progress before it hit the invalid pair, and <c>bytesConsumed</c> resets to <c>0</c>.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenLenientStylesAndInvalidCharAfterStripping_ShouldReturnInvalidData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("DE AD ZZ");
        byte[] destination = new byte[4];

        OperationStatus status = Base16.DecodeFromUtf8(
            utf8,
            destination,
            out int bytesConsumed,
            out int bytesWritten,
            BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(OperationStatus.InvalidData, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(2, bytesWritten);
    }

    /// <summary>
    /// Verifies that the lenient UTF-8 decode path takes the heap-allocated scratch branch when the source exceeds the
    /// 256-byte stackalloc threshold, and the kept-to-source map is still correct for partial outcomes.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenLenientStylesAndLargeInput_ShouldTakeHeapPath()
    {
        // 600 hex digits = 1200 UTF-8 bytes — well above the 256-byte stackalloc threshold for the lenient projection.
        byte[] payload = new byte[300];
        new Random(0xC0DE).NextBytes(payload);
        byte[] encoded = Base16.EncodeToUtf8(payload);

        // Inject whitespace every 10 bytes so the IgnoreWhitespace path is exercised on the heap-allocated map.
        byte[] withWhitespace = new byte[encoded.Length + (encoded.Length / 10)];
        int dst = 0;
        for (int i = 0; i < encoded.Length; i++)
        {
            withWhitespace[dst++] = encoded[i];
            if (((i + 1) % 10) == 0 && i != encoded.Length - 1)
                withWhitespace[dst++] = (byte)' ';
        }

        byte[] destination = new byte[payload.Length];
        OperationStatus status = Base16.DecodeFromUtf8(
            withWhitespace.AsSpan(0, dst),
            destination,
            out int bytesConsumed,
            out int bytesWritten,
            BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(dst, bytesConsumed);
        Assert.AreEqual(payload.Length, bytesWritten);
        CollectionAssert.AreEqual(payload, destination);
    }

    /// <summary>
    /// Verifies that the lenient UTF-8 decode path reports the kept-character mapping back to the source for
    /// <see cref="OperationStatus.NeedMoreData" /> when an odd trailing kept character remains after stripping
    /// whitespace.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenLenientStylesAndOddTrailingNonFinal_ShouldReportSourceProgress()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("DE AD B");
        byte[] destination = new byte[4];

        OperationStatus status = Base16.DecodeFromUtf8(
            utf8,
            destination,
            out int bytesConsumed,
            out int bytesWritten,
            BaseFormatStyles.IgnoreWhitespace,
            isFinalBlock: false);

        Assert.AreEqual(OperationStatus.NeedMoreData, status);
        Assert.AreEqual(2, bytesWritten);

        // Kept = "DEADB" (5 chars). Strict decode consumes 4 chars (2 pairs). Trailing kept char "B" is at kept index 4,
        // which maps back to source index 6.
        Assert.AreEqual(6, bytesConsumed);
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
        byte[] actual = Base16.EncodeToUtf8([]);

        Assert.IsEmpty(actual);
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
    public void TryEncodeToUtf8_WhenUnsupportedFlagsRequested_ShouldThrowExactly()
    {
        byte[] destination = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.TryEncodeToUtf8(CanonicalBytes.AsSpan(), destination, out _, BaseFormattingOptions.IncludePrefix);
        });
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

}
