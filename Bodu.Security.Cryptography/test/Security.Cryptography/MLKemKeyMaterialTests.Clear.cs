// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemKeyMaterialTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class MLKemKeyMaterialTests
{
    /// <summary>
    /// Verifies that clearing key material that holds a key pair zeroes the cached secret vector as well as the
    /// encoded decapsulation key.
    /// </summary>
    [TestMethod]
    public void Clear_WhenKeyMaterialHoldsAKeyPair_ShouldZeroTheSecretVector()
    {
        MLKemParameters parameters = MLKemParameters.MLKem768;
        (byte[] encapsulationKey, byte[] decapsulationKey, int[] matrix) = GenerateKeys(parameters, 3);
        var material = MLKemKeyMaterial.ForKeyPair(parameters, encapsulationKey, decapsulationKey, matrix);
        Assert.IsTrue(material.SecretVector!.Any(coefficient => coefficient != 0), "The secret vector starts populated.");

        material.Clear();

        Assert.IsTrue(material.SecretVector!.All(coefficient => coefficient == 0), "secret vector");
        Assert.IsTrue(material.PrivateKey!.All(value => value == 0), "decapsulation key");
    }
}
