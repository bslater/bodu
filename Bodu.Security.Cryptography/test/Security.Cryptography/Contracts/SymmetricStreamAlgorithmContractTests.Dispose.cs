// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmContractTests.Dispose.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Contracts;

public abstract partial class SymmetricStreamAlgorithmContractTests<TCipher>
{
    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.Dispose()" /> twice on the same instance is
    /// idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.Dispose();

        cipher.Dispose();
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed cipher — one that has never had any property accessed or
    /// transform created — completes without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        TCipher cipher = CreateAlgorithm();

        cipher.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.Dispose()" /> zeroes the internal key buffer in place
    /// before nullifying the field, so the secret bytes do not linger on the managed heap after disposal.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldZeroInternalKeyBuffer()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();

        FieldInfo keyField = typeof(SymmetricStreamAlgorithm)
            .GetField("_key", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var capturedKey = (byte[]?)keyField.GetValue(cipher);

        Assert.IsNotNull(capturedKey, "Internal key buffer must exist after GenerateKey.");
        Assert.IsTrue(Array.Exists(capturedKey, b => b != 0), "Generated key must not be all-zero before disposal.");

        cipher.Dispose();

        CollectionAssert.AreEqual(
            new byte[capturedKey.Length],
            capturedKey,
            "Dispose must zero the captured key buffer in place before nullifying the _key field.");
        Assert.IsNull(keyField.GetValue(cipher), "Dispose must nullify the _key field.");
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.Dispose()" /> zeroes the internal nonce buffer in place
    /// before nullifying the field.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldZeroInternalNonceBuffer()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.GenerateNonce();

        FieldInfo nonceField = typeof(SymmetricStreamAlgorithm)
            .GetField("_nonce", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var capturedNonce = (byte[]?)nonceField.GetValue(cipher);

        Assert.IsNotNull(capturedNonce, "Internal nonce buffer must exist after GenerateNonce.");
        Assert.IsTrue(Array.Exists(capturedNonce, b => b != 0), "Generated nonce must not be all-zero before disposal.");

        cipher.Dispose();

        CollectionAssert.AreEqual(
            new byte[capturedNonce.Length],
            capturedNonce,
            "Dispose must zero the captured nonce buffer in place before nullifying the _nonce field.");
        Assert.IsNull(nonceField.GetValue(cipher), "Dispose must nullify the _nonce field.");
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.Key" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cipher.Key);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.Nonce" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Nonce_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.GenerateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cipher.Nonce);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.KeySize" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cipher.KeySize);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.NonceSize" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void NonceSize_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cipher.NonceSize);
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.CreateEncryptor()" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenCalledAfterDispose_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.GenerateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            using ICryptoTransform _ = cipher.CreateEncryptor();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.CreateDecryptor()" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenCalledAfterDispose_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.GenerateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            using ICryptoTransform _ = cipher.CreateDecryptor();
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.CreateTransform(byte[], byte[])" /> after
    /// disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateTransform_WhenCalledAfterDispose_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        var key = new byte[KeyLengthBytes];
        var nonce = new byte[NonceLengthBytes];
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            using ICryptoTransform _ = cipher.CreateTransform(key, nonce);
        });
    }

    /// <summary>
    /// Verifies that an <see cref="ICryptoTransform" /> obtained from the cipher rejects further
    /// <see cref="ICryptoTransform.TransformBlock" /> calls after it has been disposed.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDisposedAndReused_ShouldThrow()
    {
        using TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.GenerateNonce();

        ICryptoTransform transform = cipher.CreateEncryptor();
        transform.Dispose();

        var input = new byte[16];
        var output = new byte[16];

        var threw = false;
        try
        {
            transform.TransformBlock(input, 0, input.Length, output, 0);
        }
        catch
        {
            threw = true;
        }

        Assert.IsTrue(threw, $"{typeof(TCipher).Name} transform must not be reusable after Dispose.");
    }

    /// <summary>
    /// Verifies that calling <see cref="IDisposable.Dispose" /> twice on an <see cref="ICryptoTransform" />
    /// obtained from the cipher is safe and idempotent.
    /// </summary>
    [TestMethod]
    public void Transform_WhenDisposedTwice_ShouldNotThrow()
    {
        using TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.GenerateNonce();

        ICryptoTransform transform = cipher.CreateEncryptor();
        transform.Dispose();

        transform.Dispose();
    }
}
