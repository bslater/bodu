// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.LanesFor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Verifies that each kernel takes one block for each of its lanes at once: eight with AVX-512, four with either
    /// AVX2 loop, two with AdvSimd, and one for the scalar loop.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    /// <param name="expected">The expected number of blocks.</param>
    [TestMethod]
    [DataRow("Scalar", 1)]
    [DataRow("Avx2", 4)]
    [DataRow("Avx2Paired", 4)]
    [DataRow("Avx512", 8)]
    [DataRow("AdvSimd", 2)]
    public void LanesFor_WhenGivenKernel_ShouldReturnItsLaneCount(string kernel, int expected)
    {
        Assert.AreEqual(expected, Poly1305Core.LanesFor(Enum.Parse<Poly1305Core.KernelKind>(kernel)));
    }
}
