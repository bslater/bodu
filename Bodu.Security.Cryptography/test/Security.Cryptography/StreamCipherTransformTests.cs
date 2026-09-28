// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherTransformTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="StreamCipherTransform" />, grouped into member-named partial files. Each stream cipher's own
/// suite covers the transform through the public algorithm; these hold the transform's whole-block and carried-keystream
/// paths to keystream drawn one block at a time, however the input is split across calls.
/// </summary>
[TestClass]
public sealed partial class StreamCipherTransformTests
{
    /// <summary>
    /// Creates a keystream engine for the named cipher under a seeded key and nonce.
    /// </summary>
    /// <param name="cipher">The cipher: <c>ChaCha20</c> or <c>Salsa20</c>.</param>
    /// <param name="seed">The seed of the key and nonce.</param>
    /// <param name="initialCounter">The block counter of the first block.</param>
    /// <returns>The engine.</returns>
    private static IStreamCipher CreateEngine(string cipher, int seed, uint initialCounter)
    {
        var random = new Random(seed);
        byte[] key = new byte[32];
        random.NextBytes(key);

        if (cipher == "ChaCha20")
        {
            byte[] nonce = new byte[ChaCha20StreamCipher.NonceSizeBytes];
            random.NextBytes(nonce);
            return new ChaCha20StreamCipher(key, nonce, initialCounter);
        }

        byte[] salsaNonce = new byte[Salsa20StreamCipher.NonceSizeBytes];
        random.NextBytes(salsaNonce);
        return new Salsa20StreamCipher(key, salsaNonce, initialCounter);
    }

    /// <summary>
    /// Combines input with keystream drawn one whole block at a time from an engine, discarding the unused tail of the
    /// last block.
    /// </summary>
    /// <param name="engine">The engine to draw from.</param>
    /// <param name="input">The input.</param>
    /// <returns>The input combined with the keystream.</returns>
    private static byte[] XorBlockByBlock(IStreamCipher engine, byte[] input)
    {
        byte[] output = new byte[input.Length];
        byte[] keystream = new byte[engine.BlockSize];

        for (int offset = 0; offset < input.Length; offset += keystream.Length)
        {
            engine.NextKeystreamBlock(keystream);
            for (int i = 0; i < keystream.Length && offset + i < input.Length; i++)
                output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
        }

        return output;
    }
}
