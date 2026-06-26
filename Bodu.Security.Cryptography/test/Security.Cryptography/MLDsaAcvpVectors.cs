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
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            if (HexFieldKatReader.GetRequired(record, "Set") != parameterSet)
                continue;

            yield return new object[]
            {
                new KeyGenKnownAnswer
                {
                    Name = HexFieldKatReader.GetRequired(record, "Name"),
                    ParameterSet = parameterSet,
                    Seed = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Seed")),
                    ExpectedPublicKey = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Pk")),
                    ExpectedPrivateKey = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Sk")),
                },
            };
        }
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
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            if (HexFieldKatReader.GetRequired(record, "Set") != parameterSet)
                continue;

            yield return new object[]
            {
                new SignatureKnownAnswer
                {
                    Name = HexFieldKatReader.GetRequired(record, "Name"),
                    ParameterSet = parameterSet,
                    Deterministic = bool.Parse(HexFieldKatReader.GetRequired(record, "Deterministic")),
                    PrivateKey = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Sk")),
                    PublicKey = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Pk")),
                    Context = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Context")),
                    Message = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Message")),
                    Rnd = record.TryGetValue("Rnd", out string? rnd) ? Convert.FromHexString(rnd) : null,
                    Signature = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Signature")),
                },
            };
        }
    }

    /// <summary>
    /// Yields the signature-verification vectors for one parameter set as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <param name="parameterSet">The parameter-set name.</param>
    /// <returns>One row per vector.</returns>
    public static IEnumerable<object[]> SigVer(string parameterSet)
    {
        using Stream stream = OpenResource("Bodu.Security.Cryptography.MLDsa.AcvpSigVer.txt");
        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            if (HexFieldKatReader.GetRequired(record, "Set") != parameterSet)
                continue;

            yield return new object[]
            {
                new SignatureKnownAnswer
                {
                    Name = HexFieldKatReader.GetRequired(record, "Name"),
                    ParameterSet = parameterSet,
                    PublicKey = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Pk")),
                    Context = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Context")),
                    Message = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Message")),
                    Signature = Convert.FromHexString(HexFieldKatReader.GetRequired(record, "Signature")),
                    ExpectedValid = bool.Parse(HexFieldKatReader.GetRequired(record, "Valid")),
                },
            };
        }
    }

    /// <summary>
    /// Asserts a key-generation vector: importing the seed ξ must reproduce the expected encoded key pair.
    /// </summary>
    /// <param name="dsa">A fresh instance of the parameter set under test.</param>
    /// <param name="vector">The KAT vector.</param>
    public static void AssertKeyGen(MLDsa dsa, KeyGenKnownAnswer vector)
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
    public static void AssertSigGen(MLDsa dsa, SignatureKnownAnswer vector)
    {
        Assert.IsNotNull(vector.PrivateKey, $"{vector.Name}: signature-generation vectors must carry the private key.");
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

        CollectionAssert.AreEqual(vector.Signature, signature);
        Assert.IsTrue(dsa.VerifyData(vector.Message, signature, vector.Context));
    }

    /// <summary>
    /// Asserts a signature-verification vector: verification must reach the mandated verdict.
    /// </summary>
    /// <param name="dsa">A fresh instance of the parameter set under test.</param>
    /// <param name="vector">The KAT vector.</param>
    public static void AssertSigVer(MLDsa dsa, SignatureKnownAnswer vector)
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
