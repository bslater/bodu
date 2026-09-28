// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemKeyMaterialTests.ForKeyPair.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class MLKemKeyMaterialTests
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
}
