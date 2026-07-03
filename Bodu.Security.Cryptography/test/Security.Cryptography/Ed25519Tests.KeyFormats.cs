// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains Ed25519-specific known-answer tests for the RFC 8410 PKCS#8 key format, driven by
/// <see cref="RoundTripKat{TValue, TWire}" /> (raw seed ↔ DER) and <see cref="InvalidKat{TInput}" /> rows. The valid
/// vector is the published RFC 8410 §10.3 example.
/// </summary>
public sealed partial class Ed25519Tests
{
    /// <summary>
    /// Yields the valid Ed25519 PKCS#8 round-trip vector: the RFC 8410 §10.3 example seed against its DER encoding
    /// (OID 1.3.101.112, version 0).
    /// </summary>
    /// <returns>One row pairing the raw seed with its DER wire form.</returns>
    private static IEnumerable<object[]> ValidPkcs8Vectors()
    {
        yield return new object[]
        {
            new RoundTripKat<byte[], byte[]>(
                "RFC 8410 §10.3 Ed25519 PKCS#8",
                Convert.FromHexString("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842"),
                Convert.FromHexString("302e020100300506032b657004220420d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842")),
        };
    }

    /// <summary>
    /// Yields invalid Ed25519 key-format vectors that the importers must reject.
    /// </summary>
    /// <returns>One row per rejected encoding.</returns>
    private static IEnumerable<object[]> InvalidPkcs8Vectors()
    {
        yield return new object[]
        {
            new InvalidKat<byte[]>(
                "wrong algorithm OID (X25519)",
                Convert.FromHexString("302e020100300506032b656e04220420d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842"),
                typeof(CryptographicException)),
        };

        yield return new object[]
        {
            new InvalidKat<byte[]>(
                "malformed DER",
                new byte[] { 0x30, 0x05, 0x01, 0x02, 0x03 },
                typeof(CryptographicException)),
        };
    }

    /// <summary>
    /// Verifies that importing a valid Ed25519 PKCS#8 structure extracts the embedded raw seed.
    /// </summary>
    /// <param name="vector">The round-trip KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidPkcs8Vectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportPkcs8PrivateKey_WhenGivenValidVector_ShouldExtractRawSeed(RoundTripKat<byte[], byte[]> vector)
    {
        using var ed25519 = new Ed25519();
        ed25519.ImportPkcs8PrivateKey(vector.Wire, out _);

        CollectionAssert.AreEqual(vector.Value, ed25519.ExportPrivateKey());
    }

    /// <summary>
    /// Verifies that re-exporting a valid Ed25519 PKCS#8 structure reproduces the identical DER.
    /// </summary>
    /// <param name="vector">The round-trip KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidPkcs8Vectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ExportPkcs8PrivateKey_WhenSeedImportedFromValidVector_ShouldReproduceWire(RoundTripKat<byte[], byte[]> vector)
    {
        using var ed25519 = new Ed25519();
        ed25519.ImportPkcs8PrivateKey(vector.Wire, out _);

        CollectionAssert.AreEqual(vector.Wire, ed25519.ExportPkcs8PrivateKey());
    }

    /// <summary>
    /// Verifies that importing a valid Ed25519 PKCS#8 structure reports the full structure length as consumed.
    /// </summary>
    /// <param name="vector">The round-trip KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidPkcs8Vectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportPkcs8PrivateKey_WhenGivenValidVector_ShouldReportBytesRead(RoundTripKat<byte[], byte[]> vector)
    {
        using var ed25519 = new Ed25519();
        ed25519.ImportPkcs8PrivateKey(vector.Wire, out int bytesRead);

        Assert.AreEqual(vector.Wire.Length, bytesRead);
    }

    /// <summary>
    /// Verifies that importing an invalid Ed25519 PKCS#8 structure throws the row's expected exception.
    /// </summary>
    /// <param name="vector">The invalid KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidPkcs8Vectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportPkcs8PrivateKey_WhenGivenInvalidVector_ShouldThrowExpectedException(InvalidKat<byte[]> vector)
    {
        using var ed25519 = new Ed25519();

        ExceptionAssert.AssertGuard(
            vector.Name,
            () => ed25519.ImportPkcs8PrivateKey(vector.Input, out _),
            vector.ExceptionType,
            vector.ParamName);
    }

    /// <summary>
    /// The canonical RFC 7468 PKCS#8 PEM encoding of the RFC 8410 §10.3 Ed25519 private key — the DER of
    /// <see cref="ValidPkcs8Vectors" /> Base64-wrapped under the <c>PRIVATE KEY</c> label.
    /// </summary>
    private const string Rfc8410Pkcs8Pem =
        "-----BEGIN PRIVATE KEY-----\n" +
        "MC4CAQAwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC\n" +
        "-----END PRIVATE KEY-----";

    /// <summary>
    /// Verifies that exporting the imported RFC 8410 §10.3 private key as PKCS#8 PEM produces the exact canonical
    /// RFC 7468 text (correct label, 64-column Base64 body).
    /// </summary>
    [TestMethod]
    public void ExportPkcs8PrivateKeyPem_WhenImportedFromValidVector_ShouldProduceCanonicalPem()
    {
        using var ed25519 = new Ed25519();
        ed25519.ImportPkcs8PrivateKey(
            Convert.FromHexString("302e020100300506032b657004220420d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842"),
            out _);

        Assert.AreEqual(Rfc8410Pkcs8Pem, ed25519.ExportPkcs8PrivateKeyPem());
    }

    /// <summary>
    /// Verifies that importing the canonical PKCS#8 PEM through the inherited <c>ImportFromPem</c> recovers the raw
    /// seed, confirming the PEM round-trip layered over the DER key format.
    /// </summary>
    [TestMethod]
    public void ImportFromPem_WhenGivenPkcs8Pem_ShouldRecoverRawSeed()
    {
        using var ed25519 = new Ed25519();
        ed25519.ImportFromPem(Rfc8410Pkcs8Pem);

        CollectionAssert.AreEqual(
            Convert.FromHexString("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842"),
            ed25519.ExportPrivateKey());
    }
}
