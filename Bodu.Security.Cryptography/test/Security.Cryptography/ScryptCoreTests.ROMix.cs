// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCoreTests.ROMix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ScryptCoreTests
{
    /// <summary>
    /// Verifies that ROMix reproduces RFC 7914, Section 10's scryptROMix vector, with <c>r = 1</c> and <c>N = 16</c>.
    /// </summary>
    [TestMethod]
    public void ROMix_WhenGivenRfc7914Vector_ShouldMatchExpectedOutput()
    {
        uint[] block = ToWords(RomixInputHex);

        ScryptCore.ROMix(block, 16, 1, new uint[16 * 32], new uint[32]);

        Assert.AreEqual(RomixOutputHex, ToHex(block));
    }

    /// <summary>
    /// Verifies that ROMix reproduces RFC 7914, Section 10's vector when <c>V</c> and the scratch start filled with
    /// garbage, as a buffer fresh from native memory may be: every unit is written before it is read.
    /// </summary>
    [TestMethod]
    public void ROMix_WhenChainAndScratchStartWithGarbage_ShouldMatchRfc7914Vector()
    {
        uint[] block = ToWords(RomixInputHex);

        ScryptCore.ROMix(block, 16, 1, RandomWords(16 * 32, seed: 1), RandomWords(32, seed: 2));

        Assert.AreEqual(RomixOutputHex, ToHex(block));
    }

    /// <summary>
    /// Verifies that, across block sizes and costs, ROMix gives the same result whether <c>V</c> and the scratch start
    /// zeroed or filled with garbage.
    /// </summary>
    /// <param name="costN">The cost parameter <c>N</c>.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    [TestMethod]
    [DataRow(2, 1)]
    [DataRow(4, 2)]
    [DataRow(64, 3)]
    [DataRow(256, 8)]
    public void ROMix_WhenChainAndScratchStartWithGarbage_ShouldMatchZeroedRun(int costN, int blockSizeR)
    {
        int unitWords = 32 * blockSizeR;
        uint[] input = RandomWords(unitWords, seed: costN + blockSizeR);
        uint[] expected = (uint[])input.Clone();
        uint[] actual = (uint[])input.Clone();

        ScryptCore.ROMix(expected, costN, blockSizeR, new uint[costN * unitWords], new uint[unitWords]);
        ScryptCore.ROMix(actual, costN, blockSizeR, RandomWords(costN * unitWords, seed: 3), RandomWords(unitWords, seed: 4));

        CollectionAssert.AreEqual(expected, actual);
    }
}
