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
public sealed partial class MLKemKeyMaterialTests
{
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
