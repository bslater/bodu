// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCoreTests.LanesFor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class SerpentCoreTests
{
    /// <summary>
    /// Verifies that each kernel takes the widest run it offers that fits in the blocks that remain, down to one block
    /// for the scalar rounds.
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
    [DataRow("Avx512", 7, 4)]
    [DataRow("Avx512", 100, 8)]
    public void LanesFor_ForEachKernel_ShouldTakeTheWidestRunThatFits(string kernel, int remaining, int expected)
    {
        Assert.AreEqual(expected, SerpentCore.LanesFor(Enum.Parse<SerpentCore.KernelKind>(kernel), remaining));
    }
}
