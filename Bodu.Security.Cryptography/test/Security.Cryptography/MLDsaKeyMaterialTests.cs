// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaKeyMaterialTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLDsaKeyMaterial" />: the values it keeps for signing and verification, the same
/// whichever way the key was set, and the zeroing of its secret state.
/// </summary>
[TestClass]
public sealed partial class MLDsaKeyMaterialTests
{
    /// <summary>
    /// Returns the engine parameters for a parameter-set designator.
    /// </summary>
    /// <param name="designator">The designator: 44, 65 or 87.</param>
    /// <returns>The parameter set.</returns>
    private static MLDsaParameters Parameters(int designator) =>
        designator switch
        {
            44 => MLDsaParameters.MLDsa44,
            65 => MLDsaParameters.MLDsa65,
            _ => MLDsaParameters.MLDsa87,
        };

    /// <summary>
    /// Returns a seed ξ drawn from a seeded generator.
    /// </summary>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The 32-byte seed ξ.</returns>
    private static byte[] Seed(int seed)
    {
        byte[] xi = new byte[32];
        new Random(seed).NextBytes(xi);

        return xi;
    }

    /// <summary>
    /// Generates a seeded key pair through the key generation that keeps no values.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="seed">The 32-byte seed ξ.</param>
    /// <returns>The encoded public and private keys.</returns>
    private static (byte[] PublicKey, byte[] PrivateKey) GenerateKeys(MLDsaParameters parameters, byte[] seed)
    {
        byte[] publicKey = new byte[parameters.PublicKeySize];
        byte[] privateKey = new byte[parameters.PrivateKeySize];
        MLDsaEngine.KeyGen(parameters, seed, publicKey, privateKey);

        return (publicKey, privateKey);
    }

    /// <summary>
    /// Asserts that two key materials hold the same keys and keep the same values, naming the first that differs.
    /// </summary>
    /// <param name="expected">The expected key material.</param>
    /// <param name="actual">The key material under test.</param>
    private static void AssertSameValues(MLDsaKeyMaterial expected, MLDsaKeyMaterial actual)
    {
        CollectionAssert.AreEqual(expected.PublicKey, actual.PublicKey, "public key");
        CollectionAssert.AreEqual(expected.PrivateKey, actual.PrivateKey, "private key");
        CollectionAssert.AreEqual(expected.PublicKeyHash, actual.PublicKeyHash, "tr");
        CollectionAssert.AreEqual(expected.Matrix, actual.Matrix, "matrix");
        CollectionAssert.AreEqual(expected.HighOrderVector, actual.HighOrderVector, "NTT(t1 * 2^d)");
        CollectionAssert.AreEqual(expected.SecretVector1, actual.SecretVector1, "s1-hat");
        CollectionAssert.AreEqual(expected.SecretVector2, actual.SecretVector2, "s2-hat");
        CollectionAssert.AreEqual(expected.LowOrderVector, actual.LowOrderVector, "t0-hat");
    }
}
