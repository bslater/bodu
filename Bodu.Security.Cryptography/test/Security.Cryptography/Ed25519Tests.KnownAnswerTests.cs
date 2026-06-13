// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Locks <see cref="Ed25519" /> against the RFC 8032 §7.1 test vectors and the full Project Wycheproof Ed25519
/// verification corpus embedded in the test assembly, both expressed as <see cref="SignatureKnownAnswer" /> KAT
/// rows.
/// </summary>
public sealed partial class Ed25519Tests
{
    /// <summary>
    /// Resource name of the embedded Wycheproof KAT file shipped with this test assembly.
    /// </summary>
    private const string WycheproofResourceName = "Bodu.Security.Cryptography.Ed25519.Wycheproof.txt";

    /// <summary>
    /// Yields the published RFC 8032 §7.1 pure-Ed25519 vectors (TEST 1–3 and TEST SHA(abc)) as KAT rows carrying
    /// the private seed for deterministic signing checks.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> Rfc8032KnownAnswers()
    {
        yield return new object[]
        {
            new SignatureKnownAnswer(
                "RFC 8032 TEST 1 (empty message)",
                Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a"),
                Array.Empty<byte>(),
                Convert.FromHexString(
                    "e5564300c360ac729086e2cc806e828a84877f1eb8e5d974d873e065224901555fb8821590a33bacc61e39701cf9b46bd25bf5f0595bbe24655141438e7a100b"),
                ExpectedValid: true,
                Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60")),
        };

        yield return new object[]
        {
            new SignatureKnownAnswer(
                "RFC 8032 TEST 2 (one byte)",
                Convert.FromHexString("3d4017c3e843895a92b70aa74d1b7ebc9c982ccf2ec4968cc0cd55f12af4660c"),
                Convert.FromHexString("72"),
                Convert.FromHexString(
                    "92a009a9f0d4cab8720e820b5f642540a2b27b5416503f8fb3762223ebdb69da085ac1e43e15996e458f3613d0f11d8c387b2eaeb4302aeeb00d291612bb0c00"),
                ExpectedValid: true,
                Convert.FromHexString("4ccd089b28ff96da9db6c346ec114e0f5b8a319f35aba624da8cf6ed4fb8a6fb")),
        };

        yield return new object[]
        {
            new SignatureKnownAnswer(
                "RFC 8032 TEST 3 (two bytes)",
                Convert.FromHexString("fc51cd8e6218a1a38da47ed00230f0580816ed13ba3303ac5deb911548908025"),
                Convert.FromHexString("af82"),
                Convert.FromHexString(
                    "6291d657deec24024827e69c3abe01a30ce548a284743a445e3680d7db5ac3ac18ff9b538d16f290ae67f760984dc6594a7c15e9716ed28dc027beceea1ec40a"),
                ExpectedValid: true,
                Convert.FromHexString("c5aa8df43f9f837bedb7442f31dcb7b166d38535076f094b85ce3a2e0b4458f7")),
        };

        yield return new object[]
        {
            new SignatureKnownAnswer(
                "RFC 8032 TEST SHA(abc)",
                Convert.FromHexString("ec172b93ad5e563bf4932c70e1245034c35467ef2efd4d64ebf819683467e2bf"),
                Convert.FromHexString(
                    "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f"),
                Convert.FromHexString(
                    "dc2a4459e7369633a52b1bf277839a00201009a3efbf3ecb69bea2186c26b58909351fc9ac90b3ecfdfbc7c66431e0303dca179c138ac17ad9bef1177331a704"),
                ExpectedValid: true,
                Convert.FromHexString("833fe62409237b9d62ec77587520911e9a759cec1d19755b7da901b96dca3d42")),
        };
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.ImportPrivateKey" /> derives the published public key and
    /// <see cref="Ed25519.SignData(ReadOnlySpan{byte})" /> reproduces the published signature for each RFC 8032
    /// §7.1 vector.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(Rfc8032KnownAnswers),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SignData_WhenGivenRfc8032Vector_ShouldDerivePublishedKeyAndSignature(SignatureKnownAnswer vector)
    {
        Assert.IsNotNull(vector.PrivateKey, $"{vector.Name}: RFC signing vectors must carry the private seed.");

        using var signer = new Ed25519();
        signer.ImportPrivateKey(vector.PrivateKey);

        CollectionAssert.AreEqual(vector.PublicKey, signer.ExportPublicKey());
        CollectionAssert.AreEqual(vector.Signature, signer.SignData(vector.Message));
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> accepts the
    /// published signature of each RFC 8032 §7.1 vector through a verify-only instance.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(Rfc8032KnownAnswers),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void VerifyData_WhenGivenRfc8032Vector_ShouldAcceptPublishedSignature(SignatureKnownAnswer vector)
    {
        using var verifier = new Ed25519();
        verifier.ImportPublicKey(vector.PublicKey);

        Assert.AreEqual(vector.ExpectedValid, verifier.VerifyData(vector.Message, vector.Signature));
    }

    /// <summary>
    /// Loads all Wycheproof verification vectors from the embedded resource and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per KAT vector.</returns>
    /// <exception cref="InvalidOperationException">The embedded KAT resource cannot be located.</exception>
    private static IEnumerable<object[]> WycheproofVectors()
    {
        using Stream stream = typeof(Ed25519Tests).Assembly.GetManifestResourceStream(WycheproofResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{WycheproofResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (SignatureKnownAnswer vector in SignatureKnownAnswer.Read(stream))
            yield return new object[] { vector };
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519.VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> reaches the verdict
    /// mandated by every vector of the Wycheproof Ed25519 corpus, covering malleable S values, truncated and
    /// oversized signatures, swapped components, and special-case points.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WycheproofVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void VerifyData_WhenGivenWycheproofVector_ShouldReachMandatedVerdict(SignatureKnownAnswer vector)
    {
        using var verifier = new Ed25519();
        verifier.ImportPublicKey(vector.PublicKey);

        Assert.AreEqual(vector.ExpectedValid, verifier.VerifyData(vector.Message, vector.Signature));
    }

    /// <summary>
    /// Verifies that signatures over every message length from 0 through 256 bytes round-trip through signing and
    /// verification, exercising the SHA-512 block boundaries in the nonce and challenge derivations.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void SignData_WhenMessageLengthSweepsZeroTo256_ShouldRoundTripVerification()
    {
        using var algorithm = new Ed25519();
        algorithm.ImportPrivateKey(Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"));

        var message = new byte[256];
        for (var i = 0; i < message.Length; i++)
            message[i] = (byte)i;

        for (var length = 0; length <= message.Length; length++)
        {
            ReadOnlySpan<byte> slice = message.AsSpan(0, length);
            var signature = algorithm.SignData(slice);

            Assert.IsTrue(algorithm.VerifyData(slice, signature), $"Round-trip failed at message length {length}.");
        }
    }
}
