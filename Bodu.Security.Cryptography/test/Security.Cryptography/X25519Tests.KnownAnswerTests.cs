// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Locks <see cref="X25519" /> against the RFC 7748 test vectors and the curated Project Wycheproof X25519 vector
/// corpus embedded in the test assembly, both expressed as <see cref="KeyAgreementKnownAnswer" /> KAT rows.
/// </summary>
public sealed partial class X25519Tests
{
    /// <summary>
    /// Resource name of the embedded curated Wycheproof KAT file shipped with this test assembly.
    /// </summary>
    private const string WycheproofResourceName = "Bodu.Security.Cryptography.X25519.Wycheproof.txt";

    /// <summary>
    /// Yields the published RFC 7748 vectors — the two §5.2 single-call scalar-multiplication vectors and both
    /// directions of the §6.1 Diffie-Hellman example — as KAT rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> Rfc7748KnownAnswers()
    {
        yield return new object[]
        {
            new KeyAgreementKnownAnswer(
                "RFC 7748 §5.2 vector 1",
                Convert.FromHexString("a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4"),
                Convert.FromHexString("e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c"),
                Convert.FromHexString("c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552"),
                ExpectRejection: false),
        };

        yield return new object[]
        {
            new KeyAgreementKnownAnswer(
                "RFC 7748 §5.2 vector 2",
                Convert.FromHexString("4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d"),
                Convert.FromHexString("e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493"),
                Convert.FromHexString("95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957"),
                ExpectRejection: false),
        };

        yield return new object[]
        {
            new KeyAgreementKnownAnswer(
                "RFC 7748 §6.1 Alice with Bob's public key",
                Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a"),
                Convert.FromHexString("de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f"),
                Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742"),
                ExpectRejection: false),
        };

        yield return new object[]
        {
            new KeyAgreementKnownAnswer(
                "RFC 7748 §6.1 Bob with Alice's public key",
                Convert.FromHexString("5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb"),
                Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a"),
                Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742"),
                ExpectRejection: false),
        };
    }

    /// <summary>
    /// Verifies that <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" /> reproduces the published shared
    /// secret for each RFC 7748 vector.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(Rfc7748KnownAnswers),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveSharedSecret_WhenGivenRfc7748Vector_ShouldProducePublishedSharedSecret(KeyAgreementKnownAnswer vector)
    {
        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(vector.PrivateKey);

        CollectionAssert.AreEqual(vector.ExpectedSharedSecret, algorithm.DeriveSharedSecret(vector.PeerPublicKey));
    }

    /// <summary>
    /// Verifies that <see cref="X25519.ImportPrivateKey" /> derives the public keys published in the RFC 7748 §6.1
    /// Diffie-Hellman example.
    /// </summary>
    [TestMethod]
    [DataRow(
        "RFC 7748 §6.1 Alice",
        "77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a",
        "8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a")]
    [DataRow(
        "RFC 7748 §6.1 Bob",
        "5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb",
        "de9edb7d7b7dc1b4d35b61c2ece435373f8343c85b78674dadfc7e146f882b4f")]
    public void ImportPrivateKey_WhenGivenRfc7748PrivateKey_ShouldDerivePublishedPublicKey(
        string testName, string privateHex, string expectedPublicHex)
    {
        _ = testName;

        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(Convert.FromHexString(privateHex));

        CollectionAssert.AreEqual(Convert.FromHexString(expectedPublicHex), algorithm.ExportPublicKey());
    }

    /// <summary>
    /// Loads the curated Wycheproof rows whose derivation must succeed and yields them as
    /// <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per accepted KAT vector.</returns>
    private static IEnumerable<object[]> WycheproofAcceptedVectors() =>
        ReadWycheproofVectors().Where(v => !v.ExpectRejection).Select(v => new object[] { v });

    /// <summary>
    /// Loads the curated Wycheproof rows whose derivation must be rejected by the strict all-zero check and yields
    /// them as <see cref="DynamicDataAttribute" />-compatible rows.
    /// </summary>
    /// <returns>One row per rejected KAT vector.</returns>
    private static IEnumerable<object[]> WycheproofRejectedVectors() =>
        ReadWycheproofVectors().Where(v => v.ExpectRejection).Select(v => new object[] { v });

    /// <summary>
    /// Reads all curated Wycheproof vectors from the embedded resource.
    /// </summary>
    /// <returns>The parsed vectors in file order.</returns>
    /// <exception cref="InvalidOperationException">The embedded KAT resource cannot be located.</exception>
    private static List<KeyAgreementKnownAnswer> ReadWycheproofVectors()
    {
        using Stream stream = typeof(X25519Tests).Assembly.GetManifestResourceStream(WycheproofResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{WycheproofResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        return KeyAgreementKnownAnswer.Read(stream).ToList();
    }

    /// <summary>
    /// Verifies that <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" /> produces the shared secret
    /// mandated by each accepted vector in the curated Wycheproof corpus, including twist points, non-canonical
    /// encodings, and edge-case multiplication results.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WycheproofAcceptedVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveSharedSecret_WhenGivenAcceptedWycheproofVector_ShouldProduceExpectedSharedSecret(KeyAgreementKnownAnswer vector)
    {
        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(vector.PrivateKey);

        byte[] actual = algorithm.DeriveSharedSecret(vector.PeerPublicKey);

        CollectionAssert.AreEqual(vector.ExpectedSharedSecret, actual);
    }

    /// <summary>
    /// Verifies that <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" /> throws
    /// <see cref="CryptographicException" /> for each curated Wycheproof vector whose low-order peer point collapses
    /// the shared secret to zero.
    /// </summary>
    /// <param name="vector">The KAT vector under test.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WycheproofRejectedVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void DeriveSharedSecret_WhenGivenRejectedWycheproofVector_ShouldThrowCryptographicException(KeyAgreementKnownAnswer vector)
    {
        using var algorithm = new X25519();
        algorithm.ImportPrivateKey(vector.PrivateKey);

        var ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.DeriveSharedSecret(vector.PeerPublicKey);
        });

        Assert.IsNotNull(ex);
    }
}
