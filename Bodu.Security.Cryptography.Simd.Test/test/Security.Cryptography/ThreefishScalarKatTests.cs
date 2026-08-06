// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishScalarKatTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies that the Threefish block cipher reproduces its published vectors in both directions with the SIMD
/// opt-out engaged.
/// </summary>
/// <remarks>
/// <para>
/// The linked Skein golden known-answer suite reaches Threefish only through the hash tier, and Skein never
/// decrypts - <c>Skein.Ubi</c> calls <c>Encrypt</c> alone. The decrypt half of each width is therefore reachable
/// only from the block-cipher tier, which is what this class drives.
/// </para>
/// <para>
/// The vectors are restated here rather than shared with the main test project's curated set, because those live in
/// private static fields of an internal harness class. Restating three tuples keeps this assembly independent of
/// that harness; they are the same all-zero baselines, and a drift in either copy shows up as a failure here.
/// </para>
/// </remarks>
[TestClass]
public sealed class ThreefishScalarKatTests
{
    /// <summary>
    /// Verifies that the disable switch really is engaged for this assembly. Every other assertion in this class
    /// would still pass on the accelerated path, so without this the suite could silently stop testing what it
    /// claims to test.
    /// </summary>
    [TestMethod]
    public void SimdCapabilities_ShouldReportDisabled()
    {
        Assert.IsFalse(SimdCapabilities.Avx512F);
        Assert.IsFalse(SimdCapabilities.Avx512FVL);
    }

    /// <summary>
    /// Verifies that Threefish-256 reproduces its published all-zero-key ciphertext on the scalar path.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenSimdDisabled_ForThreefish256_ShouldMatchPublishedCiphertext()
    {
        using var cipher = new Threefish256Cipher(new byte[32], new byte[16]);

        var actual = new byte[32];
        cipher.Encrypt(new byte[32], actual);

        CollectionAssert.AreEqual(
            Convert.FromHexString("84DA2A1F8BEAEE947066AE3E3103F1AD536DB1F4A1192495116B9F3CE6133FD8"),
            actual);
    }

    /// <summary>
    /// Verifies that Threefish-512 reproduces its published all-zero-key ciphertext on the scalar path.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenSimdDisabled_ForThreefish512_ShouldMatchPublishedCiphertext()
    {
        using var cipher = new Threefish512Cipher(new byte[64], new byte[16]);

        var actual = new byte[64];
        cipher.Encrypt(new byte[64], actual);

        CollectionAssert.AreEqual(
            Convert.FromHexString(
                "B1A2BBC6EF6025BC40EB3822161F36E375D1BB0AEE3186FBD19E47C5D479947B" +
                "7BC2F8586E35F0CFF7E7F03084B0B7B1F1AB3961A580A3E97EB41EA14A6D7BBE"),
            actual);
    }

    /// <summary>
    /// Verifies that Threefish-1024 reproduces its published all-zero-key ciphertext on the scalar path.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenSimdDisabled_ForThreefish1024_ShouldMatchPublishedCiphertext()
    {
        using var cipher = new Threefish1024Cipher(new byte[128], new byte[16]);

        var actual = new byte[128];
        cipher.Encrypt(new byte[128], actual);

        CollectionAssert.AreEqual(
            Convert.FromHexString(
                "F05C3D0A3D05B304F785DDC7D1E036015C8AA76E2F217B06C6E1544C0BC1A90D" +
                "F0ACCB9473C24E0FD54FEA68057F43329CB454761D6DF5CF7B2E9B3614FBD5A2" +
                "0B2E4760B40603540D82EABC5482C171C832AFBE68406BC39500367A592943FA" +
                "9A5B4A43286CA3C4CF46104B443143D560A4B230488311DF4FEEF7E1DFE8391E"),
            actual);
    }

