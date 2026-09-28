// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.MakeHint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that the hint computation matches the reference for both values of γ₂ over seeded pairs of coefficients.
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [DataRow(WideGamma2)]
    [DataRow(NarrowGamma2)]
    public void MakeHint_WhenCoefficientsAreSeeded_ShouldMatchTheReference(int gamma2)
    {
        var random = new Random(0x0204_0007 + gamma2);

        for (int iteration = 0; iteration < 100_000; iteration++)
        {
            int z = iteration % 3 == 0 ? random.Next(gamma2) : random.Next(Q);
            int r = random.Next(Q);

            if (MLDsaReference.MakeHint(gamma2, z, r) != MLDsaEngine.MakeHint(gamma2, z, r))
                Assert.Fail($"MakeHint({gamma2}, {z}, {r}) differs from the reference.");
        }
    }
}
