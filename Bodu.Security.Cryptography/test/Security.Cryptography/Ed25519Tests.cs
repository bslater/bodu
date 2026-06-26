// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="Ed25519" />, inheriting the asymmetric and signature contracts from
/// <see cref="SignatureAlgorithmTests{TTest, TAlgorithm}" />. RFC 8032 and Wycheproof known-answer coverage lives
/// in the <c>KnownAnswerTests</c> partial.
/// </summary>
[TestClass]
public sealed partial class Ed25519Tests
    : SignatureAlgorithmTests<Ed25519Tests, Ed25519>
{
    /// <inheritdoc />
    protected override int SignatureSizeBytes => Ed25519.SignatureSizeInBytes;

    /// <inheritdoc />
    protected override AsymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            KeySizeDesignator = 256,
            KeyExchangeAlgorithmName = null,
            SignatureAlgorithmName = "Ed25519",
            PrivateKeySizeBytes = Ed25519.PrivateKeySizeInBytes,
            PublicKeySizeBytes = Ed25519.PublicKeySizeInBytes,
        };

    /// <inheritdoc />
    protected override void GenerateKey(Ed25519 algorithm) =>
        algorithm.GenerateKey();

    /// <inheritdoc />
    protected override bool HasPrivateKey(Ed25519 algorithm) =>
        algorithm.HasPrivateKey;

    /// <inheritdoc />
    protected override bool HasPublicKey(Ed25519 algorithm) =>
        algorithm.HasPublicKey;

    /// <inheritdoc />
    protected override void ImportPrivateKey(Ed25519 algorithm, byte[] privateKey) =>
        algorithm.ImportPrivateKey(privateKey);

    /// <inheritdoc />
    protected override void ImportPublicKey(Ed25519 algorithm, byte[] publicKey) =>
        algorithm.ImportPublicKey(publicKey);

    /// <inheritdoc />
    protected override byte[] ExportPrivateKey(Ed25519 algorithm) =>
        algorithm.ExportPrivateKey();

    /// <inheritdoc />
    protected override byte[] ExportPublicKey(Ed25519 algorithm) =>
        algorithm.ExportPublicKey();

    /// <inheritdoc />
    protected override string GetAlgorithmName(Ed25519 algorithm) =>
        algorithm.AlgorithmName;

    /// <inheritdoc />
    protected override int GetSecurityStrengthBits(Ed25519 algorithm) =>
        algorithm.SecurityStrengthBits;

    /// <inheritdoc />
    protected override byte[] SignData(Ed25519 algorithm, byte[] data) =>
        algorithm.SignData(data);

    /// <inheritdoc />
    protected override bool VerifyData(Ed25519 algorithm, byte[] data, byte[] signature) =>
        algorithm.VerifyData(data, signature);

    /// <summary>
    /// Verifies that <see cref="Ed25519.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using var algorithm = Ed25519.Create();

        Assert.IsNotNull(algorithm);
        Assert.AreEqual(256, algorithm.KeySize);
        Assert.IsFalse(algorithm.HasPrivateKey);
    }
}
