// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemAcvpVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Loads the embedded NIST ACVP ML-KEM known-answer vectors and provides the shared assertion routines used by the
/// per-parameter-set <c>KnownAnswerTests</c> partials.
/// </summary>
public static class MLKemAcvpVectors
{
    /// <summary>
    /// Yields the key-generation vectors for one parameter set as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="parameterSet">The parameter-set name, such as <c>"ML-KEM-768"</c>.</param>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> KeyGen(string parameterSet)
    {
        using Stream stream = OpenResource("Bodu.Security.Cryptography.MLKem.AcvpKeyGen.txt");
        foreach (KemKeyGenKnownAnswer vector in KemKeyGenKnownAnswer.Read(stream).Where(v => v.ParameterSet == parameterSet))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Yields the encapsulation or decapsulation vectors for one parameter set as
    /// <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="parameterSet">The parameter-set name.</param>
    /// <param name="function">Either <c>"encapsulation"</c> or <c>"decapsulation"</c>.</param>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> EncapDecap(string parameterSet, string function)
    {
        using Stream stream = OpenResource("Bodu.Security.Cryptography.MLKem.AcvpEncapDecap.txt");
        foreach (KemEncapDecapKnownAnswer vector in KemEncapDecapKnownAnswer.Read(stream)
                     .Where(v => v.ParameterSet == parameterSet && v.Function == function))
        {
            yield return new object[] { vector };
        }
    }

    /// <summary>
    /// Yields the key-import validation vectors for one parameter set as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="parameterSet">The parameter-set name.</param>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> KeyCheck(string parameterSet)
    {
        using Stream stream = OpenResource("Bodu.Security.Cryptography.MLKem.AcvpKeyCheck.txt");
        foreach (KemKeyCheckKnownAnswer vector in KemKeyCheckKnownAnswer.Read(stream).Where(v => v.ParameterSet == parameterSet))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Asserts a key-generation vector: importing the seed d ‖ z must reproduce the expected encoded key pair.
    /// </summary>
    /// <param name="kem">A fresh instance of the parameter set under test.</param>
    /// <param name="vector">The KAT vector.</param>
    public static void AssertKeyGen(MLKem kem, KemKeyGenKnownAnswer vector)
    {
        var seed = vector.D.Concat(vector.Z).ToArray();
        kem.ImportPrivateSeed(seed);

        CollectionAssert.AreEqual(vector.ExpectedEncapsulationKey, kem.ExportEncapsulationKey());
        CollectionAssert.AreEqual(vector.ExpectedDecapsulationKey, kem.ExportDecapsulationKey());
    }

    /// <summary>
    /// Asserts an encapsulation vector: running the engine with the fixed randomness m must reproduce the expected
    /// ciphertext and shared secret.
    /// </summary>
    /// <param name="parameters">The engine parameter set.</param>
    /// <param name="vector">The KAT vector.</param>
    internal static void AssertEncapsulation(MLKemParameters parameters, KemEncapDecapKnownAnswer vector)
    {
        Assert.IsNotNull(vector.M, $"{vector.Name}: encapsulation vectors must carry the fixed randomness m.");

        var ciphertext = new byte[parameters.CiphertextSize];
        var sharedSecret = new byte[MLKemEngine.SharedSecretSize];
        MLKemEngine.Encapsulate(parameters, vector.Key, vector.M, ciphertext, sharedSecret);

        CollectionAssert.AreEqual(vector.Ciphertext, ciphertext);
        CollectionAssert.AreEqual(vector.SharedSecret, sharedSecret);
    }

    /// <summary>
    /// Asserts a decapsulation vector: importing the decapsulation key and decapsulating the candidate ciphertext
    /// must yield the expected shared secret, including the implicit-rejection key for tampered rows.
    /// </summary>
    /// <param name="kem">A fresh instance of the parameter set under test.</param>
    /// <param name="vector">The KAT vector.</param>
    public static void AssertDecapsulation(MLKem kem, KemEncapDecapKnownAnswer vector)
    {
        kem.ImportDecapsulationKey(vector.Key);

        CollectionAssert.AreEqual(vector.SharedSecret, kem.Decapsulate(vector.Ciphertext));
    }

    /// <summary>
    /// Asserts a key-check vector: import must succeed exactly when the vector is marked valid.
    /// </summary>
    /// <param name="kem">A fresh instance of the parameter set under test.</param>
    /// <param name="vector">The KAT vector.</param>
    public static void AssertKeyCheck(MLKem kem, KemKeyCheckKnownAnswer vector)
    {
        void Import()
        {
            if (vector.Kind == "encapsulation")
                kem.ImportEncapsulationKey(vector.Key);
            else
                kem.ImportDecapsulationKey(vector.Key);
        }

        if (vector.ExpectedValid)
        {
            Import();
        }
        else
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                Import();
            });
        }
    }

    /// <summary>
    /// Resolves the internal engine parameter set for a parameter-set name.
    /// </summary>
    /// <param name="parameterSet">The parameter-set name.</param>
    /// <returns>The matching <see cref="MLKemParameters" /> instance.</returns>
    /// <exception cref="ArgumentException">The name is not a known parameter set.</exception>
    internal static MLKemParameters ResolveParameters(string parameterSet) =>
        parameterSet switch
        {
            "ML-KEM-512" => MLKemParameters.MLKem512,
            "ML-KEM-768" => MLKemParameters.MLKem768,
            "ML-KEM-1024" => MLKemParameters.MLKem1024,
            _ => throw new ArgumentException($"Unknown parameter set '{parameterSet}'.", nameof(parameterSet)),
        };

    /// <summary>
    /// Opens an embedded KAT resource by name.
    /// </summary>
    /// <param name="resourceName">The manifest resource name.</param>
    /// <returns>The opened stream.</returns>
    /// <exception cref="InvalidOperationException">The resource is not present in the test assembly.</exception>
    private static Stream OpenResource(string resourceName) =>
        typeof(MLKemAcvpVectors).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");
}
