// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.KeystreamCost.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that the keystream a message draws after the key block costs its whole groups, and one step more for
    /// the bytes after them: a group through the buffer where that costs less than their blocks one at a time.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="bytes">The number of bytes drawn.</param>
    /// <param name="expected">The expected cost, in halves of a block function call.</param>
    [TestMethod]
    [DataRow("Scalar", 0, 0)]
    [DataRow("Scalar", 150, 6)]
    [DataRow("Ssse3", 200, 4)]
    [DataRow("Avx2", 128, 4)]
    [DataRow("Avx2", 150, 4)]
    [DataRow("Avx2", 1000, 12)]
    [DataRow("Avx512", 0, 0)]
    [DataRow("Avx512", 64, 2)]
    [DataRow("Avx512", 100, 2)]
    [DataRow("Avx512", 256, 2)]
    [DataRow("Avx512", 257, 4)]
    [DataRow("Avx512", 512, 2)]
    [DataRow("Avx512", 700, 4)]
    [DataRow("Avx512Wide", 1100, 5)]
    public void KeystreamCost_WhenGivenBytes_ForEachKernel_ShouldCostTheDrawsTheFramingsMake(string kernel, int bytes, int expected)
    {
        Assert.AreEqual(expected, Poly1305AeadCore.KeystreamCost(Enum.Parse<ChaCha20Core.KernelKind>(kernel), bytes));
    }
}
