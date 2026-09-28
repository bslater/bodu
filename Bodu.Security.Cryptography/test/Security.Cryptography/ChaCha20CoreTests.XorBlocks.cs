// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.XorBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that each kernel combines every run length from none to forty blocks, and longer runs around the
    /// widest kernel's width, with the keystream the block function produces one block at a time.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void XorBlocks_WhenRunLengthVaries_ForEachKernel_ShouldMatchBlockByBlock(string kernel)
    {
        ChaCha20Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x0C4A_0002);

        foreach (int blocks in Enumerable.Range(0, 41).Concat([47, 48, 49, 64, 100]))
        {
            uint[] state = NextState(random);
            uint counter = (uint)random.NextInt64(0, 1L << 32);
            byte[] input = NextBytes(random, blocks * ChaCha20Core.BlockBytes);
            byte[] actual = new byte[input.Length];

            ChaCha20Core.XorBlocks(kind, state, counter, input, actual);

            CollectionAssert.AreEqual(XorBlockByBlock(state, counter, input), actual, $"{blocks} blocks");
        }
    }

    /// <summary>
    /// Verifies that each kernel counts on from the last counter to zero, modulo 2^32, wherever the wrap falls within a
    /// run.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void XorBlocks_ForEachKernel_WhenCounterWraps_ShouldCountOnModulo2Pow32(string kernel)
    {
        ChaCha20Core.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x0C4A_0003);

        for (uint beforeWrap = 0; beforeWrap < 20; beforeWrap++)
        {
            uint[] state = NextState(random);
            uint counter = uint.MaxValue - beforeWrap;
            byte[] input = NextBytes(random, 40 * ChaCha20Core.BlockBytes);
            byte[] actual = new byte[input.Length];

            ChaCha20Core.XorBlocks(kind, state, counter, input, actual);

            CollectionAssert.AreEqual(XorBlockByBlock(state, counter, input), actual, $"counter {counter:X8}");
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
        var random = new Random(0x0C4A_0004);
        uint[] state = NextState(random);
        byte[] input = NextBytes(random, 45 * ChaCha20Core.BlockBytes);
        byte[] separate = new byte[input.Length];
        byte[] inPlace = (byte[])input.Clone();

        ChaCha20Core.XorBlocks(kind, state, 5, input, separate);
        ChaCha20Core.XorBlocks(kind, state, 5, inPlace, inPlace);

        CollectionAssert.AreEqual(separate, inPlace);
    }

    /// <summary>
    /// Verifies that the kernel dispatch selects combines input with the keystream the block function produces one
    /// block at a time.
    /// </summary>
    [TestMethod]
    public void XorBlocks_WhenKernelIsAuto_ShouldMatchBlockByBlock()
    {
        var random = new Random(0x0C4A_0005);
        uint[] state = NextState(random);
        byte[] input = NextBytes(random, 77 * ChaCha20Core.BlockBytes);
        byte[] actual = new byte[input.Length];

        ChaCha20Core.XorBlocks(state, 3, input, actual);

        CollectionAssert.AreEqual(XorBlockByBlock(state, 3, input), actual);
    }

    /// <summary>
    /// Verifies that the kernels leave the output beyond the input untouched.
    /// </summary>
    [TestMethod]
    public void XorBlocks_WhenOutputIsLongerThanInput_ShouldWriteOnlyTheInputLength()
    {
        var random = new Random(0x0C4A_0006);
        uint[] state = NextState(random);
        byte[] input = NextBytes(random, 19 * ChaCha20Core.BlockBytes);
        byte[] output = new byte[input.Length + 40];
        Array.Fill(output, (byte)0xA5);

        ChaCha20Core.XorBlocks(state, 0, input, output);

        CollectionAssert.AreEqual(XorBlockByBlock(state, 0, input), output[..input.Length]);
        Assert.IsTrue(output.AsSpan(input.Length).IndexOfAnyExcept((byte)0xA5) < 0, "XorBlocks must not write past the input length.");
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
        uint[] state = new uint[ChaCha20Core.StateWords];
        byte[] input = new byte[length];
        byte[] output = new byte[length];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ChaCha20Core.XorBlocks(state, 0, input, output);
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
        uint[] state = new uint[ChaCha20Core.StateWords];
        byte[] input = new byte[2 * ChaCha20Core.BlockBytes];
        byte[] output = new byte[input.Length - 1];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.XorBlocks(state, 0, input, output);
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
        uint[] state = new uint[ChaCha20Core.StateWords - 1];
        byte[] input = new byte[ChaCha20Core.BlockBytes];
        byte[] output = new byte[ChaCha20Core.BlockBytes];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ChaCha20Core.XorBlocks(state, 0, input, output);
        });

        Assert.AreEqual("state", ex.ParamName);
    }
}
