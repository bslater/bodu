// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaKeyMaterialTests.TryImportPrivateKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class MLDsaKeyMaterialTests
{
    /// <summary>
    /// Verifies that importing a generated private key yields key material holding the same keys and keeping the same
    /// values as the generated key material, for every parameter set.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void TryImportPrivateKey_WhenKeyIsConsistent_ShouldKeepTheValuesOfTheGeneratedKey(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        var generated = MLDsaKeyMaterial.Generate(parameters, Seed(0x0204_0420 + designator));

        MLDsaKeyMaterial? imported = MLDsaKeyMaterial.TryImportPrivateKey(parameters, generated.PrivateKey);

        Assert.IsNotNull(imported);
        AssertSameValues(generated, imported);
    }

    /// <summary>
    /// Verifies that importing a private key whose embedded hash tr does not match its public key yields no key
    /// material.
    /// </summary>
    [TestMethod]
    public void TryImportPrivateKey_WhenHashIsTampered_ShouldReturnNull()
    {
        MLDsaParameters parameters = MLDsaParameters.MLDsa65;
        (_, byte[] privateKey) = GenerateKeys(parameters, Seed(0x0204_0421));
        privateKey[64] ^= 0x01;

        MLDsaKeyMaterial? imported = MLDsaKeyMaterial.TryImportPrivateKey(parameters, privateKey);

        Assert.IsNull(imported);
    }
}
