// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.DecryptBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that each kernel decrypts every run length from none to forty blocks, and a few past the widest run,
    /// exactly as decrypting each block on its own does, and recovers what it encrypted.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void DecryptBlocks_WhenRunLengthVaries_ForEachKernel_ShouldMatchDecryptBlockPerBlock(string kernel)
    {
        SerpentCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5E4F_6001);

        foreach (int blocks in Enumerable.Range(0, 41).Concat([47, 48, 49, 64, 100]))
        {
            uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
            byte[] input = NextBytes(random, blocks * SerpentCore.BlockBytes);
            byte[] actual = new byte[input.Length];
            byte[] recovered = new byte[input.Length];

            SerpentCore.DecryptBlocks(kind, roundKeys, input, actual);
            SerpentCore.DecryptBlocks(kind, roundKeys, EncryptPerBlock(roundKeys, input), recovered);

            CollectionAssert.AreEqual(DecryptPerBlock(roundKeys, input), actual, $"{blocks} blocks");
            CollectionAssert.AreEqual(input, recovered, $"{blocks} blocks, round trip");
        }
    }

    /// <summary>
    /// Verifies that each kernel decrypts in place, with the output the input itself, as it decrypts into a separate
    /// buffer.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    public void DecryptBlocks_WhenOutputIsTheInput_ShouldMatchSeparateOutput(string kernel)
    {
        SerpentCore.KernelKind kind = ParseSupportedKernel(kernel);
        var random = new Random(0x5E4F_6002);
        uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
        byte[] buffer = NextBytes(random, 29 * SerpentCore.BlockBytes);
        byte[] expected = DecryptPerBlock(roundKeys, buffer);

        SerpentCore.DecryptBlocks(kind, roundKeys, buffer, buffer);

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
    public void DecryptBlocks_WhenInputIsNotWholeBlocks_ShouldThrowArgumentException(int length)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            SerpentCore.DecryptBlocks(new uint[SerpentCore.RoundKeyWords], new byte[length], new byte[length]);
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
    public void DecryptBlocks_WhenArgumentIsShort_ShouldThrowArgumentOutOfRangeException(int keyWords, int outputBytes, string paramName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.DecryptBlocks(new uint[keyWords], new byte[32], new byte[outputBytes]);
        });

        Assert.AreEqual(paramName, ex.ParamName);
    }
}
