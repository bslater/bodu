// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreTests.OnePassBlocks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Poly1305AeadCoreTests
{
    /// <summary>
    /// Verifies that the plan kept for each kernel gives what planning each message afresh gives, for every count of
    /// keystream bytes to past the longest run drawn in one pass.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    [DataRow("Ssse3")]
    [DataRow("AdvSimd")]
    [DataRow("Avx2")]
    [DataRow("Avx512")]
    [DataRow("Avx512Wide")]
    public void OnePassBlocks_WhenGivenAnyKeystreamBytes_ForEachKernel_ShouldMatchPlanOnePass(string kernel)
    {
        var kind = Enum.Parse<ChaCha20Core.KernelKind>(kernel);

        for (int bytes = 0; bytes <= 1100; bytes++)
            Assert.AreEqual(Poly1305AeadCore.PlanOnePass(kind, bytes), Poly1305AeadCore.OnePassBlocks(kind, bytes), $"{bytes} bytes");
    }

    /// <summary>
    /// Verifies that a message whose keystream spans more than sixteen blocks is never drawn in one pass, whatever the
    /// kernel.
    /// </summary>
    /// <param name="keystreamBytes">The number of keystream bytes the message needs.</param>
    [TestMethod]
    [DataRow(1025)]
    [DataRow(4096)]
    [DataRow(int.MaxValue)]
    public void OnePassBlocks_WhenLongerThanSixteenBlocks_ShouldBeZero(int keystreamBytes)
    {
        foreach (ChaCha20Core.KernelKind kernel in Enum.GetValues<ChaCha20Core.KernelKind>().Where(kind => kind != ChaCha20Core.KernelKind.Auto))
            Assert.AreEqual(0, Poly1305AeadCore.OnePassBlocks(kernel, keystreamBytes), kernel.ToString());
    }
}
