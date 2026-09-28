// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20StreamCipherTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Salsa20StreamCipher" />, the Salsa20 keystream engine, grouped into member-named partial files.
/// The engine's counter exhaustion and disposal are covered with the other engines' in
/// <see cref="StreamCipherCounterExhaustionTests" /> and <see cref="StreamCipherEngineDisposeTests" />.
/// </summary>
[TestClass]
public sealed partial class Salsa20StreamCipherTests
{
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
    /// Combines input with successive keystream blocks drawn one at a time from an engine.
    /// </summary>
    /// <param name="engine">The engine to draw from.</param>
    /// <param name="input">The input, a whole number of blocks.</param>
    /// <returns>The input combined with the keystream.</returns>
    private static byte[] XorBlockByBlock(IStreamCipher engine, byte[] input)
    {
        byte[] output = new byte[input.Length];
        byte[] keystream = new byte[engine.BlockSize];

        for (int offset = 0; offset < input.Length; offset += keystream.Length)
        {
            engine.NextKeystreamBlock(keystream);
            for (int i = 0; i < keystream.Length; i++)
                output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
        }

        return output;
    }
}
