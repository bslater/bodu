// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="AsconAead128" /> authenticated encryption algorithm.
/// Tests are partitioned across the following partial files:
/// <list type="bullet">
/// <item><description><c>AsconAead128Tests.Encrypt.cs</c> — constructor validation, <see cref="AsconAead128.Encrypt" /> argument handling, and output-size verification.</description></item>
/// <item><description><c>AsconAead128Tests.Decrypt.cs</c> — <see cref="AsconAead128.Decrypt" /> argument handling, tag-mismatch detection, and round-trip correctness.</description></item>
/// </list>
/// </summary>
[TestClass]
public partial class AsconAead128Tests
{
    private static readonly byte[] ValidKey   = new byte[AsconAead128.KeyBytes];
    private static readonly byte[] ValidNonce = new byte[AsconAead128.NonceBytes];

    static AsconAead128Tests()
    {
        for (int i = 0; i < ValidKey.Length;   i++) ValidKey[i]   = (byte)i;
        for (int i = 0; i < ValidNonce.Length; i++) ValidNonce[i] = (byte)(i + 0x10);
    }

    /// <summary>
    /// Creates a fresh <see cref="AsconAead128" /> instance using the shared test key and nonce,
    /// with associated data already processed. Most tests use this helper to obtain an
    /// encryption-ready instance.
    /// </summary>
    /// <param name="aad">Optional associated data. Defaults to an empty span.</param>
    /// <returns>An <see cref="AsconAead128" /> instance ready for encryption or decryption.</returns>
    private static AsconAead128 MakeInstance(ReadOnlySpan<byte> aad = default)
    {
        AsconAead128 instance = new AsconAead128(ValidKey, ValidNonce);
        instance.ProcessAssociatedData(aad);
        return instance;
    }

    // ── TagSize ───────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.TagSize" /> returns 16 (128 bits), as required by
    /// NIST SP 800-232 for Ascon-AEAD128.
    /// </summary>
    [TestMethod]
    public void TagSize_ShouldReturn16()
    {
        using AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        Assert.AreEqual(16, sut.TagSize);
    }

    // ── ProcessAssociatedData state-machine ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> a second time throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledTwice_ShouldThrowInvalidOperationException()
    {
        using AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> on a disposed
    /// instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        });
    }
}