    /// <summary>
    /// Verifies that Threefish-256 decrypts its published ciphertext back to the plaintext on the scalar path. This
    /// is the direction the hash tier never exercises.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenSimdDisabled_ForThreefish256_ShouldRecoverPlaintext()
    {
        using var cipher = new Threefish256Cipher(new byte[32], new byte[16]);

        var actual = new byte[32];
        cipher.Decrypt(
            Convert.FromHexString("84DA2A1F8BEAEE947066AE3E3103F1AD536DB1F4A1192495116B9F3CE6133FD8"),
            actual);

        CollectionAssert.AreEqual(new byte[32], actual);
    }

    /// <summary>
    /// Verifies that Threefish-512 decrypts its published ciphertext back to the plaintext on the scalar path.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenSimdDisabled_ForThreefish512_ShouldRecoverPlaintext()
    {
        using var cipher = new Threefish512Cipher(new byte[64], new byte[16]);

        var actual = new byte[64];
        cipher.Decrypt(
            Convert.FromHexString(
                "B1A2BBC6EF6025BC40EB3822161F36E375D1BB0AEE3186FBD19E47C5D479947B" +
                "7BC2F8586E35F0CFF7E7F03084B0B7B1F1AB3961A580A3E97EB41EA14A6D7BBE"),
            actual);

        CollectionAssert.AreEqual(new byte[64], actual);
    }

    /// <summary>
    /// Verifies that Threefish-1024 decrypts its published ciphertext back to the plaintext on the scalar path.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenSimdDisabled_ForThreefish1024_ShouldRecoverPlaintext()
    {
        using var cipher = new Threefish1024Cipher(new byte[128], new byte[16]);

        var actual = new byte[128];
        cipher.Decrypt(
            Convert.FromHexString(
                "F05C3D0A3D05B304F785DDC7D1E036015C8AA76E2F217B06C6E1544C0BC1A90D" +
                "F0ACCB9473C24E0FD54FEA68057F43329CB454761D6DF5CF7B2E9B3614FBD5A2" +
                "0B2E4760B40603540D82EABC5482C171C832AFBE68406BC39500367A592943FA" +
                "9A5B4A43286CA3C4CF46104B443143D560A4B230488311DF4FEEF7E1DFE8391E"),
            actual);

        CollectionAssert.AreEqual(new byte[128], actual);
    }

    /// <summary>
    /// Verifies that a non-zero key, tweak and plaintext round trip through encrypt and decrypt on the scalar path,
    /// so the fixed vectors above are not passing only because every input word is zero.
    /// </summary>
    /// <param name="width">The Threefish block width, in bits.</param>
    [TestMethod]
    [DataRow(256)]
    [DataRow(512)]
    [DataRow(1024)]
    public void EncryptDecrypt_WhenSimdDisabledAndInputsNonZero_ShouldRoundTrip(int width)
    {
        int blockBytes = width / 8;

        byte[] key = CreateSequence(0x10, blockBytes);
        byte[] tweak = CreateSequence(0x00, 16);
        byte[] plaintext = CreateSequence(0xFF, blockBytes, descending: true);

        using ThreefishBlockCipher cipher = width switch
        {
            256 => new Threefish256Cipher(key, tweak),
            512 => new Threefish512Cipher(key, tweak),
            _ => new Threefish1024Cipher(key, tweak),
        };

        var ciphertext = new byte[blockBytes];
        cipher.Encrypt(plaintext, ciphertext);

        CollectionAssert.AreNotEqual(plaintext, ciphertext);

        var recovered = new byte[blockBytes];
        cipher.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Builds a byte sequence stepping from <paramref name="start" />.
    /// </summary>
    /// <param name="start">The first byte value.</param>
    /// <param name="length">The number of bytes to produce.</param>
    /// <param name="descending">Whether the sequence counts down rather than up.</param>
    /// <returns>The generated sequence.</returns>
    private static byte[] CreateSequence(byte start, int length, bool descending = false)
    {
        var buffer = new byte[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = descending ? (byte)(start - i) : (byte)(start + i);
        }

        return buffer;
    }
}
