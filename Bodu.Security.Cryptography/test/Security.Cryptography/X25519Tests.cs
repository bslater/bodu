// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519Tests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for <see cref="X25519" />, inheriting the asymmetric and key-agreement contracts from
/// <see cref="KeyAgreementAlgorithmTests{TTest, TAlgorithm}" />. RFC 7748 and Wycheproof known-answer coverage
/// lives in the <c>KnownAnswerTests</c> partial.
/// </summary>
[TestClass]
public sealed partial class X25519Tests
    : KeyAgreementAlgorithmTests<X25519Tests, X25519>
{
    /// <inheritdoc />
    protected override int SharedSecretSizeBytes => X25519.SharedSecretSizeInBytes;

    /// <inheritdoc />
    protected override AsymmetricAlgorithmSpecification GetSpecification() =>
        new()
        {
            KeySizeDesignator = 256,
            KeyExchangeAlgorithmName = "X25519",
            SignatureAlgorithmName = null,
            PrivateKeySizeBytes = X25519.KeySizeInBytes,
            PublicKeySizeBytes = X25519.KeySizeInBytes,
        };

    /// <inheritdoc />
    protected override void GenerateKey(X25519 algorithm) =>
        algorithm.GenerateKey();

    /// <inheritdoc />
    protected override bool HasPrivateKey(X25519 algorithm) =>
        algorithm.HasPrivateKey;

    /// <inheritdoc />
    protected override bool HasPublicKey(X25519 algorithm) =>
        algorithm.HasPublicKey;

    /// <inheritdoc />
    protected override void ImportPrivateKey(X25519 algorithm, byte[] privateKey) =>
        algorithm.ImportPrivateKey(privateKey);

    /// <inheritdoc />
    protected override void ImportPublicKey(X25519 algorithm, byte[] publicKey) =>
        algorithm.ImportPublicKey(publicKey);

    /// <inheritdoc />
    protected override byte[] ExportPrivateKey(X25519 algorithm) =>
        algorithm.ExportPrivateKey();

    /// <inheritdoc />
    protected override byte[] ExportPublicKey(X25519 algorithm) =>
        algorithm.ExportPublicKey();

    /// <inheritdoc />
    protected override string GetAlgorithmName(X25519 algorithm) =>
        algorithm.AlgorithmName;

    /// <inheritdoc />
    protected override int GetSecurityStrengthBits(X25519 algorithm) =>
        algorithm.SecurityStrengthBits;

    /// <inheritdoc />
    protected override byte[] DeriveSharedSecret(X25519 algorithm, byte[] peerPublicKey) =>
        algorithm.DeriveSharedSecret(peerPublicKey);

    /// <summary>
    /// Verifies that <see cref="X25519.Create" /> returns a fresh instance with default state.
    /// </summary>
    [TestMethod]
    public void Create_WhenCalled_ShouldReturnNewInstanceWithDefaults()
    {
        using var algorithm = X25519.Create();

        Assert.IsNotNull(algorithm);
        Assert.AreEqual(256, algorithm.KeySize);
        Assert.IsFalse(algorithm.HasPrivateKey);
    }
}
