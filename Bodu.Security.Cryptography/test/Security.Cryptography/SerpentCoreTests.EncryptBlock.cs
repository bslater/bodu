// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.EncryptBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that encrypting a block with the circuits matches the table-driven reference, for seeded keys of every
    /// Serpent key size and seeded blocks.
    /// </summary>
    /// <param name="keyBytes">The key length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(24)]
    [DataRow(32)]
    public void EncryptBlock_WhenKeysAndBlocksAreSeededRandom_ShouldMatchTheReference(int keyBytes)
    {
        var random = new Random(0x5E4F_1001 + keyBytes);

        for (int i = 0; i < 64; i++)
        {
            uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, keyBytes));
            byte[] block = NextBytes(random, SerpentCore.BlockBytes);
            byte[] actual = new byte[SerpentCore.BlockBytes];

            SerpentCore.EncryptBlock(roundKeys, block, actual);

            CollectionAssert.AreEqual(SerpentReference.Encrypt(roundKeys, block), actual, $"case {i}");
        }
    }

    /// <summary>
    /// Verifies that encrypting in place, with the output the input itself, matches encrypting into a separate block.
    /// </summary>
    [TestMethod]
    public void EncryptBlock_WhenOutputIsTheInput_ShouldMatchSeparateOutput()
    {
        var random = new Random(0x5E4F_1002);
        uint[] roundKeys = SerpentReference.ExpandKey(NextBytes(random, 32));
        byte[] block = NextBytes(random, SerpentCore.BlockBytes);
        byte[] expected = new byte[SerpentCore.BlockBytes];
        SerpentCore.EncryptBlock(roundKeys, block, expected);

        SerpentCore.EncryptBlock(roundKeys, block, block);

        CollectionAssert.AreEqual(expected, block);
    }

    /// <summary>
    /// Verifies that fewer than 132 round-key words are rejected with <see cref="ArgumentOutOfRangeException" /> naming
    /// the round keys.
    /// </summary>
    [TestMethod]
    public void EncryptBlock_WhenRoundKeysAreShort_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.EncryptBlock(new uint[SerpentCore.RoundKeyWords - 1], new byte[16], new byte[16]);
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
    public void EncryptBlock_WhenSpanIsShorterThanABlock_ShouldThrowArgumentOutOfRangeException(int inputBytes, int outputBytes, string paramName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.EncryptBlock(new uint[SerpentCore.RoundKeyWords], new byte[inputBytes], new byte[outputBytes]);
        });

        Assert.AreEqual(paramName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that EncryptBlock forbids inlining, so it is compiled on its own rather than into its callers.
    /// </summary>
    /// <remarks>
    /// Under dynamic PGO a caller that inlined it ran out of inlining budget and left the S-box circuits as calls:
    /// Serpent-128 ran at less than half its speed.
    /// </remarks>
    [TestMethod]
    public void EncryptBlock_ShouldForbidInliningIntoItsCallers()
    {
        MethodInfo method = typeof(SerpentCore).GetMethod("EncryptBlock", BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsTrue(method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining));
    }
}
