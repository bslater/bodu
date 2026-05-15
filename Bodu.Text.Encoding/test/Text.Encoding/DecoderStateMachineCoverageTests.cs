// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DecoderStateMachineCoverageTests.cs" company="PlaceholderCompany" >
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

/// <summary>
/// Coverage-focused tests for the P0 decode state machines highlighted in the coverage report:
/// <c>Base32.DecodeCore</c>, <c>Base32.DecodeBitStream</c>, <c>Base64.DecodeBitStream</c>,
/// <c>Base58.TryDecodeCore</c>, and <c>Base85.TryDecodeCore</c>. Each test drives a specific
/// branch identified by the uncovered line ranges in the report.
/// </summary>
[TestClass]
public sealed class DecoderStateMachineCoverageTests
{
    /// <summary>
    /// Verifies that <see cref="Base32.Decode(ReadOnlySpan{char}, Base32Variant, BaseFormatStyles)" /> throws
    /// <see cref="FormatException" /> when a non-padding character appears after padding has begun.
    /// </summary>
    [TestMethod]
    public void Base32_Decode_WhenPaddingFollowedByDataChar_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("MZ=A====".AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("padding", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(ReadOnlySpan{char}, Base32Variant, BaseFormatStyles)" /> throws
    /// <see cref="FormatException" /> when the total non-whitespace symbol count is not a multiple of 8 and the
    /// padding flag is not relaxed.
    /// </summary>
    [TestMethod]
    public void Base32_Decode_WhenTotalSymbolsNotMultipleOfEight_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("MZXW6Y".AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("multiple of 8", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(ReadOnlySpan{char}, Base32Variant, BaseFormatStyles)" /> throws when the
    /// padding character count does not match the expected count for the supplied data length, while the total symbol
    /// count is still a multiple of 8 (drives the dedicated padding-mismatch branch in <c>DecodeCore</c>).
    /// </summary>
    [TestMethod]
    public void Base32_Decode_WhenPaddingCountMismatch_ShouldThrowFormatException()
    {
        // 5 data chars + 11 padding chars = 16 total (multiple of 8) but expected padding for 5 data is 3, not 11.
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("MZXW6===========".AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("padding", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.Decode(ReadOnlySpan{char}, Base32Variant, BaseFormatStyles)" /> throws when the
    /// terminal quantum has 1, 3, or 6 data characters (invalid per RFC 4648 §6 for Standard / HexExtended).
    /// </summary>
    [TestMethod]
    public void Base32_Decode_WhenTerminalQuantumIsOneDataChar_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("M=======".AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("data characters", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> returns <see langword="false" /> when the destination span is
    /// smaller than the decoded byte count, driving the destination-too-small branch in <c>DecodeCore</c>.
    /// </summary>
    [TestMethod]
    public void Base32_TryDecode_WhenDestinationTooSmallMidStream_ShouldReturnFalse()
    {
        byte[] destination = new byte[1];

        bool ok = Base32.TryDecode("MZXW6YTBOI======".AsSpan(), destination, out int bytesWritten);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> when a
    /// non-padding character appears after padding has begun in the streaming bit-stream decoder.
    /// </summary>
    [TestMethod]
    public void Base32_DecodeFromUtf8_WhenPaddingFollowedByDataChar_ShouldReturnInvalidData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("MZ=A====");
        byte[] destination = new byte[5];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> when an
    /// input byte is outside the alphabet's lookup table range.
    /// </summary>
    [TestMethod]
    public void Base32_DecodeFromUtf8_WhenByteAbovePrintableAscii_ShouldReturnInvalidData()
    {
        byte[] utf8 = new byte[] { (byte)'M', (byte)'Z', 0xFF, (byte)'X', (byte)'W', (byte)'6', (byte)'Y', (byte)'Q' };
        byte[] destination = new byte[5];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> with <see cref="OperationStatus.NeedMoreData" /> is returned
    /// when streaming input ends with partial padding (1 to 7 padding chars consumed).
    /// </summary>
    [TestMethod]
    public void Base32_DecodeFromUtf8_WhenPartialPaddingAndNotFinal_ShouldReturnNeedMoreData()
    {
        // 5 data chars + 1 padding char (incomplete pad) when isFinalBlock=false.
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6=");
        byte[] destination = new byte[5];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out _, out _, isFinalBlock: false);

        Assert.AreEqual(OperationStatus.NeedMoreData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> when the
    /// total non-whitespace symbol count is not a multiple of 8 in final-block mode with strict padding.
    /// </summary>
    [TestMethod]
    public void Base32_DecodeFromUtf8_WhenTotalSymbolsNotMultipleOfEight_ShouldReturnInvalidData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6Y");
        byte[] destination = new byte[5];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> when the
    /// padding character count does not match what the symbol count requires (despite the total still being a
    /// multiple of 8).
    /// </summary>
    [TestMethod]
    public void Base32_DecodeFromUtf8_WhenPaddingCountMismatch_ShouldReturnInvalidData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6===========");
        byte[] destination = new byte[5];

        OperationStatus status = Base32.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.Decode(ReadOnlySpan{char}, Base64Variant, BaseFormatStyles)" /> throws when a
    /// non-padding character appears after padding has begun.
    /// </summary>
    [TestMethod]
    public void Base64_DecodeFromUtf8_WhenPaddingFollowedByDataChar_ShouldReturnInvalidData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zg=A");
        byte[] destination = new byte[5];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> when an
    /// input byte is outside the alphabet's lookup table range.
    /// </summary>
    [TestMethod]
    public void Base64_DecodeFromUtf8_WhenByteAbovePrintableAscii_ShouldReturnInvalidData()
    {
        byte[] utf8 = new byte[] { (byte)'Z', 0xFF, (byte)'9', (byte)'v' };
        byte[] destination = new byte[5];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> when the
    /// padding character count does not match the symbol count.
    /// </summary>
    [TestMethod]
    public void Base64_DecodeFromUtf8_WhenPaddingCountMismatch_ShouldReturnInvalidData()
    {
        // "Zm9=" has 3 data chars and 1 padding char — but 3 data chars need 1 padding for a total of 4 chars.
        // That actually decodes successfully ("Zm9" partial = 1 byte). Use a clearer mismatch: 2 data + 1 pad.
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zg=");
        byte[] destination = new byte[5];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out _, out _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.DecodeFromUtf8" /> with partial padding in a non-final block returns
    /// <see cref="OperationStatus.NeedMoreData" /> so the caller can resume.
    /// </summary>
    [TestMethod]
    public void Base64_DecodeFromUtf8_WhenPartialPaddingAndNotFinal_ShouldReturnNeedMoreData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zg=");
        byte[] destination = new byte[5];

        OperationStatus status = Base64.DecodeFromUtf8(utf8, destination, out _, out _, isFinalBlock: false);

        Assert.AreEqual(OperationStatus.NeedMoreData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.Decode(ReadOnlySpan{char}, Base58Variant, BaseFormatStyles)" /> throws when an
    /// input character is outside the alphabet's lookup table range (covers <c>c &gt;= lookup.Length</c> at the
    /// <c>TryDecodeCore</c> branch).
    /// </summary>
    [TestMethod]
    public void Base58_Decode_WhenCharAboveLookupRange_ShouldThrowFormatException()
    {
        // 'ÿ' is past the 128-entry lookup table.
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base58.Decode("AÿA".AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("alphabet", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> throws when an
    /// input character is outside the alphabet's lookup table range.
    /// </summary>
    [TestMethod]
    public void Base85_Decode_WhenCharAboveLookupRange_ShouldThrowFormatException()
    {
        // 'ÿ' is past the 128-entry Ascii85 lookup table.
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("aÿa".AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("alphabet", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base85.Decode(ReadOnlySpan{char}, Base85Variant, BaseFormatStyles)" /> throws with the
    /// trailing-group overflow message when a trailing partial group of <c>uuuu</c> overflows the 32-bit accumulator
    /// during the <c>u</c>-padding step (covers lines 215-218 of <c>TryDecodeCore</c>).
    /// </summary>
    [TestMethod]
    public void Base85_Decode_WhenTrailingGroupOverflowsAfterPadding_ShouldThrowFormatException()
    {
        // 'u' is alphabet index 84 (max). A 4-char trailing group "uuuu" accumulates to 52200624; padding with one
        // more 'u' gives 52200624 * 85 + 84 = 4437053124 which exceeds uint.MaxValue.
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base85.Decode("uuuu".AsSpan());
        });

        Assert.IsTrue(ex.Message.Contains("overflow", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.DecodeFromUtf8" /> with <see cref="BaseFormatStyles.IgnoreWhitespace" /> tolerates
    /// embedded whitespace via the lenient kept-character projection.
    /// </summary>
    [TestMethod]
    public void Base58_DecodeFromUtf8_WhenIgnoreWhitespace_ShouldStripBeforeDecode()
    {
        byte[] payload = System.Text.Encoding.ASCII.GetBytes("Hello");
        byte[] encoded = Base58.EncodeToUtf8(payload);

        // Insert a space byte in the middle.
        byte[] withSpace = new byte[encoded.Length + 1];
        encoded.AsSpan(0, 2).CopyTo(withSpace);
        withSpace[2] = (byte)' ';
        encoded.AsSpan(2).CopyTo(withSpace.AsSpan(3));

        byte[] destination = new byte[payload.Length];
        OperationStatus status = Base58.DecodeFromUtf8(withSpace, destination, out _, out int bytesWritten, Base58Variant.BitcoinFlickr, BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(payload.Length, bytesWritten);
        CollectionAssert.AreEqual(payload, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeFromUtf8" /> tolerates whitespace and the Ascii85 delimiters when both
    /// styles are enabled.
    /// </summary>
    [TestMethod]
    public void Base85_DecodeFromUtf8_WhenDecoratedInput_ShouldDecodeViaScratchProjection()
    {
        string encoded = "<~ 9jqo^ ~>";
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(encoded);
        byte[] destination = new byte[4];

        OperationStatus status = Base85.DecodeFromUtf8(
            utf8,
            destination,
            out _,
            out int bytesWritten,
            Base85Variant.Ascii85,
            BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(System.Text.Encoding.ASCII.GetBytes("Man "), destination);
    }
}
