// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="ChaCha20Core" />, the ChaCha20 block function behind <see cref="ChaCha20StreamCipher" />,
/// grouped into member-named partial files. The block function is held to RFC 8439's vectors, and every many-block
/// kernel, driven explicitly whichever one dispatch picks, to the block function one block at a time.
/// </summary>
[TestClass]
public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Combines input with the keystream the way <see cref="ChaCha20Core.XorBlocks(ReadOnlySpan{uint}, uint, ReadOnlySpan{byte}, Span{byte})" />
    /// must: one <see cref="ChaCha20Core.Block" /> per 64 bytes, the counter counting up modulo 2^32.
    /// </summary>
    /// <param name="state">The sixteen-word state.</param>
    /// <param name="counter">The block counter of the first block.</param>
    /// <param name="input">The input, a whole number of blocks.</param>
    /// <returns>The input combined with the keystream.</returns>
    private static byte[] XorBlockByBlock(uint[] state, uint counter, byte[] input)
    {
        byte[] output = new byte[input.Length];
        byte[] keystream = new byte[ChaCha20Core.BlockBytes];

        for (int offset = 0; offset < input.Length; offset += ChaCha20Core.BlockBytes)
        {
            ChaCha20Core.Block(state, counter++, keystream);
            for (int i = 0; i < ChaCha20Core.BlockBytes; i++)
                output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
        }

        return output;
    }

    /// <summary>
    /// Returns sixteen seeded random state words; nothing in the kernels depends on the constant words holding the
    /// constant, so random words exercise every lane of every word.
    /// </summary>
    /// <param name="random">The source of the words.</param>
    /// <returns>The state.</returns>
    private static uint[] NextState(Random random)
    {
        uint[] state = new uint[ChaCha20Core.StateWords];
        for (int i = 0; i < state.Length; i++)
            state[i] = (uint)random.NextInt64(0, 1L << 32);

        return state;
    }

    /// <summary>
    /// Returns a new array of seeded random bytes.
    /// </summary>
    /// <param name="random">The source of the bytes.</param>
    /// <param name="length">The number of bytes.</param>
    /// <returns>The bytes.</returns>
    private static byte[] NextBytes(Random random, int length)
    {
        byte[] bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }

    /// <summary>
    /// Parses a kernel name, marking the test inconclusive when the processor cannot run that kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static ChaCha20Core.KernelKind ParseSupportedKernel(string name)
    {
        ChaCha20Core.KernelKind kernel = Enum.Parse<ChaCha20Core.KernelKind>(name);
        if (!ChaCha20Core.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }
}
