// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.ProcessAssociatedData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── ProcessAssociatedData state-machine ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> a second time throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledTwice_ShouldThrowInvalidOperationException()
    {
        using var sut = new AsconAead128(ValidKey, ValidNonce);
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
        var sut = new AsconAead128(ValidKey, ValidNonce);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> after a successful
    /// <see cref="AsconAead128.Encrypt" /> throws <see cref="InvalidOperationException" /> rather
    /// than allowing additional AAD to be folded into a finalised state.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledAfterEncrypt_ShouldThrowExactly()
    {
        using AsconAead128 sut = MakeInstance();
        var plaintext = new byte[8];
        var output = new byte[plaintext.Length + AsconAead128.TagBytes];
        sut.Encrypt(plaintext, output);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.ProcessAssociatedData(new byte[4]);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> after a successful
    /// <see cref="AsconAead128.Decrypt" /> throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledAfterDecrypt_ShouldThrowExactly()
    {
        var plaintext = new byte[8];

        using AsconAead128 enc = MakeInstance();
        var sealed_ = new byte[plaintext.Length + AsconAead128.TagBytes];
        enc.Encrypt(plaintext, sealed_);

        using AsconAead128 dec = MakeInstance();
        var recovered = new byte[plaintext.Length];
        dec.Decrypt(sealed_, recovered);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            dec.ProcessAssociatedData(new byte[4]);
        });
    }
}
