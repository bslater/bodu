// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.EncryptWideBlock.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that the streamed rounds encrypt seeded blocks of every width, over one, two and the variant's own number
    /// of eight-round passes, under seeded keys and tweaks, to the ciphertext of the replaced 1.1.0 rounds.
    /// </summary>
    /// <param name="words">The number of words in a block.</param>
    /// <param name="rounds">The number of rounds.</param>
    [TestMethod]
    [DataRow(8, 8)]
    [DataRow(8, 16)]
    [DataRow(8, 48)]
    [DataRow(16, 8)]
    [DataRow(16, 64)]
    [DataRow(32, 8)]
    [DataRow(32, 80)]
    public void EncryptStreamedWideBlock_ForEachWidthAndRoundCount_ShouldMatchTheReplacedRounds(int words, int rounds)
    {
        foreach ((string name, SerpentWideReference reference, uint[] roundKeys, byte[][] blocks) in WideBlockCases(words, rounds, 0x5E7E_1E00 + 1))
        {
            byte[] actual = new byte[words * 4];
            for (int b = 0; b < blocks.Length; b++)
            {
                SerpentCore.EncryptStreamedWideBlock(roundKeys, words, rounds, blocks[b], actual);
                CollectionAssert.AreEqual(reference.Encrypt(blocks[b]), actual, $"{name}, block {b}");
            }
        }
    }

    /// <summary>
    /// Verifies that the streamed rounds encrypt a block of every width in place, into the memory that holds it, to the
    /// ciphertext of the replaced 1.1.0 rounds.
    /// </summary>
    /// <param name="words">The number of words in a block.</param>
    [TestMethod]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(32)]
    public void EncryptStreamedWideBlock_WhenOutputIsTheInput_ShouldMatchTheReplacedRounds(int words)
    {
        foreach ((string name, SerpentWideReference reference, uint[] roundKeys, byte[][] blocks) in WideBlockCases(words, 16, 0x5E7E_1E00 + 2))
        {
            for (int b = 0; b < blocks.Length; b++)
            {
                byte[] buffer = (byte[])blocks[b].Clone();
                SerpentCore.EncryptStreamedWideBlock(roundKeys, words, reference.Rounds, buffer, buffer);
                CollectionAssert.AreEqual(reference.Encrypt(blocks[b]), buffer, $"{name}, block {b}");
            }
        }
    }

    /// <summary>
    /// Verifies that the resident rounds encrypt seeded eight-word blocks, over one, two and Serpent-256's number of
    /// eight-round passes, under seeded keys and tweaks, to the ciphertext of the replaced 1.1.0 rounds.
    /// </summary>
    /// <param name="rounds">The number of rounds.</param>
    [TestMethod]
    [DataRow(8)]
    [DataRow(16)]
    [DataRow(48)]
    public void EncryptResidentWideBlock_ForEachRoundCount_ShouldMatchTheReplacedRounds(int rounds)
    {
        foreach ((string name, SerpentWideReference reference, uint[] roundKeys, byte[][] blocks) in WideBlockCases(SerpentCore.ResidentWideBlockWords, rounds, 0x5E7E_1E00 + 3))
        {
            byte[] actual = new byte[SerpentCore.ResidentWideBlockWords * 4];
            for (int b = 0; b < blocks.Length; b++)
            {
                SerpentCore.EncryptResidentWideBlock(roundKeys, rounds, blocks[b], actual);
                CollectionAssert.AreEqual(reference.Encrypt(blocks[b]), actual, $"{name}, block {b}");
            }
        }
    }

    /// <summary>
    /// Verifies that the resident rounds encrypt an eight-word block in place, into the memory that holds it, to the
    /// ciphertext of the replaced 1.1.0 rounds.
    /// </summary>
    [TestMethod]
    public void EncryptResidentWideBlock_WhenOutputIsTheInput_ShouldMatchTheReplacedRounds()
    {
        foreach ((string name, SerpentWideReference reference, uint[] roundKeys, byte[][] blocks) in WideBlockCases(SerpentCore.ResidentWideBlockWords, 48, 0x5E7E_1E00 + 4))
        {
            for (int b = 0; b < blocks.Length; b++)
            {
                byte[] buffer = (byte[])blocks[b].Clone();
                SerpentCore.EncryptResidentWideBlock(roundKeys, reference.Rounds, buffer, buffer);
                CollectionAssert.AreEqual(reference.Encrypt(blocks[b]), buffer, $"{name}, block {b}");
            }
        }
    }

    /// <summary>
    /// Verifies that EncryptWideBlock encrypts blocks of every width, through the resident rounds at eight words and the
    /// streamed ones otherwise, to the ciphertext of the replaced 1.1.0 rounds.
    /// </summary>
    /// <param name="words">The number of words in a block.</param>
    /// <param name="rounds">The variant's number of rounds.</param>
    [TestMethod]
    [DataRow(8, 48)]
    [DataRow(16, 64)]
    [DataRow(32, 80)]
    public void EncryptWideBlock_ForEachWidth_ShouldMatchTheReplacedRounds(int words, int rounds)
    {
        foreach ((string name, SerpentWideReference reference, uint[] roundKeys, byte[][] blocks) in WideBlockCases(words, rounds, 0x5E7E_1E00 + 5))
        {
            byte[] actual = new byte[words * 4];
            for (int b = 0; b < blocks.Length; b++)
            {
                SerpentCore.EncryptWideBlock(roundKeys, words, rounds, blocks[b], actual);
                CollectionAssert.AreEqual(reference.Encrypt(blocks[b]), actual, $"{name}, block {b}");
            }
        }
    }

    /// <summary>
    /// Verifies that a width other than 8, 16 or 32 words is rejected with <see cref="ArgumentOutOfRangeException" />
    /// naming it.
    /// </summary>
    /// <param name="words">The rejected width.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(4)]
    [DataRow(12)]
    [DataRow(24)]
    [DataRow(64)]
    public void EncryptStreamedWideBlock_WhenWordsIsNotEightSixteenOrThirtyTwo_ShouldThrowArgumentOutOfRangeException(int words)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.EncryptStreamedWideBlock(new uint[4096], words, 8, new byte[512], new byte[512]);
        });

        Assert.AreEqual("words", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a round count that is not a positive multiple of 8 is rejected by the streamed rounds with
    /// <see cref="ArgumentOutOfRangeException" /> naming it.
    /// </summary>
    /// <param name="rounds">The rejected round count.</param>
    [TestMethod]
    [DataRow(-8)]
    [DataRow(0)]
    [DataRow(4)]
    [DataRow(12)]
    public void EncryptStreamedWideBlock_WhenRoundsIsNotAPositiveMultipleOfEight_ShouldThrowArgumentOutOfRangeException(int rounds)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.EncryptStreamedWideBlock(new uint[4096], 16, rounds, new byte[64], new byte[64]);
        });

        Assert.AreEqual("rounds", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a round count that is not a positive multiple of 8 is rejected by the resident rounds with
    /// <see cref="ArgumentOutOfRangeException" /> naming it.
    /// </summary>
    /// <param name="rounds">The rejected round count.</param>
    [TestMethod]
    [DataRow(-8)]
    [DataRow(0)]
    [DataRow(4)]
    [DataRow(12)]
    public void EncryptResidentWideBlock_WhenRoundsIsNotAPositiveMultipleOfEight_ShouldThrowArgumentOutOfRangeException(int rounds)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.EncryptResidentWideBlock(new uint[4096], rounds, new byte[32], new byte[32]);
        });

        Assert.AreEqual("rounds", ex.ParamName);
    }

    /// <summary>
    /// Verifies that round keys, an input or an output too short for a 16-word block of 64 rounds is rejected by the
    /// streamed rounds with <see cref="ArgumentOutOfRangeException" /> naming it.
    /// </summary>
    /// <param name="keyWords">The number of round-key words.</param>
    /// <param name="inputBytes">The input length.</param>
    /// <param name="outputBytes">The output length.</param>
    /// <param name="paramName">The expected parameter name.</param>
    [TestMethod]
    [DataRow((65 * 16) - 1, 64, 64, "roundKeys")]
    [DataRow(65 * 16, 63, 64, "input")]
    [DataRow(65 * 16, 64, 63, "output")]
    public void EncryptStreamedWideBlock_WhenABufferIsShort_ShouldThrowArgumentOutOfRangeException(int keyWords, int inputBytes, int outputBytes, string paramName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.EncryptStreamedWideBlock(new uint[keyWords], 16, 64, new byte[inputBytes], new byte[outputBytes]);
        });

        Assert.AreEqual(paramName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that round keys, an input or an output too short for an eight-word block of 48 rounds is rejected by
    /// the resident rounds with <see cref="ArgumentOutOfRangeException" /> naming it.
    /// </summary>
    /// <param name="keyWords">The number of round-key words.</param>
    /// <param name="inputBytes">The input length.</param>
    /// <param name="outputBytes">The output length.</param>
    /// <param name="paramName">The expected parameter name.</param>
    [TestMethod]
    [DataRow((49 * 8) - 1, 32, 32, "roundKeys")]
    [DataRow(49 * 8, 31, 32, "input")]
    [DataRow(49 * 8, 32, 31, "output")]
    public void EncryptResidentWideBlock_WhenABufferIsShort_ShouldThrowArgumentOutOfRangeException(int keyWords, int inputBytes, int outputBytes, string paramName)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            SerpentCore.EncryptResidentWideBlock(new uint[keyWords], 48, new byte[inputBytes], new byte[outputBytes]);
        });

        Assert.AreEqual(paramName, ex.ParamName);
    }

    /// <summary>
    /// Verifies that the streamed and resident rounds forbid inlining and are optimized at once, so each is compiled
    /// on its own rather than into its callers.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <remarks>
    /// Under dynamic PGO a caller that inlined Serpent-128's rounds ran out of inlining budget and left the S-box
    /// circuits as calls: Serpent-128 ran at less than half its speed.
    /// </remarks>
    [TestMethod]
    [DataRow("EncryptStreamedWideBlock")]
    [DataRow("EncryptResidentWideBlock")]
    public void EncryptWideBlock_ForEachPath_ShouldForbidInliningIntoItsCallers(string methodName)
    {
        MethodInfo method = typeof(SerpentCore).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsTrue(method.MethodImplementationFlags.HasFlag(MethodImplAttributes.NoInlining), methodName);
        Assert.IsTrue(method.MethodImplementationFlags.HasFlag(MethodImplAttributes.AggressiveOptimization), methodName);
    }
}
