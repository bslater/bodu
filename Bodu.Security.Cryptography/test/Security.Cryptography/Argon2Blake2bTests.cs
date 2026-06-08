// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Blake2bTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the self-contained <see cref="Argon2Blake2b" /> primitive that Argon2 bundles, both against published
/// BLAKE2b reference digests and against the public <see cref="Blake2b" /> type for the lengths it supports.
/// </summary>
[TestClass]
public class Argon2Blake2bTests
{
    /// <summary>
    /// Verifies that the bundled BLAKE2b matches the published BLAKE2b-512 digest of the empty string.
    /// </summary>
    [TestMethod]
    public void Hash_WhenInputIsEmpty_ShouldMatchPublishedBlake2b512()
    {
        Span<byte> digest = stackalloc byte[64];
        Argon2Blake2b.Hash(ReadOnlySpan<byte>.Empty, digest);

        Assert.AreEqual(
            "786a02f742015903c6c6fd852552d272912f4740e15847618a86e217f71f5419" +
            "d25e1031afee585313896444934eb04b903a685b1448b755d56f701afe9be2ce",
            Convert.ToHexString(digest).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that the bundled BLAKE2b matches the published BLAKE2b-512 digest of <c>"abc"</c>.
    /// </summary>
    [TestMethod]
    public void Hash_WhenInputIsAbc_ShouldMatchPublishedBlake2b512()
    {
        Span<byte> digest = stackalloc byte[64];
        Argon2Blake2b.Hash(Encoding.ASCII.GetBytes("abc"), digest);

        Assert.AreEqual(
            "ba80a53f981c4d0d6a2797b69f12f6e94c212f14685ac4b74b12bb6fdbffa2d1" +
            "7d87c5392aab792dc252d5de4533cc9518d38aa8dbf1925ab92386edd4009923",
            Convert.ToHexString(digest).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that the bundled BLAKE2b agrees with the public <see cref="Blake2b" /> type at a shared 256-bit output
    /// size.
    /// </summary>
    [TestMethod]
    public void Hash_WhenOutputIs256Bits_ShouldMatchPublicBlake2b()
    {
        var message = Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

        Span<byte> bundled = stackalloc byte[32];
        Argon2Blake2b.Hash(message, bundled);

        using var blake = new Blake2b(256);
        var reference = blake.ComputeHash(message);

        CollectionAssert.AreEqual(reference, bundled.ToArray());
    }

    /// <summary>
    /// Verifies that the digest length is folded into the parameter block: a 50-byte digest is not a truncation of the
    /// 64-byte digest of the same input.
    /// </summary>
    [TestMethod]
    public void Hash_WhenOutputLengthDiffers_ShouldNotBeATruncation()
    {
        var message = Encoding.ASCII.GetBytes("length parameterization");

        Span<byte> fifty = stackalloc byte[50];
        Span<byte> sixtyFour = stackalloc byte[64];
        Argon2Blake2b.Hash(message, fifty);
        Argon2Blake2b.Hash(message, sixtyFour);

        Assert.IsFalse(fifty.SequenceEqual(sixtyFour[..50]));
    }

    /// <summary>
    /// Verifies that an output length outside the 1–64-byte range is rejected.
    /// </summary>
    [TestMethod]
    public void Hash_WhenOutputLengthExceedsMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            var tooLong = new byte[65];
            Argon2Blake2b.Hash(ReadOnlySpan<byte>.Empty, tooLong);
        });
    }
}
