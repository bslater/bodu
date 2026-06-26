// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.KeyFormats.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains X25519-specific known-answer tests for the RFC 8410 SubjectPublicKeyInfo key format, driven by
/// <see cref="RoundTripKat{TValue, TWire}" /> (raw key ↔ DER) and <see cref="InvalidKat{TInput}" /> rows.
/// </summary>
public sealed partial class X25519Tests
{
    /// <summary>
    /// Yields the valid X25519 SubjectPublicKeyInfo round-trip vector: the RFC 7748 §6.1 Alice public key against its
    /// RFC 8410 (OID 1.3.101.110) DER encoding.
    /// </summary>
    /// <returns>One row pairing the raw public key with its DER wire form.</returns>
    private static IEnumerable<object[]> ValidSubjectPublicKeyInfoVectors()
    {
        yield return new object[]
        {
            new RoundTripKat<byte[], byte[]>(
                "RFC 8410 X25519 SPKI (RFC 7748 Alice key)",
                Convert.FromHexString("8520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a"),
                Convert.FromHexString("302a300506032b656e0321008520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a")),
        };
    }

    /// <summary>
    /// Yields invalid X25519 SubjectPublicKeyInfo vectors that the importer must reject.
    /// </summary>
    /// <returns>One row per rejected encoding.</returns>
    private static IEnumerable<object[]> InvalidSubjectPublicKeyInfoVectors()
    {
        yield return new object[]
        {
            new InvalidKat<byte[]>(
                "wrong algorithm OID (Ed25519)",
                Convert.FromHexString("302a300506032b65700321008520f0098930a754748b7ddcb43ef75a0dbf3a0d26381af4eba4a98eaa9b4e6a"),
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
    /// Verifies that importing a valid X25519 SubjectPublicKeyInfo extracts the embedded raw public key.
    /// </summary>
    /// <param name="vector">The round-trip KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidSubjectPublicKeyInfoVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportSubjectPublicKeyInfo_WhenGivenValidVector_ShouldExtractRawPublicKey(RoundTripKat<byte[], byte[]> vector)
    {
        using var x25519 = new X25519();
        x25519.ImportSubjectPublicKeyInfo(vector.Wire, out _);

        CollectionAssert.AreEqual(vector.Value, x25519.ExportPublicKey());
    }

    /// <summary>
    /// Verifies that re-exporting a valid X25519 SubjectPublicKeyInfo reproduces the identical DER.
    /// </summary>
    /// <param name="vector">The round-trip KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidSubjectPublicKeyInfoVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ExportSubjectPublicKeyInfo_WhenKeyImportedFromValidVector_ShouldReproduceWire(RoundTripKat<byte[], byte[]> vector)
    {
        using var x25519 = new X25519();
        x25519.ImportSubjectPublicKeyInfo(vector.Wire, out _);

        CollectionAssert.AreEqual(vector.Wire, x25519.ExportSubjectPublicKeyInfo());
    }

    /// <summary>
    /// Verifies that importing a valid X25519 SubjectPublicKeyInfo reports the full structure length as consumed.
    /// </summary>
    /// <param name="vector">The round-trip KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(ValidSubjectPublicKeyInfoVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportSubjectPublicKeyInfo_WhenGivenValidVector_ShouldReportBytesRead(RoundTripKat<byte[], byte[]> vector)
    {
        using var x25519 = new X25519();
        x25519.ImportSubjectPublicKeyInfo(vector.Wire, out int bytesRead);

        Assert.AreEqual(vector.Wire.Length, bytesRead);
    }

    /// <summary>
    /// Verifies that importing an invalid X25519 SubjectPublicKeyInfo throws the row's expected exception.
    /// </summary>
    /// <param name="vector">The invalid KAT vector under test.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidSubjectPublicKeyInfoVectors), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ImportSubjectPublicKeyInfo_WhenGivenInvalidVector_ShouldThrowExpectedException(InvalidKat<byte[]> vector)
    {
        using var x25519 = new X25519();

        ExceptionAssert.AssertGuard(
            vector.Name,
            () => x25519.ImportSubjectPublicKeyInfo(vector.Input, out _),
            vector.ExceptionType,
            vector.ParamName);
    }
}
