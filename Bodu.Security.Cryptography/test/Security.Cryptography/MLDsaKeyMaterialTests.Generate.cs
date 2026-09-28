// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaKeyMaterialTests.Generate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class MLDsaKeyMaterialTests
{
    /// <summary>
    /// Verifies that key material generated from a seed keeps the values that key material deriving them from the
    /// encoded keys caches, so what key generation hands over is what signing and verification expect.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void Generate_WhenCalled_ShouldKeepTheValuesTheKeysDerive(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);

        var generated = MLDsaKeyMaterial.Generate(parameters, Seed(0x0204_0410 + designator));
        var derived = MLDsaKeyMaterial.ForKeyPair(
            parameters, (byte[])generated.PublicKey.Clone(), (byte[])generated.PrivateKey!.Clone());

        AssertSameValues(derived, generated);
    }

    /// <summary>
    /// Verifies that key material generated from a seed holds the encoded keys key generation produces from that seed.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void Generate_WhenCalled_ShouldHoldTheKeysKeyGenerationProduces(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        byte[] seed = Seed(0x0204_0411 + designator);

        var generated = MLDsaKeyMaterial.Generate(parameters, seed);
        (byte[] publicKey, byte[] privateKey) = GenerateKeys(parameters, seed);

        CollectionAssert.AreEqual(publicKey, generated.PublicKey, "public key");
        CollectionAssert.AreEqual(privateKey, generated.PrivateKey, "private key");
    }
}
