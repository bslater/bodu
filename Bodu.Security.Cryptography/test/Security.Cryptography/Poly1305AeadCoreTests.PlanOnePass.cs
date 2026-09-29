// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.PlanOnePass.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that a message's keystream is drawn in one pass, and over how many blocks, only where that is estimated
    /// to cost less than drawing it as it comes; the byte counts include the 64 bytes before an RFC 8439 message or the
    /// 32 before a secretbox one.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="keystreamBytes">The number of keystream bytes the message needs, the key block's included.</param>
    /// <param name="expected">The expected number of blocks in the one-pass run, or zero.</param>
    [TestMethod]
    [DataRow("Scalar", 164, 0)]
    [DataRow("Scalar", 1024, 0)]
    [DataRow("Ssse3", 320, 0)]
    [DataRow("Ssse3", 385, 8)]
    [DataRow("AdvSimd", 129, 4)]
    [DataRow("Avx2", 128, 0)]
    [DataRow("Avx2", 129, 4)]
    [DataRow("Avx2", 320, 8)]
    [DataRow("Avx2", 576, 0)]
    [DataRow("Avx512", 48, 0)]
    [DataRow("Avx512", 64, 0)]
    [DataRow("Avx512", 65, 4)]
    [DataRow("Avx512", 96, 4)]
    [DataRow("Avx512", 256, 4)]
    [DataRow("Avx512", 257, 8)]
    [DataRow("Avx512", 575, 9)]
    [DataRow("Avx512", 576, 0)]
    [DataRow("Avx512", 1024, 16)]
    [DataRow("Avx512", 1025, 0)]
    [DataRow("Avx512Wide", 65, 4)]
    [DataRow("Avx512Wide", 576, 16)]
    public void PlanOnePass_WhenGivenKeystreamBytes_ForEachKernel_ShouldTakeOnePassOnlyWhereItCostsLess(string kernel, int keystreamBytes, int expected)
    {
        Assert.AreEqual(expected, Poly1305AeadCore.PlanOnePass(Enum.Parse<ChaCha20Core.KernelKind>(kernel), keystreamBytes));
    }

    /// <summary>
    /// Verifies that on the block function, where every block costs the same however it is drawn, no message's
    /// keystream is drawn in one pass.
    /// </summary>
    [TestMethod]
    public void PlanOnePass_WhenKernelIsScalar_ShouldNeverTakeOnePass()
    {
        for (int bytes = 0; bytes <= 1100; bytes++)
            Assert.AreEqual(0, Poly1305AeadCore.PlanOnePass(ChaCha20Core.KernelKind.Scalar, bytes), $"{bytes} bytes");
    }
}
