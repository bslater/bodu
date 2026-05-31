// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.CreateTransform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateTransform()" /> succeeds on a fresh cipher and returns a
    /// non-null transform, using the cipher's current <see cref="SymmetricStreamAlgorithm.Key" /> and
    /// <see cref="SymmetricStreamAlgorithm.Nonce" />.
    /// </summary>
    [TestMethod]
    public void CreateTransform_WhenInvokedParameterless_ShouldSucceed()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        using ICryptoTransform transform = cipher.CreateTransform();

        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateTransform(byte[], byte[])" /> rejects a
    /// <see langword="null" /> key with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateTransform_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        var nonce = CreateNonce();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cipher.CreateTransform(null!, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateTransform(byte[], byte[])" /> rejects a
    /// <see langword="null" /> nonce with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateTransform_WhenNonceIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        var key = CreateKey();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cipher.CreateTransform(key, null!);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.CreateTransform(byte[], byte[])" /> on a disposed
    /// cipher throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateTransform_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        var key = CreateKey();
        var nonce = CreateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = cipher.CreateTransform(key, nonce);
        });
    }
}
