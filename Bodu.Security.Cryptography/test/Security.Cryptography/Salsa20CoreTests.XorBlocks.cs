// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20CoreTests.XorBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Salsa20CoreTests
{
    /// <summary>
    /// Verifies that each kernel combines every run length from none to forty blocks, and longer runs around the
    /// widest kernel's width, with the keystream the core function produces one block at a time.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void XorBlocks_ForEachKernel_ShouldMatchBlockByBlock(string kernel)
    {
        ChaCha20Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5A15_0002);

        foreach (int blocks in Enumerable.Range(0, 41).Concat([47, 48, 49, 64, 100]))
        {
            uint[] state = NextState(random);
            ulong counter = (ulong)random.NextInt64();
            byte[] input = NextBytes(random, blocks * Salsa20Core.BlockBytes);
            byte[] actual = new byte[input.Length];

            Salsa20Core.XorBlocks(kind, state, counter, input, actual);

            CollectionAssert.AreEqual(XorBlockByBlock(state, counter, input), actual, $"{blocks} blocks");
        }
    }

    /// <summary>
    /// Verifies that each kernel carries the counter's low word into its high word wherever the carry falls within a
    /// run, and wraps the whole counter to zero, modulo 2^64.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void XorBlocks_ForEachKernel_WhenCounterCarriesOrWraps_ShouldCountOnModulo2Pow64(string kernel)
    {
        ChaCha20Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5A15_0003);

        foreach (ulong top in new[] { 0x0000_0000_FFFF_FFFFUL, 0x1234_5678_FFFF_FFFFUL, ulong.MaxValue })
        {
            for (uint beforeCarry = 0; beforeCarry < 20; beforeCarry++)
            {
                uint[] state = NextState(random);
                ulong counter = top - beforeCarry;
                byte[] input = NextBytes(random, 40 * Salsa20Core.BlockBytes);
                byte[] actual = new byte[input.Length];

                Salsa20Core.XorBlocks(kind, state, counter, input, actual);

                CollectionAssert.AreEqual(XorBlockByBlock(state, counter, input), actual, $"counter {counter:X16}");
            }
        }
    }

    /// <summary>
    /// Verifies that each kernel writes the same output in place, with the destination the input itself, as into a
    /// separate buffer.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void XorBlocks_ForEachKernel_WhenOutputIsTheInput_ShouldMatchSeparateOutput(string kernel)
    {
        ChaCha20Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5A15_0004);
        uint[] state = NextState(random);
        byte[] input = NextBytes(random, 45 * Salsa20Core.BlockBytes);
        byte[] separate = new byte[input.Length];
        byte[] inPlace = (byte[])input.Clone();

        Salsa20Core.XorBlocks(kind, state, 5, input, separate);
        Salsa20Core.XorBlocks(kind, state, 5, inPlace, inPlace);

        CollectionAssert.AreEqual(separate, inPlace);
    }

    /// <summary>
    /// Verifies that the kernel dispatch selects combines input with the keystream the core function produces one block
    /// at a time.
    /// </summary>
    [TestMethod]
    public void XorBlocks_WhenKernelIsAuto_ShouldMatchBlockByBlock()
    {
        var random = new Random(0x5A15_0005);
        uint[] state = NextState(random);
        byte[] input = NextBytes(random, 77 * Salsa20Core.BlockBytes);
        byte[] actual = new byte[input.Length];

        Salsa20Core.XorBlocks(state, 3, input, actual);

        CollectionAssert.AreEqual(XorBlockByBlock(state, 3, input), actual);
    }

    /// <summary>
    /// Verifies that input that is not a whole number of blocks is rejected with <see cref="ArgumentException" />
    /// naming the parameter.
    /// </summary>
    /// <param name="length">The rejected input length.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(63)]
    [DataRow(65)]
    public void XorBlocks_WhenInputIsNotWholeBlocks_ShouldThrowArgumentException(int length)
    {
        uint[] state = new uint[Salsa20Core.StateWords];
        byte[] input = new byte[length];
        byte[] output = new byte[length];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Salsa20Core.XorBlocks(state, 0, input, output);
        });

        Assert.AreEqual("input", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an output shorter than the input is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    [TestMethod]
    public void XorBlocks_WhenOutputIsShorterThanInput_ShouldThrowArgumentOutOfRangeException()
    {
        uint[] state = new uint[Salsa20Core.StateWords];
        byte[] input = new byte[2 * Salsa20Core.BlockBytes];
        byte[] output = new byte[input.Length - 1];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.XorBlocks(state, 0, input, output);
        });

        Assert.AreEqual("output", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a state shorter than sixteen words is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming the parameter.
    /// </summary>
    [TestMethod]
    public void XorBlocks_WhenStateHoldsFewerThanSixteenWords_ShouldThrowArgumentOutOfRangeException()
    {
        uint[] state = new uint[Salsa20Core.StateWords - 1];
        byte[] input = new byte[Salsa20Core.BlockBytes];
        byte[] output = new byte[Salsa20Core.BlockBytes];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            Salsa20Core.XorBlocks(state, 0, input, output);
        });

        Assert.AreEqual("state", ex.ParamName);
    }
}
