// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherEngineDisposeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests that the internal keystream engines refuse to produce keystream after disposal. Disposal zeroes the key
/// and derived state, so a post-dispose <c>NextKeystreamBlock</c> would otherwise emit a predictable,
/// key-independent pad derived from all-zero state. The engines are friend types
/// (<c>InternalsVisibleTo Bodu.Security.Cryptography.Test</c>), so the guard is asserted directly.
/// </summary>
[TestClass]
public sealed class StreamCipherEngineDisposeTests
{
    /// <summary>
    /// Verifies that <see cref="ChaCha20StreamCipher.NextKeystreamBlock" /> throws
    /// <see cref="ObjectDisposedException" /> after the engine has been disposed.
    /// </summary>
    [TestMethod]
    public void ChaCha20StreamCipher_NextKeystreamBlock_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var cipher = new ChaCha20StreamCipher(new byte[32], new byte[12], initialCounter: 0u);
        cipher.Dispose();

        byte[] block = new byte[64];
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.NextKeystreamBlock(block);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Salsa20StreamCipher.NextKeystreamBlock" /> throws
    /// <see cref="ObjectDisposedException" /> after the engine has been disposed.
    /// </summary>
    [TestMethod]
    public void Salsa20StreamCipher_NextKeystreamBlock_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var cipher = new Salsa20StreamCipher(new byte[32], new byte[8], initialCounter: 0uL);
        cipher.Dispose();

        byte[] block = new byte[64];
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.NextKeystreamBlock(block);
        });
    }

    /// <summary>
    /// Verifies that <see cref="RabbitStreamCipher.NextKeystreamBlock" /> throws
    /// <see cref="ObjectDisposedException" /> after the engine has been disposed.
    /// </summary>
    [TestMethod]
    public void RabbitStreamCipher_NextKeystreamBlock_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var cipher = new RabbitStreamCipher(new byte[16], new byte[8]);
        cipher.Dispose();

        byte[] block = new byte[cipher.BlockSize];
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.NextKeystreamBlock(block);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Hc128StreamCipher.NextKeystreamBlock" /> throws
    /// <see cref="ObjectDisposedException" /> after the engine has been disposed.
    /// </summary>
    [TestMethod]
    public void Hc128StreamCipher_NextKeystreamBlock_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        var cipher = new Hc128StreamCipher(new byte[16], new byte[16]);
        cipher.Dispose();

        byte[] block = new byte[cipher.BlockSize];
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.NextKeystreamBlock(block);
        });
    }
}
