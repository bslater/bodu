// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaKeyMaterialTests.ForPublicKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class MLDsaKeyMaterialTests
{
    /// <summary>
    /// Verifies that key material built from a public key alone caches the hash tr, the matrix Â and NTT(t₁·2ᵈ) as
    /// key material for the whole key pair does.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void ForPublicKey_WhenCreated_ShouldCacheThePublicValuesOfTheKeyPair(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        var keyPair = MLDsaKeyMaterial.Generate(parameters, Seed(0x0204_0430 + designator));

        var material = MLDsaKeyMaterial.ForPublicKey(parameters, (byte[])keyPair.PublicKey.Clone());

        CollectionAssert.AreEqual(keyPair.PublicKeyHash, material.PublicKeyHash, "tr");
        CollectionAssert.AreEqual(keyPair.Matrix, material.Matrix, "matrix");
        CollectionAssert.AreEqual(keyPair.HighOrderVector, material.HighOrderVector, "NTT(t1 * 2^d)");
    }

    /// <summary>
    /// Verifies that key material built from a public key alone holds no private key and no secret vector.
    /// </summary>
    [TestMethod]
    public void ForPublicKey_WhenCreated_ShouldHoldNoSecretState()
    {
        MLDsaParameters parameters = MLDsaParameters.MLDsa65;
        (byte[] publicKey, _) = GenerateKeys(parameters, Seed(0x0204_0431));

        var material = MLDsaKeyMaterial.ForPublicKey(parameters, publicKey);

        Assert.IsNull(material.PrivateKey, "private key");
        Assert.IsNull(material.SecretVector1, "s1-hat");
        Assert.IsNull(material.SecretVector2, "s2-hat");
        Assert.IsNull(material.LowOrderVector, "t0-hat");
    }
}
