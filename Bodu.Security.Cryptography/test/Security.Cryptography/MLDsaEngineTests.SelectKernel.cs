// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.SelectKernel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that dispatch selects a concrete kernel the processor can run.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenCalled_ShouldReturnAKernelTheProcessorSupports()
    {
        MLDsaEngine.KernelKind kernel = MLDsaEngine.SelectKernel();

        Assert.AreNotEqual(MLDsaEngine.KernelKind.Auto, kernel);
        Assert.IsTrue(MLDsaEngine.IsSupported(kernel), kernel.ToString());
    }

    /// <summary>
    /// Verifies that dispatch prefers the AVX2 kernel wherever its gate is open.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx2IsAvailable_ShouldReturnAvx2()
    {
        if (!SimdCapabilities.Avx2)
            Assert.Inconclusive("AVX2 is not available on this processor.");

        Assert.AreEqual(MLDsaEngine.KernelKind.Avx2, MLDsaEngine.SelectKernel());
    }

    /// <summary>
    /// Verifies that dispatch falls back to the scalar kernel wherever the AVX2 gate is closed.
    /// </summary>
    [TestMethod]
    public void SelectKernel_WhenAvx2IsUnavailable_ShouldReturnScalar()
    {
        if (SimdCapabilities.Avx2)
            Assert.Inconclusive("AVX2 is available on this processor.");

        Assert.AreEqual(MLDsaEngine.KernelKind.Scalar, MLDsaEngine.SelectKernel());
    }

    /// <summary>
    /// Verifies that the scalar kernel, and the dispatched kind, run on every processor.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Auto")]
    [DataRow("Scalar")]
    public void IsSupported_WhenKernelRunsEverywhere_ShouldReturnTrue(string kernel)
    {
        Assert.IsTrue(MLDsaEngine.IsSupported(Enum.Parse<MLDsaEngine.KernelKind>(kernel)));
    }

    /// <summary>
    /// Verifies that a value naming no kernel is reported as unsupported.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsUndefined_ShouldReturnFalse()
    {
        Assert.IsFalse(MLDsaEngine.IsSupported((MLDsaEngine.KernelKind)99));
    }

    /// <summary>
    /// Verifies that the vector kernel is reported as supported exactly when the processor has its instruction set,
    /// whatever the process's SIMD switch says.
    /// </summary>
    [TestMethod]
    public void IsSupported_WhenKernelIsVectorized_ShouldFollowTheProcessor()
    {
        Assert.AreEqual(System.Runtime.Intrinsics.X86.Avx2.IsSupported, MLDsaEngine.IsSupported(MLDsaEngine.KernelKind.Avx2));
    }
}
