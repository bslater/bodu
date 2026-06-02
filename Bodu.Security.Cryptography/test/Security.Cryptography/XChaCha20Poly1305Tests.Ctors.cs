// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XChaCha20Poly1305Tests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class XChaCha20Poly1305Tests
{
    /// <summary>
    /// Verifies that the <see cref="XChaCha20Poly1305(byte[], byte[])" /> constructor throws
    /// <see cref="ArgumentNullException" /> when the key array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XChaCha20Poly1305(null!, s_validNonce);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="XChaCha20Poly1305(byte[], byte[])" /> constructor throws
    /// <see cref="ArgumentNullException" /> when the nonce array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new XChaCha20Poly1305(s_validKey, null!);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="XChaCha20Poly1305(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> constructor throws
    /// <see cref="ArgumentException" /> when the key is not 32 bytes.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsWrongSize_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XChaCha20Poly1305(new byte[31], s_validNonce);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="XChaCha20Poly1305(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> constructor throws
    /// <see cref="ArgumentException" /> when the nonce is not 24 bytes.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonceIsWrongSize_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new XChaCha20Poly1305(s_validKey, new byte[23]);
        });
    }
}
