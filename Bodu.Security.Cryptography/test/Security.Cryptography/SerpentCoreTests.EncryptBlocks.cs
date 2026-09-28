// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.EncryptBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that each kernel encrypts every run length from none to forty blocks, and a few past the widest run,
    /// exactly as encrypting each block on its own does.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void EncryptBlocks_WhenRunLengthVaries_ForEachKernel_ShouldMatchEncryptBlockPerBlock(string kernel)
    {
        SerpentCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5E4F_5001);

        foreach (int blocks in Enumerable.Range(0, 41).Concat([47, 48, 49, 64, 100]))
        {
            uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
            byte[] input = NextBytes(random, blocks * SerpentCore.BlockBytes);
            byte[] actual = new byte[input.Length];

            SerpentCore.EncryptBlocks(kind, roundKeys, input, actual);

            CollectionAssert.AreEqual(EncryptPerBlock(roundKeys, input), actual, $"{blocks} blocks");
        }
    }

    /// <summary>
    /// Verifies that each kernel encrypts in place, with the output the input itself, as it encrypts into a separate
    /// buffer.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void EncryptBlocks_WhenOutputIsTheInput_ShouldMatchSeparateOutput(string kernel)
    {
        SerpentCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5E4F_5002);
        uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
        byte[] buffer = NextBytes(random, 29 * SerpentCore.BlockBytes);
        byte[] expected = EncryptPerBlock(roundKeys, buffer);

        SerpentCore.EncryptBlocks(kind, roundKeys, buffer, buffer);

        CollectionAssert.AreEqual(expected, buffer);
    }

    /// <summary>
    /// Verifies that input that is not a whole number of blocks is rejected with <see cref="ArgumentException" />
    /// naming the input.
    /// </summary>
    /// <param name="length">The rejected input length.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(17)]
    public void EncryptBlocks_WhenInputIsNotWholeBlocks_ShouldThrowArgumentException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            SerpentCore.EncryptBlocks(new uint[SerpentCore.RoundKeyWords], new byte[length], new byte[length]);
        });

        Assert.AreEqual("input", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an output shorter than the input, or fewer than 132 round-key words, is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> naming it.
    /// </summary>
    /// <param name="keyWords">The number of round-key words.</param>
    /// <param name="outputBytes">The output length, for a 32-byte input.</param>
    /// <param name="paramName">The expected parameter name.</param>
    [TestMethod]
    [DataRow(131, 32, "roundKeys")]
    [DataRow(132, 31, "output")]
    public void EncryptBlocks_WhenArgumentIsShort_ShouldThrowArgumentOutOfRangeException(int keyWords, int outputBytes, string paramName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.EncryptBlocks(new uint[keyWords], new byte[32], new byte[outputBytes]);
        });

        Assert.AreEqual(paramName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that every vector kernel forbids inlining, so each is compiled on its own rather than into the
    /// dispatcher.
    /// </summary>
    /// <remarks>
    /// As with the single-block entry points, a dispatcher that inlined a kernel under dynamic PGO would run out of
    /// inlining budget and leave the S-box circuits as calls.
    /// </remarks>
    [TestMethod]
    public void EncryptBlocks_WhenDeclared_ForEachVectorKernel_ShouldForbidInliningIntoTheDispatcher()
    {
        MethodInfo[] kernels =
        [
            typeof(SerpentCore.Vector128Kernel<>).GetMethod("EncryptBlocks", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(SerpentCore.Vector128Kernel<>).GetMethod("DecryptBlocks", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(SerpentCore.Vector256Kernel<>).GetMethod("EncryptBlocks", BindingFlags.NonPublic | BindingFlags.Static)!,
            typeof(SerpentCore.Vector256Kernel<>).GetMethod("DecryptBlocks", BindingFlags.NonPublic | BindingFlags.Static)!,
        ];

        foreach (MethodInfo kernel in kernels)
            Assert.IsTrue(kernel.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), $"{kernel.DeclaringType!.Name}.{kernel.Name}");
    }
}
