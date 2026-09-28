// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.UseHint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that the hint recovery matches the reference for both hint values and both values of γ₂ across [0, q).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [DataRow(WideGamma2)]
    [DataRow(NarrowGamma2)]
    public void UseHint_WhenCoefficientIsInRange_ShouldMatchTheReference(int gamma2)
    {
        for (int value = 0; value < Q; value += 97)
        {
            for (int hint = 0; hint <= 1; hint++)
            {
                int expected = MLDsaReference.UseHint(gamma2, hint, value);
                int actual = MLDsaEngine.UseHint(gamma2, hint, value);

                if (expected != actual)
                    Assert.Fail($"UseHint({gamma2}, {hint}, {value}) = {actual}, not {expected}.");
            }
        }
    }
}
