// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.Utf8.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> with the Crockford variant decodes its alphabet.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenCrockfordVariant_ShouldDecodeCanonicalAlphabet()
    {
        var original = Ascii("foobar");
        var encoded = Base32.Encode(original, Base32Variant.Crockford);
        var utf8 = System.Text.Encoding.ASCII.GetBytes(encoded);
        var destination = new byte[6];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out _, out var bytesWritten, Base32Variant.Crockford);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(original, destination);
    }

    /// <summary>
    /// Verifies that when the destination is smaller than a single Base32 quantum the decoder reports no progress
    /// rather than partial-quantum writes — the OperationStatus contract requires <c>bytesConsumed</c> to map to a
    /// re-feedable position.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationSmallerThanQuantum_ShouldReportZeroProgress()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6YTBOI======");
        var destination = new byte[3];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out var bytesConsumed, out var bytesWritten);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> rolls back <c>bytesConsumed</c> and <c>bytesWritten</c> to
    /// the last completed 8-symbol / 5-byte quantum boundary when the destination cannot accommodate the next quantum.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReportLastQuantumBoundary()
    {
        // "MZXW6YTBOI======" encodes "foobar" (6 bytes). A 5-byte destination fits only the first quantum.
        var utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6YTBOI======");
        var destination = new byte[5];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out var bytesConsumed, out var bytesWritten);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(8, bytesConsumed);
        Assert.AreEqual(5, bytesWritten);

        // The committed bytes correspond to "fooba" (5 bytes of "foobar").
        var expected = Ascii("fooba");
        CollectionAssert.AreEqual(expected, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />
    /// when the destination cannot fit the result.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReturnDestinationTooSmall()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6YTBOI======");
        var destination = new byte[1];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out var _, out var _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> reports <see cref="OperationStatus.NeedMoreData" /> with
    /// quantum-aligned counters when the streaming input ends mid-quantum.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenNeedMoreData_ShouldReportLastQuantumBoundary()
    {
        // "MZXW6YTB" is exactly one full quantum (5 bytes). Append "OI" to start a partial second quantum without
        // padding.
        var utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6YTBOI");
        var destination = new byte[6];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out var bytesConsumed, out var bytesWritten, isFinalBlock: false);

        Assert.AreEqual(OperationStatus.NeedMoreData, status);
        Assert.AreEqual(8, bytesConsumed);
        Assert.AreEqual(5, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> in non-final-block mode returns
    /// <see cref="OperationStatus.NeedMoreData" /> when input ends mid-group.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenPartialGroupAndNotFinal_ShouldReturnNeedMoreData()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6"); // 5 chars - mid-group
        var destination = new byte[6];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out var _, out var _, isFinalBlock: false);

        Assert.AreEqual(OperationStatus.NeedMoreData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> recovers bytes from UTF-8 Base32 input and reports
    /// consumed and written counts.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenStrictValidInput_ShouldReturnDoneWithCounts()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6YTBOI======");
        var destination = new byte[6];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out var bytesConsumed, out var bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(16, bytesConsumed);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }
    /// <summary>
    /// Verifies that <see cref="Base32.EncodeToUtf8" /> returns the ASCII byte representation of the Standard
    /// variant output.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ShouldReturnAsciiBytesOfStandardOutput()
    {
        var actual = Base32.EncodeToUtf8(Ascii("foobar"));

        Assert.AreEqual("MZXW6YTBOI======", System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.EncodeToUtf8" /> returns an empty array for empty input.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenEmpty_ShouldReturnEmptyArray()
    {
        var actual = Base32.EncodeToUtf8([]);

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.EncodeToUtf8" /> honours <see cref="BaseFormattingOptions.OmitPadding" />.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_WhenOmitPadding_ShouldNotEmitPaddingBytes()
    {
        var actual = Base32.EncodeToUtf8(Ascii("foo"), Base32Variant.Standard, BaseFormattingOptions.OmitPadding);

        Assert.AreEqual("MZXW6", System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that encoding to UTF-8 and decoding back recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_EncodeAndDecodeUtf8_ShouldRecoverOriginal()
    {
        var original = Ascii("foobar");

        var encoded = Base32.EncodeToUtf8(original);
        var destination = new byte[6];

        OperationStatus status = Base32.DecodeFromUtf8(encoded, destination, out _, out _);

        Assert.AreEqual(OperationStatus.Done, status);
        CollectionAssert.AreEqual(original, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncodeToUtf8" /> writes the canonical Standard variant output as ASCII
    /// bytes.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationLargeEnough_ShouldReturnTrueAndExpectedBytes()
    {
        var destination = new byte[16];

        var ok = Base32.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(16, bytesWritten);
        Assert.AreEqual("MZXW6YTBOI======", System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncodeToUtf8" /> returns <see langword="false" /> when the destination is
    /// too small.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        var destination = new byte[1];

        var ok = Base32.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out var bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryEncodeToUtf8" /> rejects unsupported formatting flags with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenUnsupportedFlag_ShouldThrowExactly()
    {
        var destination = new byte[16];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base32.TryEncodeToUtf8(Ascii("foobar").AsSpan(), destination, out _, Base32Variant.Standard, BaseFormattingOptions.InsertLineBreaks);
        });
    }

}
