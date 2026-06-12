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
/// Locks <see cref="X25519" /> against the RFC 7748 §6.1 Diffie-Hellman example and the curated Project Wycheproof
/// X25519 vector corpus embedded in the test assembly.
/// </summary>
public partial class X25519Tests
{
    /// <summary>
    /// Resource name of the embedded curated Wycheproof KAT file shipped with this test assembly.
    /// </summary>
    private const string WycheproofResourceName = "Bodu.Security.Cryptography.X25519.Wycheproof.txt";

    /// <summary>
    /// Verifies that <see cref="X25519.DeriveSharedSecret(ReadOnlySpan{byte})" /> reproduces the shared secret of
    /// the RFC 7748 §6.1 Diffie-Hellman example from both parties' perspectives.
    /// </summary>
    [TestMethod]
    public void DeriveSharedSecret_WhenGivenRfc7748Example_ShouldProducePublishedSharedSecret()
    {
        var expected = Convert.FromHexString("4a5d9d5ba4ce2de1728e3bf480350f25e07e21c947d19e3376f09b3c1e161742");

        using var alice = new X25519();
        using var bob = new X25519();
        alice.ImportPrivateKey(Convert.FromHexString("77076d0a7318a57d3c16c17251b26645df4c2f87ebc0992ab177fba51db92c2a"));
        bob.ImportPrivateKey(Convert.FromHexString("5dab087e624a8a4b79e17f8b83800ee66f3bb1292618b6fd1c2f8b27ff88e0eb"));

        CollectionAssert.AreEqual(expected, alice.DeriveSharedSecret(bob.ExportPublicKey()));
        CollectionAssert.AreEqual(expected, bob.DeriveSharedSecret(alice.ExportPublicKey()));
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

        var actual = algorithm.DeriveSharedSecret(vector.PeerPublicKey);

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
