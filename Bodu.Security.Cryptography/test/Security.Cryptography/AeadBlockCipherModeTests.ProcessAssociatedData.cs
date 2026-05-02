// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.ProcessAssociatedData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class AeadBlockCipherModeTests<TTransform>
{
    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" /> throws
    /// <see cref="InvalidOperationException" /> when called more than once on the same instance.
    /// Associated data must be supplied exactly once before any encryption or decryption.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledTwice_ShouldThrowInvalidOperationException()
    {
        var transform = MakeTransform();
        transform.ProcessAssociatedData(new byte[] { 1, 2, 3 });

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            transform.ProcessAssociatedData(new byte[] { 4, 5, 6 }));
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" /> throws
    /// <see cref="InvalidOperationException" /> when called after
    /// <see cref="IAeadBlockCipherModeTransform.Encrypt" /> has been invoked on the same instance.
    /// </summary>
    /// <remarks>
    /// AEAD transforms enforce a single-pass contract: each instance is intended for exactly one
    /// encrypt or decrypt operation, and associated data must be supplied before that call.
    /// </remarks>
    [TestMethod]
    public void ProcessAssociatedData_AfterEncrypt_ShouldThrowInvalidOperationException()
    {
        var transform = MakeTransform();
        var plaintext = new byte[ExpectedBlockSize];
        transform.Encrypt(plaintext, new byte[plaintext.Length + transform.TagSize]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            transform.ProcessAssociatedData(new byte[] { 0x01, 0x02, 0x03 }),
            "ProcessAssociatedData must throw after Encrypt has been called on the same instance.");
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" /> throws
    /// <see cref="InvalidOperationException" /> when called after
    /// <see cref="IAeadBlockCipherModeTransform.Decrypt" /> has been invoked on the same instance.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_AfterDecrypt_ShouldThrowInvalidOperationException()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = CreateInitializationVector();
        var plaintext = new byte[ExpectedBlockSize];

        var enc = CreateTransform(cipher, (byte[])iv.Clone());
        var ct = new byte[plaintext.Length + enc.TagSize];
        enc.Encrypt(plaintext, ct);

        var dec = CreateTransform(cipher, (byte[])iv.Clone());
        dec.Decrypt(ct, new byte[plaintext.Length]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            dec.ProcessAssociatedData(new byte[] { 0x01, 0x02, 0x03 }),
            "ProcessAssociatedData must throw after Decrypt has been called on the same instance.");
    }
}
