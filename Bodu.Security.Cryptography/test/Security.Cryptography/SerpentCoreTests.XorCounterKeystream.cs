// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.XorCounterKeystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that each kernel combines every length from none to forty blocks, with no partial block and with partial
    /// blocks of one, seven and fifteen bytes, and a few runs past the widest group, with the keystream of successive
    /// counter blocks exactly as encrypting each counter block on its own does, and advances the counter one block for
    /// every whole or partial block.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void XorCounterKeystream_WhenLengthVaries_ForEachKernel_ShouldMatchEncryptBlockPerCounterBlock(string kernel)
    {
        SerpentCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5E4F_C001);

        foreach (int blocks in Enumerable.Range(0, 41).Concat([47, 48, 49, 64, 100]))
        {
            foreach (int partial in new[] { 0, 1, 7, 15 })
            {
                uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
                UInt128 counter = NextCounter(random);
                byte[] input = NextBytes(random, (blocks * SerpentCore.BlockBytes) + partial);

                AssertXorCounterKeystreamMatchesReference(kind, roundKeys, counter, input, $"{blocks} blocks and {partial} bytes");
            }
        }
    }

    /// <summary>
    /// Verifies that each kernel carries the counter out of its least significant 32, 64 and 96 bits, and wraps it from
    /// its greatest value to zero, as encrypting each counter block on its own does, wherever the carry falls in a group
    /// of eight or four blocks.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <remarks>
    /// The bits above each carry are seeded, so a carry that fails to reach them, or reaches the wrong word, changes the
    /// counter block.
    /// </remarks>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void XorCounterKeystream_WhenCounterCarriesBetweenWords_ForEachKernel_ShouldMatchEncryptBlockPerCounterBlock(string kernel)
    {
        SerpentCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5E4F_C002);
        uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));

        foreach (int carryBit in new[] { 32, 64, 96, 128 })
        {
            // Every bit below the carry is set, bar those the distance subtracts, so the carry falls that many blocks in.
            UInt128 upper = carryBit == 128 ? UInt128.Zero : NextCounter(random) << carryBit;
            UInt128 below = carryBit == 128 ? UInt128.MaxValue : (UInt128.One << carryBit) - 1;

            for (int distance = 1; distance <= 17; distance++)
            {
                UInt128 counter = upper + below - (UInt128)(distance - 1);
                byte[] input = NextBytes(random, (29 * SerpentCore.BlockBytes) + 5);

                AssertXorCounterKeystreamMatchesReference(kind, roundKeys, counter, input, $"carry out of {carryBit} bits after {distance} blocks");
            }
        }
    }

    /// <summary>
    /// Verifies that each kernel combines the keystream in place, with the output the input itself, as it does into a
    /// separate buffer.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void XorCounterKeystream_WhenOutputIsTheInput_ForEachKernel_ShouldMatchSeparateOutput(string kernel)
    {
        SerpentCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5E4F_C003);
        uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
        UInt128 counter = NextCounter(random);
        byte[] buffer = NextBytes(random, (29 * SerpentCore.BlockBytes) + 9);
        byte[] expected = ReferenceCounterKeystream(roundKeys, counter, buffer);

        ulong high = (ulong)(counter >> 64);
        ulong low = (ulong)counter;
        SerpentCore.XorCounterKeystream(kind, roundKeys, ref high, ref low, buffer, buffer);

        CollectionAssert.AreEqual(expected, buffer);
    }

    /// <summary>
    /// Verifies that each kernel writes nothing past the input when the output is longer.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void XorCounterKeystream_WhenOutputIsLongerThanInput_ForEachKernel_ShouldLeaveTheRestUntouched(string kernel)
    {
        SerpentCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5E4F_C004);
        uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
        UInt128 counter = NextCounter(random);
        byte[] input = NextBytes(random, (13 * SerpentCore.BlockBytes) + 3);
        byte[] output = new byte[input.Length + 64];
        output.AsSpan(input.Length).Fill(0xA5);

        ulong high = (ulong)(counter >> 64);
        ulong low = (ulong)counter;
        SerpentCore.XorCounterKeystream(kind, roundKeys, ref high, ref low, input, output);

        CollectionAssert.AreEqual(ReferenceCounterKeystream(roundKeys, counter, input), output[..input.Length]);
        Assert.IsTrue(output.AsSpan(input.Length).IndexOfAnyExcept((byte)0xA5) < 0, "The bytes past the input changed.");
    }

    /// <summary>
    /// Verifies that an output shorter than the input, or fewer than 132 round-key words, is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> naming it.
    /// </summary>
    /// <param name="keyWords">The number of round-key words.</param>
    /// <param name="outputBytes">The output length, for a 20-byte input.</param>
    /// <param name="paramName">The expected parameter name.</param>
    [TestMethod]
    [DataRow(131, 20, "roundKeys")]
    [DataRow(132, 19, "output")]
    public void XorCounterKeystream_WhenArgumentIsShort_ShouldThrowArgumentOutOfRangeException(int keyWords, int outputBytes, string paramName)
    {
        ulong high = 0;
        ulong low = 0;

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.XorCounterKeystream(new uint[keyWords], ref high, ref low, new byte[20], new byte[outputBytes]);
        });

        Assert.AreEqual(paramName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the scalar counter loop and every vector counter kernel forbid inlining and ask for full
    /// optimization, so each is compiled on its own rather than into the dispatcher.
    /// </summary>
    [TestMethod]
    public void XorCounterKeystream_WhenDeclared_ForEachCounterKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        MethodInfo[] kernels =
        [
            typeof(SerpentCore).GetMethod("XorCounterBlocks", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(SerpentCore.Vector128Kernel<>).GetMethod("XorCounterBlocks", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(SerpentCore.Vector256Kernel<>).GetMethod("XorCounterBlocks", BindingFlags.NonPublic | BindingFlags.Static)!,
        ];

        foreach (MethodInfo kernel in kernels)
        {
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), $"{kernel.DeclaringType!.Name}.{kernel.Name} inlining");
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveOptimization), $"{kernel.DeclaringType!.Name}.{kernel.Name} optimization");
        }
    }

    /// <summary>
    /// Runs the counter keystream through a kernel and asserts that it matches encrypting each counter block on its own,
    /// and that the counter advanced one block for every whole or partial block, modulo <c>2¹²⁸</c>.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="roundKeys">The round keys.</param>
    /// <param name="counter">The first counter block's value.</param>
    /// <param name="input">The input.</param>
    /// <param name="scenario">The scenario, for failure messages.</param>
    private static void AssertXorCounterKeystreamMatchesReference(SerpentCore.KernelKind kernel, uint[] roundKeys, UInt128 counter, byte[] input, string scenario)
    {
        ulong high = (ulong)(counter >> 64);
        ulong low = (ulong)counter;
        byte[] actual = new byte[input.Length];

        SerpentCore.XorCounterKeystream(kernel, roundKeys, ref high, ref low, input, actual);

        CollectionAssert.AreEqual(ReferenceCounterKeystream(roundKeys, counter, input), actual, scenario);
        UInt128 advanced = counter + (UInt128)((input.Length + SerpentCore.BlockBytes - 1) / SerpentCore.BlockBytes);
        Assert.AreEqual(advanced, new UInt128(high, low), $"{scenario}: the counter");
    }

    /// <summary>
    /// Returns a seeded random 128-bit counter.
    /// </summary>
    /// <param name="random">The source of the bits.</param>
    /// <returns>The counter.</returns>
    private static UInt128 NextCounter(Random random) =>
        new((ulong)random.NextInt64(long.MinValue, long.MaxValue), (ulong)random.NextInt64(long.MinValue, long.MaxValue));

    /// <summary>
    /// Computes counter-mode output one block at a time: each counter block, the big-endian form of the counter, is
    /// encrypted on its own and combined with a block of input, and the counter increases by one per whole or partial
    /// block, modulo <c>2¹²⁸</c>.
    /// </summary>
    /// <param name="roundKeys">The round keys.</param>
    /// <param name="counter">The first counter block's value.</param>
    /// <param name="input">The input.</param>
    /// <returns>The output.</returns>
    private static byte[] ReferenceCounterKeystream(uint[] roundKeys, UInt128 counter, byte[] input)
    {
        byte[] output = new byte[input.Length];
        byte[] block = new byte[SerpentCore.BlockBytes];
        byte[] keystream = new byte[SerpentCore.BlockBytes];

        for (int offset = 0; offset < input.Length; offset += SerpentCore.BlockBytes, counter++)
        {
            BinaryPrimitives.WriteUInt128BigEndian(block, counter);
            SerpentCore.EncryptBlock(roundKeys, block, keystream);

            for (int i = 0; i < Math.Min(SerpentCore.BlockBytes, input.Length - offset); i++)
                output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
        }

        return output;
    }
}
