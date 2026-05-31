// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class OcbModeTransformTests
{
    // ── Constructor — valid tagSize values ────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="OcbModeTransform" /> constructor correctly sets
    /// <see cref="OcbModeTransform.TagSize" /> to the supplied <c>tagSize</c> argument
    /// for each of the three tag sizes recommended by RFC 7253, and for the implementation
    /// minimum of 8 bits (1 byte).
    /// </summary>
    /// <remarks>
    /// RFC 7253 §2.2 identifies TAGLEN = 64, 96, and 128 bits (8, 12, and 16 bytes) as the
    /// recommended tag sizes. The implementation also permits any positive multiple of 8 in
    /// [8, blockSize] bits.
    /// </remarks>
    [TestMethod]
    [DataRow(8, DisplayName = "tagSize = 8 bits (minimum)")]
    [DataRow(64, DisplayName = "tagSize = 64 bits (RFC 7253 recommended)")]
    [DataRow(96, DisplayName = "tagSize = 96 bits (RFC 7253 recommended)")]
    [DataRow(128, DisplayName = "tagSize = 128 bits (RFC 7253 recommended, default)")]
    public void Ctor_WithValidTagSize_ShouldSetTagSizeCorrectly(int tagSize)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        OcbModeTransform transform = CreateTransform(cipher, new byte[ExpectedBlockSize], tagSize);

        Assert.AreEqual(tagSize, transform.TagSize,
            $"TagSize must equal the tagSize passed to the constructor (tagSize={tagSize}).");
    }

    /// <summary>
    /// Verifies that omitting the optional <c>tagSize</c> parameter defaults
    /// <see cref="OcbModeTransform.TagSize" /> to 128 bits (16 bytes).
    /// </summary>
    /// <remarks>
    /// RFC 7253 §2.2 designates TAGLEN = 128 bits as the full-width tag. The default
    /// ensures backward compatibility with call sites that do not specify a tag size,
    /// and matches the vectors in RFC 7253 Appendix A tests 01–16.
    /// </remarks>
    [TestMethod]
    public void Ctor_WithDefaultTagSize_ShouldBe128Bits()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var transform = new OcbModeTransform(cipher, new byte[ExpectedBlockSize]);

        Assert.AreEqual(128, transform.TagSize,
            "Omitting tagSize must produce TagSize = 128 bits.");
    }

    // ── Constructor — invalid tagSize values ──────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <c>tagSize</c> value of zero or below throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, DisplayName = "tagSize = 0   (below minimum)")]
    [DataRow(-1, DisplayName = "tagSize = -1  (negative)")]
    [DataRow(-128, DisplayName = "tagSize = -128 (large negative)")]
    public void Ctor_WithTagSizeBelowMinimum_ShouldThrowExactly(int tagSize)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);

        Assert.ThrowsExactly<ArgumentException>(() =>
            CreateTransform(cipher, new byte[ExpectedBlockSize], tagSize),
            $"tagSize = {tagSize} is below the minimum of 8 bits and must throw ArgumentException.");
    }

    /// <summary>
    /// Verifies that a <c>tagSize</c> value exceeding the cipher block size in bits throws
    /// <see cref="ArgumentException" />, and that values not aligned to 8-bit byte boundaries
    /// also throw.
    /// </summary>
    /// <remarks>
    /// The value 16 is included specifically to catch the common mistake of passing the
    /// byte count rather than the bit width — e.g. writing <c>tagSize: 16</c> when
    /// the intended value is <c>tagSize: 128</c>. 16 bits is a valid byte-aligned value
    /// (a 2-byte tag), so the test instead asserts on values that violate either the upper
    /// bound or the 8-bit alignment rule.
    /// </remarks>
    [TestMethod]
    [DataRow(129, DisplayName = "tagSize = 129 (above block size, not multiple of 8)")]
    [DataRow(136, DisplayName = "tagSize = 136 (above block size)")]
    [DataRow(256, DisplayName = "tagSize = 256 (double block size)")]
    [DataRow(7, DisplayName = "tagSize = 7   (not a multiple of 8)")]
    [DataRow(12, DisplayName = "tagSize = 12  (not a multiple of 8)")]
    public void Ctor_WithTagSizeOutsideAllowedRange_ShouldThrowExactly(int tagSize)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);

        Assert.ThrowsExactly<ArgumentException>(() =>
            CreateTransform(cipher, new byte[ExpectedBlockSize], tagSize),
            $"tagSize = {tagSize} is outside the allowed range or not aligned to 8 bits and must throw ArgumentException.");
    }
}