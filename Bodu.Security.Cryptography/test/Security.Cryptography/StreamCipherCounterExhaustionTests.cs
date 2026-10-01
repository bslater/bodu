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
/// a single (key, nonce) pair - a catastrophic AEAD failure.
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
        byte[] key = new byte[32];
        byte[] nonce = new byte[12];
        var cipher = new ChaCha20StreamCipher(key, nonce, initialCounter: 1u);

        // Position _counter one step short of wrap. The next NextKeystreamBlock emits block #0 and advances
        // _counter to 1, which equals _initialCounter and latches exhaustion.
        SetField(cipher, "_counter", 0u);

        byte[] block = new byte[64];
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
        byte[] key = new byte[32];
        byte[] nonce = new byte[8];
        var cipher = new Salsa20StreamCipher(key, nonce, initialCounter: 1uL);

        SetField(cipher, "_counter", 0uL);

        byte[] block = new byte[64];
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
        byte[] key = new byte[32];
        byte[] nonce = new byte[12];
        var cipher = new ChaCha20StreamCipher(key, nonce, initialCounter: 0u);

        SetField(cipher, "_counterExhausted", true);

        byte[] block = new byte[64];
        for (int i = 0; i < 3; i++)
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
        byte[] key = new byte[32];
        byte[] nonce = new byte[8];
        var cipher = new Salsa20StreamCipher(key, nonce, initialCounter: 0uL);

        SetField(cipher, "_counterExhausted", true);

        byte[] block = new byte[64];
        for (int i = 0; i < 3; i++)
        {
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                cipher.NextKeystreamBlock(block);
            });
        }
    }

    /// <summary>
    /// Verifies that <see cref="ChaCha20StreamCipher.XorKeystreamBlocks" />, asked for more blocks than remain before
    /// the 32-bit counter returns to its initial value, writes the blocks that remain, latches, and throws
    /// <see cref="CryptographicException" />, as the same number of single-block calls would.
    /// </summary>
    [TestMethod]
    public void ChaCha20StreamCipher_XorKeystreamBlocks_WhenRequestPassesExhaustion_ShouldWriteRemainingBlocksThenThrow()
    {
        byte[] key = new byte[32];
        byte[] nonce = new byte[12];
        var cipher = new ChaCha20StreamCipher(key, nonce, initialCounter: 1u);
        var reference = new ChaCha20StreamCipher(key, nonce, initialCounter: 1u);

        // Three blocks remain before the counter returns to 1: those with counters 0xFFFFFFFE, 0xFFFFFFFF and 0.
        SetField(cipher, "_counter", 0xFFFF_FFFEu);
        SetField(reference, "_counter", 0xFFFF_FFFEu);

        byte[] input = new byte[5 * 64];
        byte[] output = new byte[input.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.XorKeystreamBlocks(input, output);
        });

        byte[] expected = new byte[3 * 64];
        for (int block = 0; block < 3; block++)
            reference.NextKeystreamBlock(expected.AsSpan(block * 64, 64));

        CollectionAssert.AreEqual(expected, output[..(3 * 64)]);
        Assert.IsTrue(output.AsSpan(3 * 64).IndexOfAnyExcept((byte)0) < 0, "No block past exhaustion may be written.");
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.NextKeystreamBlock(new byte[64]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ChaCha20StreamCipher.XorKeystreamBlocks" />, asked for exactly the blocks that remain,
    /// writes them without throwing and latches, so the next call throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void ChaCha20StreamCipher_XorKeystreamBlocks_WhenRequestEndsAtExhaustion_ShouldLatchWithoutThrowing()
    {
        var cipher = new ChaCha20StreamCipher(new byte[32], new byte[12], initialCounter: 1u);
        SetField(cipher, "_counter", 0xFFFF_FFFEu);
        byte[] blocks = new byte[3 * 64];

        cipher.XorKeystreamBlocks(blocks, blocks);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.XorKeystreamBlocks(new byte[64], new byte[64]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ChaCha20StreamCipher.XorKeystreamBlocks" /> throws <see cref="CryptographicException" />
    /// once the exhaustion latch is set.
    /// </summary>
    [TestMethod]
    public void ChaCha20StreamCipher_XorKeystreamBlocks_WhenExhaustionLatched_ShouldThrowCryptographicException()
    {
        var cipher = new ChaCha20StreamCipher(new byte[32], new byte[12], initialCounter: 0u);
        SetField(cipher, "_counterExhausted", true);
        byte[] blocks = new byte[4 * 64];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.XorKeystreamBlocks(blocks, blocks);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Salsa20StreamCipher.XorKeystreamBlocks" />, asked for more blocks than remain before
    /// the 64-bit counter returns to its initial value, writes the blocks that remain, latches, and throws
    /// <see cref="CryptographicException" />, as the same number of single-block calls would.
    /// </summary>
    [TestMethod]
    public void Salsa20StreamCipher_XorKeystreamBlocks_WhenRequestPassesExhaustion_ShouldWriteRemainingBlocksThenThrow()
    {
        byte[] key = new byte[32];
        byte[] nonce = new byte[8];
        var cipher = new Salsa20StreamCipher(key, nonce, initialCounter: 1uL);
        var reference = new Salsa20StreamCipher(key, nonce, initialCounter: 1uL);

        // Three blocks remain before the counter returns to 1: those with counters 2^64 − 2, 2^64 − 1 and 0.
        SetField(cipher, "_counter", ulong.MaxValue - 1);
        SetField(reference, "_counter", ulong.MaxValue - 1);

        byte[] input = new byte[5 * 64];
        byte[] output = new byte[input.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.XorKeystreamBlocks(input, output);
        });

        byte[] expected = new byte[3 * 64];
        for (int block = 0; block < 3; block++)
            reference.NextKeystreamBlock(expected.AsSpan(block * 64, 64));

        CollectionAssert.AreEqual(expected, output[..(3 * 64)]);
        Assert.IsTrue(output.AsSpan(3 * 64).IndexOfAnyExcept((byte)0) < 0, "No block past exhaustion may be written.");
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.NextKeystreamBlock(new byte[64]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Salsa20StreamCipher.XorKeystreamBlocks" /> throws <see cref="CryptographicException" />
    /// once the exhaustion latch is set.
    /// </summary>
    [TestMethod]
    public void Salsa20StreamCipher_XorKeystreamBlocks_WhenExhaustionLatched_ShouldThrowCryptographicException()
    {
        var cipher = new Salsa20StreamCipher(new byte[32], new byte[8], initialCounter: 0uL);
        SetField(cipher, "_counterExhausted", true);
        byte[] blocks = new byte[4 * 64];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.XorKeystreamBlocks(blocks, blocks);
        });
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo? field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Expected internal field '{fieldName}' on {instance.GetType().Name}.");
        field.SetValue(instance, value);
    }
}
