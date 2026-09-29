// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.StepCost.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that a step costs what the kernels were measured to take against the block function: the same for four
    /// or eight blocks with AVX-512VL and half as much again for sixteen, and twice as much for four or eight blocks on
    /// the kernels with sixteen vector registers.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="lanes">The number of blocks the step computes.</param>
    /// <param name="expected">The expected cost, in halves of a block function call.</param>
    [TestMethod]
    [DataRow("Scalar", 1, 2)]
    [DataRow("Ssse3", 1, 2)]
    [DataRow("Ssse3", 4, 4)]
    [DataRow("AdvSimd", 4, 4)]
    [DataRow("Avx2", 4, 4)]
    [DataRow("Avx2", 8, 4)]
    [DataRow("Avx512", 1, 2)]
    [DataRow("Avx512", 4, 2)]
    [DataRow("Avx512", 8, 2)]
    [DataRow("Avx512Wide", 4, 2)]
    [DataRow("Avx512Wide", 8, 2)]
    [DataRow("Avx512Wide", 16, 3)]
    public void StepCost_WhenGivenLanes_ForEachKernel_ShouldBeTheMeasuredEstimate(string kernel, int lanes, int expected)
    {
        Assert.AreEqual(expected, ChaCha20Core.StepCost(Enum.Parse<ChaCha20Core.KernelKind>(kernel), lanes));
    }
}
