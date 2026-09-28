// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaKeyMaterialTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class MLDsaKeyMaterialTests
{
    /// <summary>
    /// Verifies that clearing key material that holds a key pair zeroes the cached secret vectors ŝ₁, ŝ₂ and t̂₀ as
    /// well as the encoded private key.
    /// </summary>
    [TestMethod]
    public void Clear_WhenKeyMaterialHoldsAKeyPair_ShouldZeroTheSecretState()
    {
        var material = MLDsaKeyMaterial.Generate(MLDsaParameters.MLDsa65, Seed(0x0204_0440));
        Assert.IsTrue(material.SecretVector1!.Any(coefficient => coefficient != 0), "The secret vectors start populated.");

        material.Clear();

        Assert.IsTrue(material.PrivateKey!.All(value => value == 0), "private key");
        Assert.IsTrue(material.SecretVector1!.All(coefficient => coefficient == 0), "s1-hat");
        Assert.IsTrue(material.SecretVector2!.All(coefficient => coefficient == 0), "s2-hat");
        Assert.IsTrue(material.LowOrderVector!.All(coefficient => coefficient == 0), "t0-hat");
    }
}
