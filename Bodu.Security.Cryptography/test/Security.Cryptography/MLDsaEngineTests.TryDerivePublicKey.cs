// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.TryDerivePublicKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that the values import hands over equal what the Expand methods derive from the encoded keys.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void TryDerivePublicKey_WhenValuesAreRequested_ShouldProduceWhatTheExpandMethodsDerive(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        (byte[] publicKey, byte[] privateKey) = GenerateKeys(parameters, Seed(0x0204_0310 + designator));
        KeyValues values = KeyValues.Allocate(parameters);

        bool derived = MLDsaEngine.TryDerivePublicKey(
            parameters,
            privateKey,
            new byte[parameters.PublicKeySize],
            values.Matrix,
            values.HighOrderVector,
            values.SecretVector1,
            values.SecretVector2,
            values.LowOrderVector);

        Assert.IsTrue(derived);
        values.AssertEqualTo(Expand(parameters, publicKey, privateKey));
    }

    /// <summary>
    /// Verifies that both forms of the import check recompute the public key a private key was generated with.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void TryDerivePublicKey_WhenKeyIsConsistent_ShouldRecomputeThePublicKey(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        (byte[] publicKey, byte[] privateKey) = GenerateKeys(parameters, Seed(0x0204_0311 + designator));
        byte[] plain = new byte[parameters.PublicKeySize];
        byte[] keeping = new byte[parameters.PublicKeySize];
        KeyValues values = KeyValues.Allocate(parameters);

        Assert.IsTrue(MLDsaEngine.TryDerivePublicKey(parameters, privateKey, plain));
        Assert.IsTrue(MLDsaEngine.TryDerivePublicKey(
            parameters, privateKey, keeping, values.Matrix, values.HighOrderVector, values.SecretVector1, values.SecretVector2, values.LowOrderVector));

        CollectionAssert.AreEqual(publicKey, plain, "plain");
        CollectionAssert.AreEqual(publicKey, keeping, "keeping values");
    }

    /// <summary>
    /// Verifies that both forms of the import check reject a private key whose embedded hash tr does not match its
    /// public key.
    /// </summary>
    [TestMethod]
    public void TryDerivePublicKey_WhenHashIsTampered_ShouldReturnFalse()
    {
        MLDsaParameters parameters = MLDsaParameters.MLDsa65;
        (_, byte[] privateKey) = GenerateKeys(parameters, Seed(0x0204_0312));
        privateKey[64] ^= 0x01;
        KeyValues values = KeyValues.Allocate(parameters);

        bool plain = MLDsaEngine.TryDerivePublicKey(parameters, privateKey, new byte[parameters.PublicKeySize]);
        bool keeping = MLDsaEngine.TryDerivePublicKey(
            parameters,
            privateKey,
            new byte[parameters.PublicKeySize],
            values.Matrix,
            values.HighOrderVector,
            values.SecretVector1,
            values.SecretVector2,
            values.LowOrderVector);

        Assert.IsFalse(plain, "plain");
        Assert.IsFalse(keeping, "keeping values");
    }
}
