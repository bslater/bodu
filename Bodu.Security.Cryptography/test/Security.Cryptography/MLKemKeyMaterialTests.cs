// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemKeyMaterialTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="MLKemKeyMaterial" />: the values it derives from the encoded keys, and the
/// zeroing of its secret state.
/// </summary>
[TestClass]
public sealed class MLKemKeyMaterialTests
{
    /// <summary>
    /// Verifies that key material built from a key pair caches H(ek), the matrix Â expanded from ρ, and the vectors t̂
    /// and ŝ decoded from the two keys, for every parameter set.
    /// </summary>
    /// <param name="k">The rank k selecting the parameter set.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void ForKeyPair_WhenMatrixIsNotSupplied_ShouldCacheTheValuesDerivedFromTheKeys(int k)
    {
        MLKemParameters parameters = Parameters(k);
        (byte[] encapsulationKey, byte[] decapsulationKey, _) = GenerateKeys(parameters, k);

        var material = MLKemKeyMaterial.ForKeyPair(parameters, encapsulationKey, decapsulationKey);

        byte[] hash = new byte[32];
        KeccakSponge.Sha3_256(encapsulationKey, hash);
        int[] matrix = new int[k * k * MLKemEngine.N];
        MLKemEngine.ExpandMatrix(parameters, encapsulationKey.AsSpan(384 * k), matrix);
        int[] publicVector = new int[k * MLKemEngine.N];
        MLKemEngine.DecodeVector(parameters, encapsulationKey.AsSpan(0, 384 * k), publicVector);
        int[] secretVector = new int[k * MLKemEngine.N];
        MLKemEngine.DecodeVector(parameters, decapsulationKey.AsSpan(0, 384 * k), secretVector);

        CollectionAssert.AreEqual(hash, material.EncapsulationKeyHash, "H(ek)");
        CollectionAssert.AreEqual(matrix, material.Matrix, "matrix");
        CollectionAssert.AreEqual(publicVector, material.PublicVector, "t-hat");
        CollectionAssert.AreEqual(secretVector, material.SecretVector, "s-hat");
    }

    /// <summary>
    /// Verifies that the matrix key generation hands over is the matrix expanded from the encapsulation key's seed, so
    /// key material built from it caches the same values as key material that expands its own.
    /// </summary>
    /// <param name="k">The rank k selecting the parameter set.</param>
    [TestMethod]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void KeyGen_WhenMatrixIsRequested_ShouldProduceTheExpandedMatrix(int k)
    {
        MLKemParameters parameters = Parameters(k);
        (byte[] encapsulationKey, _, int[] generated) = GenerateKeys(parameters, k);

        int[] expanded = new int[k * k * MLKemEngine.N];
        MLKemEngine.ExpandMatrix(parameters, encapsulationKey.AsSpan(384 * k), expanded);

        CollectionAssert.AreEqual(expanded, generated);
    }

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

    /// <summary>
    /// Returns the engine parameters for a rank.
    /// </summary>
    /// <param name="k">The rank.</param>
    /// <returns>The parameter set.</returns>
    private static MLKemParameters Parameters(int k) =>
        k switch
        {
            2 => MLKemParameters.MLKem512,
            3 => MLKemParameters.MLKem768,
            _ => MLKemParameters.MLKem1024,
        };

    /// <summary>
    /// Generates a seeded key pair together with the matrix key generation expands.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="k">The rank.</param>
    /// <returns>The encapsulation key, the decapsulation key and the matrix.</returns>
    private static (byte[] EncapsulationKey, byte[] DecapsulationKey, int[] Matrix) GenerateKeys(MLKemParameters parameters, int k)
    {
        var random = new Random(0x0203_0100 + k);
        byte[] d = new byte[32];
        byte[] z = new byte[32];
        random.NextBytes(d);
        random.NextBytes(z);

        byte[] encapsulationKey = new byte[parameters.EncapsulationKeySize];
        byte[] decapsulationKey = new byte[parameters.DecapsulationKeySize];
        int[] matrix = new int[k * k * MLKemEngine.N];
        MLKemEngine.KeyGen(parameters, d, z, encapsulationKey, decapsulationKey, matrix, [], []);

        return (encapsulationKey, decapsulationKey, matrix);
    }
}
