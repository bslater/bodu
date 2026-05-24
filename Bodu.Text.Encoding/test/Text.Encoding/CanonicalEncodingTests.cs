// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CanonicalEncodingTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

/// <summary>
/// Tests for the <see cref="BaseFormatStyles.RequireCanonicalEncoding" /> validation flag on Base32 and Base64.
/// Per RFC 4648 §3.5 a non-canonical encoding has non-zero bits in the final partial group; the canonical flag
/// rejects those inputs while still accepting strictly-canonical ones.
/// </summary>
[TestClass]
public sealed class CanonicalEncodingTests
{

    /// <summary>
    /// Verifies that the canonical encoding "MY======" (single byte 0x66 / 'f') is accepted with the canonical flag.
    /// </summary>
    [TestMethod]
    public void Base32_Decode_WhenCanonicalInput_ShouldAccept()
    {
        var decoded = Base32.Decode("MY======".AsSpan(), Base32Variant.Standard, BaseFormatStyles.RequireCanonicalEncoding);

        CollectionAssert.AreEqual(new byte[] { 0x66 }, decoded);
    }

    /// <summary>
    /// Verifies canonical-mode acceptance across many round-tripped inputs — what we encoded must decode under the
    /// canonical flag without rejection.
    /// </summary>
    /// <param name="inputLength">The payload byte count under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(5)]
    [DataRow(31)]
    [DataRow(64)]
    public void Base32_Decode_WhenCanonicalRoundTrip_ShouldAccept(int inputLength)
    {
        var payload = new byte[inputLength];
        new Random(unchecked((int)0xBEEFCAFE)).NextBytes(payload);

        var encoded = Base32.Encode(payload);
        var decoded = Base32.Decode(encoded.AsSpan(), Base32Variant.Standard, BaseFormatStyles.RequireCanonicalEncoding);

        CollectionAssert.AreEqual(payload, decoded);
    }

    /// <summary>
    /// Verifies that the same non-canonical Base32 input decodes without error when the canonical flag is not set —
    /// the unused bits are silently discarded.
    /// </summary>
    [TestMethod]
    public void Base32_Decode_WhenNonCanonicalInputAndCanonicalFlagAbsent_ShouldDecodeWithDiscardedBits()
    {
        var canonical = Base32.Decode("MY======".AsSpan());
        var nonCanonical = Base32.Decode("MZ======".AsSpan());

        CollectionAssert.AreEqual(canonical, nonCanonical);
    }

    /// <summary>
    /// Verifies that a non-canonical Base32 encoding (non-zero unused bits in the final symbol) is rejected when
    /// the canonical flag is set. "MZ======" has the same decoded bytes as "MY======" but the second symbol's
    /// unused bits are non-zero.
    /// </summary>
    [TestMethod]
    public void Base32_Decode_WhenNonCanonicalInputAndRequireCanonical_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.Decode("MZ======".AsSpan(), Base32Variant.Standard, BaseFormatStyles.RequireCanonicalEncoding);
        });

        Assert.IsTrue(ex.Message.Contains("canonical", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that the Base32 streaming UTF-8 path also enforces canonicity.
    /// </summary>
    [TestMethod]
    public void Base32_DecodeFromUtf8_WhenNonCanonicalInputAndRequireCanonical_ShouldReturnInvalidData()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("MZ======");
        var destination = new byte[1];

        OperationStatus status = Base32.DecodeFromUtf8(
            utf8,
            destination,
            out var _,
            out var _,
            Base32Variant.Standard,
            BaseFormatStyles.RequireCanonicalEncoding);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryDecode" /> returns <see langword="false" /> for non-canonical input when
    /// the canonical flag is set.
    /// </summary>
    [TestMethod]
    public void Base32_TryDecode_WhenNonCanonicalInputAndRequireCanonical_ShouldReturnFalse()
    {
        var destination = new byte[1];

        var ok = Base32.TryDecode("MZ======".AsSpan(), destination, out var _, Base32Variant.Standard, BaseFormatStyles.RequireCanonicalEncoding);

        Assert.IsFalse(ok);
    }

    /// <summary>
    /// Verifies that the canonical Base64 encoding "Zg==" (single byte 'f' = 0x66) is accepted with the canonical flag.
    /// </summary>
    [TestMethod]
    public void Base64_Decode_WhenCanonicalInput_ShouldAccept()
    {
        var decoded = Base64.Decode("Zg==".AsSpan(), Base64Variant.Standard, BaseFormatStyles.RequireCanonicalEncoding);

        CollectionAssert.AreEqual(new byte[] { 0x66 }, decoded);
    }

    /// <summary>
    /// Verifies canonical-mode acceptance for Base64 round-tripped inputs.
    /// </summary>
    /// <param name="inputLength">The payload byte count under test.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(31)]
    [DataRow(64)]
    public void Base64_Decode_WhenCanonicalRoundTrip_ShouldAccept(int inputLength)
    {
        var payload = new byte[inputLength];
        new Random(unchecked((int)0xBEEFCAFE)).NextBytes(payload);

        var encoded = Base64.Encode(payload);
        var decoded = Base64.Decode(encoded.AsSpan(), Base64Variant.Standard, BaseFormatStyles.RequireCanonicalEncoding);

        CollectionAssert.AreEqual(payload, decoded);
    }

    /// <summary>
    /// Verifies that the same non-canonical Base64 input decodes without error when the canonical flag is not set.
    /// </summary>
    [TestMethod]
    public void Base64_Decode_WhenNonCanonicalInputAndCanonicalFlagAbsent_ShouldDecodeWithDiscardedBits()
    {
        var canonical = Base64.Decode("Zg==".AsSpan());
        var nonCanonical = Base64.Decode("Zh==".AsSpan());

        CollectionAssert.AreEqual(canonical, nonCanonical);
    }

    /// <summary>
    /// Verifies that a non-canonical Base64 encoding ("Zh==" — non-zero unused bits) is rejected when the canonical
    /// flag is set. "Zh==" decodes to the same byte as "Zg==" because the bottom 4 bits are unused.
    /// </summary>
    [TestMethod]
    public void Base64_Decode_WhenNonCanonicalInputAndRequireCanonical_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.Decode("Zh==".AsSpan(), Base64Variant.Standard, BaseFormatStyles.RequireCanonicalEncoding);
        });

        Assert.IsTrue(ex.Message.Contains("canonical", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> returns <see langword="false" /> for non-canonical input when
    /// the canonical flag is set.
    /// </summary>
    [TestMethod]
    public void Base64_TryDecode_WhenNonCanonicalInputAndRequireCanonical_ShouldReturnFalse()
    {
        var destination = new byte[1];

        var ok = Base64.TryDecode("Zh==".AsSpan(), destination, out var _, Base64Variant.Standard, BaseFormatStyles.RequireCanonicalEncoding);

        Assert.IsFalse(ok);
    }

}
