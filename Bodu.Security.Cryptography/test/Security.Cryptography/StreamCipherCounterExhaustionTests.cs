// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherCounterExhaustionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Regression tests that exercise the block-counter exhaustion latch on the ChaCha20 and Salsa20 keystream
/// engines. The latch is the safety guard that prevents keystream reuse once the block counter would wrap back
/// to its initial value; if it ever stops firing, the cipher would silently emit duplicate keystream blocks under
/// a single (key, nonce) pair — a catastrophic AEAD failure.
/// </summary>
/// <remarks>
/// Both engines latch via a private <c>_counterExhausted</c> field set when <c>_counter</c> advances back to
/// <c>_initialCounter</c>. Real wraparound across the full counter space (2^32 / 2^64 blocks) is infeasible to
/// drive in a unit test, so these tests use reflection to position the counter one step short of the wrap and
/// then exercise the wrap-and-throw transition. Reflection is acceptable here because the engines are friend
/// types (<c>InternalsVisibleTo Bodu.Security.Cryptography.Test</c>) and the test asserts a security contract
/// the engine intentionally exposes through its disposable surface.
/// </remarks>
[TestClass]
public sealed class StreamCipherCounterExhaustionTests
{
    /// <summary>
    /// Verifies that <see cref="ChaCha20StreamCipher.NextKeystreamBlock" /> latches and throws
    /// <see cref="CryptographicException" /> when the next increment would wrap the 32-bit counter back to its
    /// initial value.
    /// </summary>
    [TestMethod]
    public void ChaCha20StreamCipher_NextKeystreamBlock_WhenCounterWouldWrap_ShouldThrowCryptographicException()
    {
        var key = new byte[32];
        var nonce = new byte[12];
        var cipher = new ChaCha20StreamCipher(key, nonce, initialCounter: 1u);

        // Position _counter one step short of wrap. The next NextKeystreamBlock emits block #0 and advances
        // _counter to 1, which equals _initialCounter and latches exhaustion.
        SetField(cipher, "_counter", 0u);

        var block = new byte[64];
        cipher.NextKeystreamBlock(block);

        // Latch must reject the subsequent call before any keystream byte is emitted again.
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.NextKeystreamBlock(block);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Salsa20StreamCipher.NextKeystreamBlock" /> latches and throws
    /// <see cref="CryptographicException" /> when the next increment would wrap the 64-bit counter back to its
    /// initial value.
    /// </summary>
    [TestMethod]
    public void Salsa20StreamCipher_NextKeystreamBlock_WhenCounterWouldWrap_ShouldThrowCryptographicException()
    {
        var key = new byte[32];
        var nonce = new byte[8];
        var cipher = new Salsa20StreamCipher(key, nonce, initialCounter: 1uL);

        SetField(cipher, "_counter", 0uL);

        var block = new byte[64];
        cipher.NextKeystreamBlock(block);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.NextKeystreamBlock(block);
        });
    }

    /// <summary>
    /// Verifies that once the latch is set, every subsequent <see cref="ChaCha20StreamCipher.NextKeystreamBlock" />
    /// call continues to throw rather than silently producing keystream.
    /// </summary>
    [TestMethod]
    public void ChaCha20StreamCipher_NextKeystreamBlock_WhenExhaustionLatched_ShouldThrowOnEverySubsequentCall()
    {
        var key = new byte[32];
        var nonce = new byte[12];
        var cipher = new ChaCha20StreamCipher(key, nonce, initialCounter: 0u);

        SetField(cipher, "_counterExhausted", true);

        var block = new byte[64];
        for (var i = 0; i < 3; i++)
        {
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                cipher.NextKeystreamBlock(block);
            });
        }
    }

    /// <summary>
    /// Verifies that once the latch is set, every subsequent <see cref="Salsa20StreamCipher.NextKeystreamBlock" />
    /// call continues to throw rather than silently producing keystream.
    /// </summary>
    [TestMethod]
    public void Salsa20StreamCipher_NextKeystreamBlock_WhenExhaustionLatched_ShouldThrowOnEverySubsequentCall()
    {
        var key = new byte[32];
        var nonce = new byte[8];
        var cipher = new Salsa20StreamCipher(key, nonce, initialCounter: 0uL);

        SetField(cipher, "_counterExhausted", true);

        var block = new byte[64];
        for (var i = 0; i < 3; i++)
        {
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                cipher.NextKeystreamBlock(block);
            });
        }
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo? field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Expected internal field '{fieldName}' on {instance.GetType().Name}.");
        field.SetValue(instance, value);
    }
}
