// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HkdfTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Validates the <see cref="Hkdf" /> primitive against the published RFC 5869 test vectors, differentially against the
/// BCL <see cref="HKDF" /> implementation, and for its argument-validation contract.
/// </summary>
[TestClass]
public sealed class HkdfTests
{
    /// <summary>
    /// Verifies that <see cref="Hkdf.DeriveKey(HashAlgorithmName, ReadOnlySpan{byte}, int, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// produces a result of the requested length for a simple SHA-256 input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void DeriveKey_WhenGivenSimpleInput_ShouldProduceRequestedLength()
    {
        byte[] output = Hkdf.DeriveKey(HashAlgorithmName.SHA256, "input keying material"u8, 32, salt: "salt"u8, info: "info"u8);

        Assert.AreEqual(32, output.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Hkdf.Extract(HashAlgorithmName, ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> and
    /// <see cref="Hkdf.DeriveKey(HashAlgorithmName, ReadOnlySpan{byte}, int, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// reproduce the published RFC 5869 PRK and OKM for each test case.
    /// </summary>
    /// <param name="testName">The RFC 5869 test-case label.</param>
    /// <param name="hashName">The hash algorithm name.</param>
    /// <param name="ikmHex">The input keying material, hex-encoded.</param>
    /// <param name="saltHex">The salt, hex-encoded (possibly empty).</param>
    /// <param name="infoHex">The info, hex-encoded (possibly empty).</param>
    /// <param name="length">The requested output length.</param>
    /// <param name="expectedPrkHex">The expected pseudorandom key, hex-encoded.</param>
    /// <param name="expectedOkmHex">The expected output keying material, hex-encoded.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(
        "RFC 5869 Test Case 1 (SHA-256)", "SHA256",
        "0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b", "000102030405060708090a0b0c", "f0f1f2f3f4f5f6f7f8f9", 42,
        "077709362c2e32df0ddc3f0dc47bba6390b6c73bb50f9c3122ec844ad7c2b3e5",
        "3cb25f25faacd57a90434f64d0362f2a2d2d0a90cf1a5a4c5db02d56ecc4c5bf34007208d5b887185865")]
    [DataRow(
        "RFC 5869 Test Case 3 (SHA-256, empty salt and info)", "SHA256",
        "0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b", "", "", 42,
        "19ef24a32c717b167f33a91d6f648bdf96596776afdb6377ac434c1c293ccb04",
        "8da4e775a563c18f715f802a063c5a31b8a11f5c5ee1879ec3454e5f3c738d2d9d201395faa4b61a96c8")]
    [DataRow(
        "RFC 5869 Test Case 4 (SHA-1)", "SHA1",
        "0b0b0b0b0b0b0b0b0b0b0b", "000102030405060708090a0b0c", "f0f1f2f3f4f5f6f7f8f9", 42,
        "9b6c18c432a7bf8f0e71c8eb88f4b30baa2ba243",
        "085a01ea1b10f36933068b56efa5ad81a4f14b822f5b091568a9cdd4f155fda2c22e422478d305f3f896")]
    public void Hkdf_WhenGivenRfc5869Vector_ShouldReproducePublishedOutput(
        string testName, string hashName, string ikmHex, string saltHex, string infoHex, int length, string expectedPrkHex, string expectedOkmHex)
    {
        _ = testName;
        var hash = new HashAlgorithmName(hashName);
        byte[] ikm = Convert.FromHexString(ikmHex);
        byte[] salt = Convert.FromHexString(saltHex);
        byte[] info = Convert.FromHexString(infoHex);

        byte[] prk = Hkdf.Extract(hash, ikm, salt);
        byte[] okm = Hkdf.DeriveKey(hash, ikm, length, salt, info);

        CollectionAssert.AreEqual(Convert.FromHexString(expectedPrkHex), prk);
        CollectionAssert.AreEqual(Convert.FromHexString(expectedOkmHex), okm);
    }

    /// <summary>
    /// Verifies that <see cref="Hkdf.Extract(HashAlgorithmName, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />,
    /// <see cref="Hkdf.Expand(HashAlgorithmName, ReadOnlySpan{byte}, int, ReadOnlySpan{byte})" />, and
    /// <see cref="Hkdf.DeriveKey(HashAlgorithmName, ReadOnlySpan{byte}, int, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// match the BCL <see cref="HKDF" /> across hash algorithms and output lengths.
    /// </summary>
    /// <param name="hashName">The hash algorithm name.</param>
    /// <param name="length">The requested output length.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("SHA256", 16)]
    [DataRow("SHA256", 100)]
    [DataRow("SHA384", 48)]
    [DataRow("SHA384", 200)]
    [DataRow("SHA512", 64)]
    [DataRow("SHA512", 300)]
    public void Hkdf_WhenCompoundedAgainstBcl_ShouldMatch(string hashName, int length)
    {
        var hash = new HashAlgorithmName(hashName);
        byte[] ikm = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();
        byte[] salt = Enumerable.Range(0, 13).Select(i => (byte)(0xA0 + i)).ToArray();
        byte[] info = Enumerable.Range(0, 20).Select(i => (byte)(0xF0 + i)).ToArray();

        CollectionAssert.AreEqual(HKDF.Extract(hash, ikm, salt), Hkdf.Extract(hash, ikm, salt));

        byte[] prk = Hkdf.Extract(hash, ikm, salt);
        CollectionAssert.AreEqual(HKDF.Expand(hash, prk, length, info), Hkdf.Expand(hash, prk, length, info));
        CollectionAssert.AreEqual(HKDF.DeriveKey(hash, ikm, length, salt, info), Hkdf.DeriveKey(hash, ikm, length, salt, info));
    }

    /// <summary>
    /// Verifies that an unsupported hash algorithm throws <see cref="ArgumentException" /> naming the
    /// <c>hashAlgorithm</c> parameter.
    /// </summary>
    [TestMethod]
    public void Extract_WhenHashAlgorithmUnsupported_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Hkdf.Extract(HashAlgorithmName.MD5, "ikm"u8, "salt"u8);
        });

        Assert.AreEqual("hashAlgorithm", ex.ParamName);
    }

    /// <summary>
    /// Verifies that requesting more than 255 hash lengths of output throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Expand_WhenOutputLengthTooLarge_ShouldThrowArgumentOutOfRangeException()
    {
        byte[] prk = Hkdf.Extract(HashAlgorithmName.SHA256, "ikm"u8, "salt"u8);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Hkdf.Expand(HashAlgorithmName.SHA256, prk, (255 * 32) + 1);
        });
    }

    /// <summary>
    /// Verifies that requesting zero bytes of output throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Expand_WhenOutputLengthZero_ShouldThrowArgumentOutOfRangeException()
    {
        byte[] prk = Hkdf.Extract(HashAlgorithmName.SHA256, "ikm"u8, "salt"u8);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Hkdf.Expand(HashAlgorithmName.SHA256, prk, 0);
        });
    }

    /// <summary>
    /// Verifies that an extract destination of the wrong length throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Extract_WhenDestinationWrongLength_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Hkdf.Extract(HashAlgorithmName.SHA256, "ikm"u8, "salt"u8, new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that expanding with a pseudorandom key shorter than one hash length throws
    /// <see cref="ArgumentException" /> naming the <c>pseudoRandomKey</c> parameter.
    /// </summary>
    [TestMethod]
    public void Expand_WhenPseudoRandomKeyTooShort_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Hkdf.Expand(HashAlgorithmName.SHA256, new byte[16], 32);
        });

        Assert.AreEqual("pseudoRandomKey", ex.ParamName);
    }
}
