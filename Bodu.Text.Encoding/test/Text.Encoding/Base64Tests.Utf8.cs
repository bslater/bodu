// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Utf8.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8" /> returns the Standard variant output as ASCII bytes.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ShouldReturnAsciiBytesOfStandardOutput()
    {
        byte[] actual = Base64.EncodeToUtf8(Ascii("foobar"));

        Assert.AreEqual("Zm9vYmFy", System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenEmpty_ShouldReturnEmptyArray()
    {
        byte[] actual = Base64.EncodeToUtf8(ReadOnlySpan<byte>.Empty);

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8" /> with <see cref="Base64Variant.UrlSafe" /> swaps the alphabet
    /// characters.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenUrlSafeVariant_ShouldSwapAlphabetCharacters()
    {
        byte[] bytes = new byte[] { 0xFB, 0xFF, 0xFE };

        byte[] actual = Base64.EncodeToUtf8(bytes, Base64Variant.UrlSafe);

        string encoded = System.Text.Encoding.ASCII.GetString(actual);
        Assert.IsFalse(encoded.Contains('+'));
        Assert.IsFalse(encoded.Contains('/'));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.EncodeToUtf8" /> with <see cref="BaseFormattingOptions.OmitPadding" /> drops
    /// padding bytes.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenOmitPadding_ShouldNotEmitPaddingBytes()
    {
        byte[] actual = Base64.EncodeToUtf8(Ascii("foob"), Base64Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual("Zm9vYg", System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncodeToUtf8" /> writes the Standard variant output as ASCII bytes.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationLargeEnough_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[8];

        bool ok = Base64.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, bytesWritten);
        Assert.AreEqual("Zm9vYmFy", System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncodeToUtf8" /> returns <see langword="false" /> when destination is too
    /// small.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        byte[] destination = new byte[1];

        bool ok = Base64.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncodeToUtf8" /> rejects unsupported flags.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenUnsupportedFlag_ShouldThrowArgumentException()
    {
        byte[] destination = new byte[16];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base64.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out _, Base64Variant.Standard, BaseFormattingOptions.InsertLineBreaks);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> recovers bytes from a UTF-8 Base64 input and reports
    /// counts.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenStrictValidInput_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy");
        byte[] destination = new byte[6];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(8, bytesConsumed);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> in non-final-block mode returns
    /// <see cref="OperationStatus.NeedMoreData" /> for mid-group input.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenMidGroupAndNotFinal_ShouldReturnNeedMoreData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9v"); // 4 chars - complete
        byte[] partial = System.Text.Encoding.ASCII.GetBytes("Zm9vYmE"); // 7 chars - mid-group
        byte[] destination = new byte[6];

        OperationStatus statusComplete = Base64.DecodeFromUtf8(utf8, destination, out _, out _, isFinalBlock: false);
        Assert.AreEqual(OperationStatus.Done, statusComplete);

        OperationStatus statusPartial = Base64.DecodeFromUtf8(partial, destination, out _, out _, isFinalBlock: false);
        Assert.AreEqual(OperationStatus.NeedMoreData, statusPartial);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> with the UrlSafe variant accepts the substituted alphabet.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenUrlSafeVariant_ShouldDecodeSubstitutedAlphabet()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("-__-");
        byte[] destination = new byte[3];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out _, out int bytesWritten, Base64Variant.UrlSafe);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(3, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />
    /// when the destination is undersized.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReturnDestinationTooSmall()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy");
        byte[] destination = new byte[1];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out int _, out int _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that round-trip via the UTF-8 path recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_EncodeAndDecodeUtf8_ShouldRecoverOriginal()
    {
        byte[] original = Ascii("foobar");
        byte[] encoded = Base64.EncodeToUtf8(original);
        byte[] destination = new byte[6];

        OperationStatus status = Base64.DecodeFromUtf8(encoded, destination, out _, out _);

        Assert.AreEqual(OperationStatus.Done, status);
        CollectionAssert.AreEqual(original, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> rolls back <c>bytesConsumed</c> and <c>bytesWritten</c> to
    /// the last completed 4-symbol / 3-byte quantum boundary when the destination cannot accommodate the next quantum.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReportLastQuantumBoundary()
    {
        // "Zm9vYmFy" decodes to "foobar" (6 bytes). A 3-byte destination fits only the first quantum.
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy");
        byte[] destination = new byte[3];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(4, bytesConsumed);
        Assert.AreEqual(3, bytesWritten);

        byte[] expected = Ascii("foo");
        CollectionAssert.AreEqual(expected, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> reports <see cref="OperationStatus.NeedMoreData" /> with
    /// quantum-aligned counters when the streaming input ends mid-quartet.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenNeedMoreData_ShouldReportLastQuantumBoundary()
    {
        // "Zm9vYg" is one full quartet ("Zm9v" = "foo") + 2 partial chars.
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYg");
        byte[] destination = new byte[6];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten, isFinalBlock: false);

        Assert.AreEqual(OperationStatus.NeedMoreData, status);
        Assert.AreEqual(4, bytesConsumed);
        Assert.AreEqual(3, bytesWritten);
    }
}
