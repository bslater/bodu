// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class OcbModeTransformTests
{
    // ── Constructor — valid tagLen values ─────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="OcbModeTransform" /> constructor correctly sets
    /// <see cref="OcbModeTransform.TagSize" /> to the supplied <c>tagLen</c> argument
    /// for each of the three tag lengths recommended by RFC 7253, and for the implementation
    /// minimum of 1 byte.
    /// </summary>
    /// <remarks>
    /// RFC 7253 §2.2 identifies 8, 12, and 16 bytes (TAGLEN = 64, 96, and 128 bits) as the
    /// recommended tag lengths. The implementation also permits any value in [1, blockSize].
    /// </remarks>
    [TestMethod]
    [DataRow(1, DisplayName = "tagLen = 1  (minimum)")]
    [DataRow(8, DisplayName = "tagLen = 8  (64-bit, RFC 7253 recommended)")]
    [DataRow(12, DisplayName = "tagLen = 12 (96-bit, RFC 7253 recommended)")]
    [DataRow(16, DisplayName = "tagLen = 16 (128-bit, RFC 7253 recommended, default)")]
    public void Ctor_WithValidTagLength_ShouldSetTagSizeCorrectly(int tagLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var transform = CreateTransform(cipher, new byte[ExpectedBlockSize], tagLen);

        Assert.AreEqual(tagLen, transform.TagSize,
            $"TagSize must equal the tagLen passed to the constructor (tagLen={tagLen}).");
    }

    /// <summary>
    /// Verifies that omitting the optional <c>tagLen</c> parameter defaults
    /// <see cref="OcbModeTransform.TagSize" /> to 16 (128-bit tag).
    /// </summary>
    /// <remarks>
    /// RFC 7253 §2.2 designates TAGLEN = 128 bits as the full-width tag. The default
    /// ensures backward compatibility with call sites that do not specify a tag length,
    /// and matches the vectors in RFC 7253 Appendix A tests 01–16.
    /// </remarks>
    [TestMethod]
    public void Ctor_WithDefaultTagLength_ShouldUseTagSize16()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var transform = new OcbModeTransform(cipher, new byte[ExpectedBlockSize]);

        Assert.AreEqual(16, transform.TagSize,
            "Omitting tagLen must produce TagSize = 16.");
    }

    // ── Constructor — invalid tagLen values ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that a <c>tagLen</c> value of zero or below throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, DisplayName = "tagLen = 0  (below minimum)")]
    [DataRow(-1, DisplayName = "tagLen = -1 (negative)")]
    [DataRow(-16, DisplayName = "tagLen = -16 (large negative)")]
    public void Ctor_WithTagLengthBelowMinimum_ShouldThrowArgumentException(int tagLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);

        Assert.ThrowsExactly<ArgumentException>(() =>
            CreateTransform(cipher, new byte[ExpectedBlockSize], tagLen),
            $"tagLen = {tagLen} is below the minimum of 1 and must throw ArgumentException.");
    }

    /// <summary>
    /// Verifies that a <c>tagLen</c> value exceeding the cipher block size throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    /// <remarks>
    /// The value 128 is included specifically to catch the common mistake of passing the
    /// TAGLEN bit-width rather than the byte count — e.g. writing <c>tagLen: 128</c> when
    /// the intended value is <c>tagLen: 16</c>.
    /// </remarks>
    [TestMethod]
    [DataRow(17, DisplayName = "tagLen = 17  (one above block size)")]
    [DataRow(32, DisplayName = "tagLen = 32  (double block size)")]
    [DataRow(128, DisplayName = "tagLen = 128 (TAGLEN in bits, not bytes — common mistake)")]
    public void Ctor_WithTagLengthAboveBlockSize_ShouldThrowArgumentException(int tagLen)
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);

        Assert.ThrowsExactly<ArgumentException>(() =>
            CreateTransform(cipher, new byte[ExpectedBlockSize], tagLen),
            $"tagLen = {tagLen} exceeds the cipher block size and must throw ArgumentException.");
    }
}