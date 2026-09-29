// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.CheapestRunBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that a run is rounded up to more blocks only where the kernel then takes it for less, and otherwise
    /// kept to the blocks it must cover.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="blocks">The number of blocks the run must cover.</param>
    /// <param name="expected">The expected number of blocks in the run.</param>
    [TestMethod]
    [DataRow("Scalar", 3, 3)]
    [DataRow("Ssse3", 2, 2)]
    [DataRow("Ssse3", 3, 4)]
    [DataRow("Ssse3", 7, 8)]
    [DataRow("AdvSimd", 15, 16)]
    [DataRow("Avx2", 2, 2)]
    [DataRow("Avx2", 3, 4)]
    [DataRow("Avx2", 5, 8)]
    [DataRow("Avx2", 9, 9)]
    [DataRow("Avx2", 11, 12)]
    [DataRow("Avx2", 13, 16)]
    [DataRow("Avx512", 1, 1)]
    [DataRow("Avx512", 2, 4)]
    [DataRow("Avx512", 9, 9)]
    [DataRow("Avx512", 13, 16)]
    [DataRow("Avx512Wide", 9, 16)]
    public void CheapestRunBlocks_WhenGivenBlocks_ForEachKernel_ShouldRoundUpOnlyWhereThatCostsLess(string kernel, int blocks, int expected)
    {
        Assert.AreEqual(expected, Poly1305AeadCore.CheapestRunBlocks(Enum.Parse<ChaCha20Core.KernelKind>(kernel), blocks));
    }

    /// <summary>
    /// Verifies that the run chosen for every count of blocks up to sixteen costs no more than any run from the blocks
    /// it must cover to sixteen, and that every shorter run among them costs more.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void CheapestRunBlocks_WhenGivenAnyBlocks_ForEachKernel_ShouldBeTheShortestOfTheCheapestRuns(string kernel)
    {
        var kind = Enum.Parse<ChaCha20Core.KernelKind>(kernel);

        for (int blocks = 0; blocks <= 16; blocks++)
        {
            int chosen = Poly1305AeadCore.CheapestRunBlocks(kind, blocks);
            int chosenCost = ChaCha20Core.CostFor(kind, chosen);

            Assert.IsTrue(chosen >= blocks && chosen <= Math.Max(blocks, 16), $"{blocks} blocks: chose {chosen}.");

            for (int candidate = blocks; candidate <= 16; candidate++)
            {
                int cost = ChaCha20Core.CostFor(kind, candidate);

                Assert.IsTrue(cost >= chosenCost, $"{blocks} blocks: {candidate} costs {cost}, less than {chosen} at {chosenCost}.");
                Assert.IsTrue(candidate >= chosen || cost > chosenCost, $"{blocks} blocks: {candidate} costs no more than {chosen}.");
            }
        }
    }
}
