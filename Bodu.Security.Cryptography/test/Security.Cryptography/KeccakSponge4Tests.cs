// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakSponge4Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="KeccakSponge4" />, grouped into member-named partial files. Each of the four sponges is held,
/// through every four-way kernel, to the stream a <see cref="KeccakSponge" /> of the same function squeezes for its
/// message.
/// </summary>
[TestClass]
public sealed partial class KeccakSponge4Tests
{
    /// <summary>
    /// Creates four sponges of the SHAKE function with the specified rate, over the specified kernel.
    /// </summary>
    /// <param name="rate">The rate: 168 for SHAKE128, 136 for SHAKE256.</param>
    /// <param name="kernel">The kernel.</param>
    /// <returns>The four sponges.</returns>
    private static KeccakSponge4 Create(int rate, KeccakPermutation.KernelKind kernel) =>
        rate == KeccakSponge.Shake128RateBytes ? KeccakSponge4.CreateShake128(kernel) : KeccakSponge4.CreateShake256(kernel);

    /// <summary>
    /// Returns the stream one scalar sponge of the SHAKE function with the specified rate squeezes for a message.
    /// </summary>
    /// <param name="rate">The rate: 168 for SHAKE128, 136 for SHAKE256.</param>
    /// <param name="message">The message.</param>
    /// <param name="length">The number of bytes to squeeze.</param>
    /// <returns>The stream.</returns>
    private static byte[] ScalarStream(int rate, byte[] message, int length)
    {
        byte[] stream = new byte[length];
        if (rate == KeccakSponge.Shake128RateBytes)
            KeccakSponge.Shake128(message, stream);
        else
            KeccakSponge.Shake256(message, stream);

        return stream;
    }

    /// <summary>
    /// Returns four distinct seeded messages of one length.
    /// </summary>
    /// <param name="random">The source of the bytes.</param>
    /// <param name="length">The length of each message.</param>
    /// <returns>The four messages.</returns>
    private static byte[][] Messages(Random random, int length)
    {
        byte[][] messages = new byte[KeccakSponge4.Ways][];
        for (int lane = 0; lane < messages.Length; lane++)
        {
            messages[lane] = new byte[length];
            random.NextBytes(messages[lane]);
        }

        return messages;
    }

    /// <summary>
    /// Parses a four-way kernel's name, reporting the test inconclusive when the processor cannot run the kernel.
    /// </summary>
    /// <param name="name">The kernel's name.</param>
    /// <returns>The kernel.</returns>
    private static KeccakPermutation.KernelKind ParseSupportedKernel(string name)
    {
        KeccakPermutation.KernelKind kernel = Enum.Parse<KeccakPermutation.KernelKind>(name);
        if (!KeccakPermutation.IsSupported(kernel))
            Assert.Inconclusive($"The {name} kernel cannot run on this processor.");

        return kernel;
    }
}
