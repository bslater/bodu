// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransformTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class CtrModeTransformTests
    : BlockCipherModeTests<CtrModeTransform>
{
    /// <inheritdoc />
    protected override CtrModeTransform CreateTransform(IBlockCipher cipher, byte[] iv) =>
        new(cipher, iv);

    /// <inheritdoc />
    /// <remarks>
    /// CTR's constructor parameter is named <c>initialCounter</c> rather than <c>iv</c>, so the
    /// argument-validation tests assert that exceptions expose this parameter name.
    /// </remarks>
    protected override string IvParameterName => "initialCounter";

    /// <inheritdoc />
    /// <remarks>
    /// CTR is a stream cipher construction: the last block of keystream is truncated to match the
    /// plaintext length, so non-block-aligned input is valid and must not throw.
    /// </remarks>
    protected override bool RequiresBlockAlignedInput => false;

    /// <inheritdoc />
    /// <remarks>
    /// CTR detects and refuses to reproduce the initial keystream after the internal counter wraps
    /// through its full value space, so
    /// <see cref="BlockCipherModeTests{TMode}.Transform_WhenCounterWouldWrap_ShouldThrowCryptographicException" />
    /// is executed for this mode.
    /// </remarks>
    protected override bool GuardsAgainstKeystreamReuse => true;

    /// <summary>
    /// Gets the ciphers the batching tests run CTR over: AES and Serpent-128 with 16-byte blocks, Skipjack with 8-byte
    /// blocks, and Threefish-512 with 64-byte blocks, so a run of counters divides differently for each. Serpent-128
    /// forms its counter blocks and applies their keystream in its own kernels, eight, four or one block at a time.
    /// </summary>
    /// <returns>One row per cipher: its name, and a factory for a fresh keyed instance.</returns>
    public static IEnumerable<object[]> BatchingCiphers()
    {
        yield return new object[] { "AES-128", (Func<IBlockCipher>)(() => new AesBlockCipher(Enumerable.Range(1, 16).Select(i => (byte)i).ToArray())) };
        yield return new object[] { "Serpent-128", (Func<IBlockCipher>)(() => new Serpent128Cipher(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray())) };
        yield return new object[] { "Skipjack", (Func<IBlockCipher>)(() => new SkipjackBlockCipher(Enumerable.Range(1, 10).Select(i => (byte)i).ToArray())) };
        yield return new object[] { "Threefish-512", (Func<IBlockCipher>)(() => new Threefish512Cipher(Enumerable.Range(1, 64).Select(i => (byte)i).ToArray(), new byte[16])) };
    }

    /// <summary>
    /// Computes CTR output one block at a time: each call consumes one counter block per whole or partial block, and
    /// the counter is incremented big-endian across the whole block.
    /// </summary>
    /// <param name="cipher">The cipher producing the keystream.</param>
    /// <param name="initialCounter">The first counter block.</param>
    /// <param name="input">The concatenated input of every call.</param>
    /// <param name="calls">The length of each call, in order.</param>
    /// <returns>The concatenated output of every call.</returns>
    private static byte[] ReferenceKeystreamXor(IBlockCipher cipher, byte[] initialCounter, byte[] input, int[] calls)
    {
        int blockSize = cipher.BlockSize / 8;
        byte[] counter = (byte[])initialCounter.Clone();
        byte[] keystream = new byte[blockSize];
        byte[] output = new byte[input.Length];
        int start = 0;

        foreach (int length in calls)
        {
            for (int offset = 0; offset < length; offset += blockSize)
            {
                cipher.Encrypt(counter, keystream);
                for (int i = counter.Length - 1; i >= 0 && ++counter[i] == 0; i--)
                {
                }

                for (int i = 0; i < Math.Min(blockSize, length - offset); i++)
                    output[start + offset + i] = (byte)(input[start + offset + i] ^ keystream[i]);
            }

            start += length;
        }

        return output;
    }
}
