// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.CostFor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that a run costs the steps each kernel divides it into: the widest groups that fit first, then the
    /// narrower, then the blocks left over one at a time.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="blocks">The number of blocks in the run.</param>
    /// <param name="expected">The expected cost, in halves of a block function call.</param>
    [TestMethod]
    [DataRow("Scalar", 0, 0)]
    [DataRow("Scalar", 5, 10)]
    [DataRow("Ssse3", 7, 10)]
    [DataRow("Ssse3", 8, 8)]
    [DataRow("AdvSimd", 9, 10)]
    [DataRow("Avx2", 13, 10)]
    [DataRow("Avx512", 12, 4)]
    [DataRow("Avx512", 16, 4)]
    [DataRow("Avx512Wide", 16, 3)]
    [DataRow("Avx512Wide", 28, 7)]
    [DataRow("Avx512Wide", 29, 9)]
    public void CostFor_WhenGivenARun_ForEachKernel_ShouldSumTheStepsItIsDividedInto(string kernel, int blocks, int expected)
    {
        Assert.AreEqual(expected, ChaCha20Core.CostFor(Enum.Parse<ChaCha20Core.KernelKind>(kernel), blocks));
    }

    /// <summary>
    /// Verifies that on the block function every run costs one block function call a block, however long.
    /// </summary>
    [TestMethod]
    public void CostFor_WhenKernelIsScalar_ShouldCostEachBlockAlike()
    {
        for (int blocks = 0; blocks <= 64; blocks++)
            Assert.AreEqual(2 * blocks, ChaCha20Core.CostFor(ChaCha20Core.KernelKind.Scalar, blocks), $"{blocks} blocks");
    }
}
