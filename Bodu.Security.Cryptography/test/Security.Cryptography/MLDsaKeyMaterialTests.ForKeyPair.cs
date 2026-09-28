// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaKeyMaterialTests.ForKeyPair.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class MLDsaKeyMaterialTests
{
    /// <summary>
    /// Verifies that key material built from the encoded keys alone caches tr, the matrix Â, NTT(t₁·2ᵈ) and the
    /// secret vectors as the engine's Expand methods derive them, for every parameter set.
    /// </summary>
    /// <param name="designator">The parameter-set designator.</param>
    [TestMethod]
    [DataRow(44)]
    [DataRow(65)]
    [DataRow(87)]
    public void ForKeyPair_WhenCreated_ShouldCacheTheValuesDerivedFromTheKeys(int designator)
    {
        MLDsaParameters parameters = Parameters(designator);
        (byte[] publicKey, byte[] privateKey) = GenerateKeys(parameters, Seed(0x0204_0400 + designator));
        int k = parameters.K;
        int l = parameters.L;

        var material = MLDsaKeyMaterial.ForKeyPair(parameters, publicKey, privateKey);

        byte[] hash = new byte[64];
        KeccakSponge.Shake256(publicKey, hash);
        int[] matrix = new int[k * l * MLDsaEngine.N];
        MLDsaEngine.ExpandMatrix(parameters, publicKey.AsSpan(0, 32), matrix);
        int[] highOrderVector = new int[k * MLDsaEngine.N];
        MLDsaEngine.ExpandPublicKey(parameters, publicKey, highOrderVector);
        int[] secretVector1 = new int[l * MLDsaEngine.N];
        int[] secretVector2 = new int[k * MLDsaEngine.N];
        int[] lowOrderVector = new int[k * MLDsaEngine.N];
        MLDsaEngine.ExpandPrivateKey(parameters, privateKey, secretVector1, secretVector2, lowOrderVector);

        CollectionAssert.AreEqual(hash, material.PublicKeyHash, "tr");
        CollectionAssert.AreEqual(matrix, material.Matrix, "matrix");
        CollectionAssert.AreEqual(highOrderVector, material.HighOrderVector, "NTT(t1 * 2^d)");
        CollectionAssert.AreEqual(secretVector1, material.SecretVector1, "s1-hat");
        CollectionAssert.AreEqual(secretVector2, material.SecretVector2, "s2-hat");
        CollectionAssert.AreEqual(lowOrderVector, material.LowOrderVector, "t0-hat");
    }
}
