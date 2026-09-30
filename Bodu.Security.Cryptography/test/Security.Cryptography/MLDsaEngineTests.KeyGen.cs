// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.KeyGen.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that the values key generation hands over - the matrix, NTT(t₁·2ᵈ) formed by linearity from values
    /// already at hand, and the secret vectors - equal what the Expand methods derive from the encoded keys.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void KeyGen_WhenValuesAreRequested_ShouldProduceWhatTheExpandMethodsDerive(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        byte[] publicKey = new byte[parameters.PublicKeySize];
        byte[] privateKey = new byte[parameters.PrivateKeySize];
        KeyValues values = KeyValues.Allocate(parameters);

        MLDsaEngine.KeyGen(
            parameters,
            Seed(0x0204_0300 + designator),
            publicKey,
            privateKey,
            values.Matrix,
            values.HighOrderVector,
            values.SecretVector1,
            values.SecretVector2,
            values.LowOrderVector);

        values.AssertEqualTo(Expand(parameters, publicKey, privateKey));
    }

    /// <summary>
    /// Verifies that requesting the values leaves key generation's encoded keys as they are without them.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void KeyGen_WhenValuesAreRequested_ShouldProduceTheSameKeys(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        byte[] seed = Seed(0x0204_0301 + designator);
        byte[] publicKey = new byte[parameters.PublicKeySize];
        byte[] privateKey = new byte[parameters.PrivateKeySize];
        KeyValues values = KeyValues.Allocate(parameters);

        MLDsaEngine.KeyGen(
            parameters, seed, publicKey, privateKey, values.Matrix, values.HighOrderVector, values.SecretVector1, values.SecretVector2, values.LowOrderVector);
        (byte[] expectedPublicKey, byte[] expectedPrivateKey) = GenerateKeys(parameters, seed);

        CollectionAssert.AreEqual(expectedPublicKey, publicKey, "public key");
        CollectionAssert.AreEqual(expectedPrivateKey, privateKey, "private key");
    }

    /// <summary>
    /// Verifies that key generation rejects a value span of the wrong length with <see cref="ArgumentException" />
    /// naming it.
    /// </summary>
    [TestMethod]
    public void KeyGen_WhenAValueSpanHasTheWrongLength_ShouldThrowArgumentException()
    {
        MLDsaParameters parameters = MLDsaParameters.MLDsa65;
        KeyValues values = KeyValues.Allocate(parameters);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            MLDsaEngine.KeyGen(
                parameters,
                new byte[32],
                new byte[parameters.PublicKeySize],
                new byte[parameters.PrivateKeySize],
                values.Matrix,
                values.HighOrderVector,
                values.SecretVector1.AsSpan(1),
                values.SecretVector2,
                values.LowOrderVector);
        });

        Assert.AreEqual("secretVector1", ex.ParamName);
    }
}
