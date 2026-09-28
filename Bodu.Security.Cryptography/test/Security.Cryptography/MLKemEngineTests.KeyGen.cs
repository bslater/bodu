// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngineTests.KeyGen.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLKemEngineTests
{
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
        var random = new Random(0x0203_0100 + k);
        byte[] d = new byte[32];
        byte[] z = new byte[32];
        random.NextBytes(d);
        random.NextBytes(z);
        byte[] encapsulationKey = new byte[parameters.EncapsulationKeySize];
        int[] generated = new int[k * k * MLKemEngine.N];

        MLKemEngine.KeyGen(parameters, d, z, encapsulationKey, new byte[parameters.DecapsulationKeySize], generated, [], []);

        int[] expanded = new int[k * k * MLKemEngine.N];
        MLKemEngine.ExpandMatrix(parameters, encapsulationKey.AsSpan(384 * k), expanded);
        CollectionAssert.AreEqual(expanded, generated);
    }
}
