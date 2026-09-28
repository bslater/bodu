// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemKeyMaterialTests.ForPublicKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class MLKemKeyMaterialTests
{
    /// <summary>
    /// Verifies that key material built from an encapsulation key alone holds no secret vector.
    /// </summary>
    [TestMethod]
    public void ForPublicKey_WhenCreated_ShouldHoldNoSecretVector()
    {
        MLKemParameters parameters = MLKemParameters.MLKem768;
        (byte[] encapsulationKey, _, _) = GenerateKeys(parameters, 3);

        var material = MLKemKeyMaterial.ForPublicKey(parameters, encapsulationKey);

        Assert.IsNull(material.SecretVector);
        Assert.IsNull(material.PrivateKey);
    }
}
