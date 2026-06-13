// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaAcvpVectors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Loads the embedded NIST ACVP ML-DSA known-answer vectors and provides the shared assertion routines used by the
/// per-parameter-set <c>KnownAnswerTests</c> partials.
/// </summary>
public static class MLDsaAcvpVectors
{
    /// <summary>
    /// Yields the key-generation vectors for one parameter set as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="parameterSet">The parameter-set name, such as <c>"ML-DSA-65"</c>.</param>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> KeyGen(string parameterSet)
    {
        using Stream stream = OpenResource("Bodu.Security.Cryptography.MLDsa.AcvpKeyGen.txt");
        foreach (DsaKeyGenKnownAnswer vector in DsaKeyGenKnownAnswer.Read(stream).Where(v => v.ParameterSet == parameterSet))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Yields the signature-generation vectors (deterministic and hedged) for one parameter set as
    /// <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="parameterSet">The parameter-set name.</param>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> SigGen(string parameterSet)
    {
        using Stream stream = OpenResource("Bodu.Security.Cryptography.MLDsa.AcvpSigGen.txt");
        foreach (DsaSigGenKnownAnswer vector in DsaSigGenKnownAnswer.Read(stream).Where(v => v.ParameterSet == parameterSet))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Yields the signature-verification vectors for one parameter set as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="parameterSet">The parameter-set name.</param>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> SigVer(string parameterSet)
    {
        using Stream stream = OpenResource("Bodu.Security.Cryptography.MLDsa.AcvpSigVer.txt");
        foreach (DsaSigVerKnownAnswer vector in DsaSigVerKnownAnswer.Read(stream).Where(v => v.ParameterSet == parameterSet))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Asserts a key-generation vector: importing the seed ξ must reproduce the expected encoded key pair.
    /// </summary>
    /// <param name="dsa">A fresh instance of the parameter set under test.</param>
    /// <param name="vector">The KAT vector.</param>
    public static void AssertKeyGen(MLDsa dsa, DsaKeyGenKnownAnswer vector)
    {
        dsa.ImportPrivateSeed(vector.Seed);

        CollectionAssert.AreEqual(vector.ExpectedPublicKey, dsa.ExportPublicKey());
        CollectionAssert.AreEqual(vector.ExpectedPrivateKey, dsa.ExportPrivateKey());
    }

    /// <summary>
    /// Asserts a signature-generation vector: signing with the vector's key, context, and randomness mode must
    /// reproduce the expected signature, which must also verify under the matching public key.
    /// </summary>
    /// <param name="dsa">A fresh instance of the parameter set under test.</param>
    /// <param name="vector">The KAT vector.</param>
    public static void AssertSigGen(MLDsa dsa, DsaSigGenKnownAnswer vector)
    {
        dsa.ImportPrivateKey(vector.PrivateKey);
        CollectionAssert.AreEqual(vector.PublicKey, dsa.ExportPublicKey());

        byte[] signature;
        if (vector.Deterministic)
        {
            dsa.DeterministicSigning = true;
            signature = dsa.SignData(vector.Message, vector.Context);
        }
        else
        {
            Assert.IsNotNull(vector.Rnd, $"{vector.Name}: hedged vectors must carry the explicit rnd.");
            signature = dsa.SignDataInternal(vector.Message, vector.Context, vector.Rnd);
        }

        CollectionAssert.AreEqual(vector.ExpectedSignature, signature);
        Assert.IsTrue(dsa.VerifyData(vector.Message, signature, vector.Context));
    }

    /// <summary>
    /// Asserts a signature-verification vector: verification must reach the mandated verdict.
    /// </summary>
    /// <param name="dsa">A fresh instance of the parameter set under test.</param>
    /// <param name="vector">The KAT vector.</param>
    public static void AssertSigVer(MLDsa dsa, DsaSigVerKnownAnswer vector)
    {
        dsa.ImportPublicKey(vector.PublicKey);

        Assert.AreEqual(vector.ExpectedValid, dsa.VerifyData(vector.Message, vector.Signature, vector.Context));
    }

    /// <summary>
    /// Opens an embedded KAT resource by name.
    /// </summary>
    /// <param name="resourceName">The manifest resource name.</param>
    /// <returns>The opened stream.</returns>
    /// <exception cref="InvalidOperationException">The resource is not present in the test assembly.</exception>
    private static Stream OpenResource(string resourceName) =>
        typeof(MLDsaAcvpVectors).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{resourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");
}
