// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20CoreTests.LanesFor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class ChaCha20CoreTests
{
    /// <summary>
    /// Verifies that each kernel takes the widest run it offers that fits in the blocks that remain, down to one block
    /// for the scalar block function.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="remaining">The number of blocks left.</param>
    /// <param name="expected">The expected run length.</param>
    [TestMethod]
    [DataRow("Scalar", 100, 1)]
    [DataRow("Ssse3", 3, 1)]
    [DataRow("Ssse3", 4, 4)]
    [DataRow("Ssse3", 100, 4)]
    [DataRow("AdvSimd", 3, 1)]
    [DataRow("AdvSimd", 100, 4)]
    [DataRow("Avx2", 7, 4)]
    [DataRow("Avx2", 8, 8)]
    [DataRow("Avx2", 100, 8)]
    [DataRow("Avx512", 3, 1)]
    [DataRow("Avx512", 15, 8)]
    [DataRow("Avx512", 100, 8)]
    [DataRow("Avx512Wide", 15, 8)]
    [DataRow("Avx512Wide", 16, 16)]
    [DataRow("Avx512Wide", 7, 4)]
    [DataRow("Avx512Wide", 1, 1)]
    public void LanesFor_WhenGivenRemainingBlocks_ForEachKernel_ShouldTakeTheWidestRunThatFits(string kernel, int remaining, int expected)
    {
        Assert.AreEqual(expected, ChaCha20Core.LanesFor(Enum.Parse<ChaCha20Core.KernelKind>(kernel), remaining));
    }
}
