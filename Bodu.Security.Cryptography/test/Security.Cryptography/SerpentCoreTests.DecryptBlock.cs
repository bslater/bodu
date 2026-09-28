// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.DecryptBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that decrypting a block with the circuits matches the table-driven reference and recovers the block
    /// the reference encrypted, for seeded keys of every Serpent key size and seeded blocks.
    /// </summary>
    /// <param name="keyBytes">The key length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void DecryptBlock_WhenKeysAndBlocksAreSeededRandom_ShouldMatchTheReference(int keyBytes)
    {
        var random = new Random(0x5E4F_2001 + keyBytes);

        for (int i = 0; i < 64; i++)
        {
            uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, keyBytes));
            byte[] block = NextBytes(random, SerpentCore.BlockBytes);
            byte[] actual = new byte[SerpentCore.BlockBytes];
            byte[] recovered = new byte[SerpentCore.BlockBytes];

            SerpentCore.DecryptBlock(roundKeys, block, actual);
            SerpentCore.DecryptBlock(roundKeys, SerpentReference.Encrypt(roundKeys, block), recovered);

            CollectionAssert.AreEqual(SerpentReference.Decrypt(roundKeys, block), actual, $"case {i}");
            CollectionAssert.AreEqual(block, recovered, $"case {i}, round trip");
        }
    }

    /// <summary>
    /// Verifies that decrypting in place, with the output the input itself, matches decrypting into a separate block.
    /// </summary>
    [TestMethod]
    public void DecryptBlock_WhenOutputIsTheInput_ShouldMatchSeparateOutput()
    {
        var random = new Random(0x5E4F_2002);
        uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
        byte[] block = NextBytes(random, SerpentCore.BlockBytes);
        byte[] expected = new byte[SerpentCore.BlockBytes];
        SerpentCore.DecryptBlock(roundKeys, block, expected);

        SerpentCore.DecryptBlock(roundKeys, block, block);

        CollectionAssert.AreEqual(expected, block);
    }

    /// <summary>
    /// Verifies that fewer than 132 round-key words are rejected with <see cref="ArgumentOutOfRangeException" /> naming
    /// the round keys.
    /// </summary>
    [TestMethod]
    public void DecryptBlock_WhenRoundKeysAreShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.DecryptBlock(new uint[SerpentCore.RoundKeyWords - 1], new byte[16], new byte[16]);
        });

        Assert.AreEqual("roundKeys", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an input or output shorter than a block is rejected with
    /// <see cref="ArgumentOutOfRangeException" /> naming it.
    /// </summary>
    /// <param name="inputBytes">The input length.</param>
    /// <param name="outputBytes">The output length.</param>
    /// <param name="paramName">The expected parameter name.</param>
    [TestMethod]
    [DataRow(15, 16, "input")]
    [DataRow(16, 15, "output")]
    public void DecryptBlock_WhenSpanIsShorterThanABlock_ShouldThrowArgumentOutOfRangeException(int inputBytes, int outputBytes, string paramName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.DecryptBlock(new uint[SerpentCore.RoundKeyWords], new byte[inputBytes], new byte[outputBytes]);
        });

        Assert.AreEqual(paramName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that DecryptBlock forbids inlining, so it is compiled on its own rather than into its callers.
    /// </summary>
    /// <remarks>
    /// Under dynamic PGO a caller that inlined it ran out of inlining budget and left the S-box circuits as calls:
    /// Serpent-128 ran at less than half its speed.
    /// </remarks>
    [TestMethod]
    public void DecryptBlock_ShouldForbidInliningIntoItsCallers()
    {
        MethodInfo method = typeof(SerpentCore).GetMethod("DecryptBlock", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsTrue(method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining));
    }
}
