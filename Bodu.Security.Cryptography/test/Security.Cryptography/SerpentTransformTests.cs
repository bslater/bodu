// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentTransformTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests against the
/// wide-block tweakable <see cref="SerpentTransform" /> implementation, distinct from the standardised 128-bit
/// <see cref="Serpent128Transform" /> covered by <see cref="Serpent128TransformTests" />. KAT coverage for the wide-block
/// variants is provided at the block-cipher tier by <see cref="Serpent256CipherTests" /> /
/// <see cref="Serpent512CipherTests" /> / <see cref="Serpent1024CipherTests" />, whose rows are cross-validated against
/// <c>tools/cipher-vectors/wide_serpent.py</c>; this transform-tier test class therefore only exercises the behavioural
/// surface (construction, raw-block transform, disposal).
/// </summary>
[TestClass]
internal sealed class SerpentTransformTests
    : BlockCipherTransformTests<SerpentTransformTests, SerpentTransform>
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly byte[] _tweak;

    /// <summary>
    /// Initializes a new instance with a freshly generated key, IV, and tweak cached so that
    /// <see cref="CreateEncryptor" /> and <see cref="CreateDecryptor" /> produce paired transforms.
    /// </summary>
    public SerpentTransformTests()
    {
        using var seed = new Serpent256();
        seed.GenerateKey();
        seed.GenerateIV();
        seed.GenerateTweak();
        _key = seed.Key;
        _iv = seed.IV;
        _tweak = seed.Tweak;
    }

    /// <inheritdoc />
    protected override SerpentTransform CreateAlgorithm() => CreateEncryptor();

    /// <inheritdoc />
    protected override SerpentTransform CreateEncryptor() => BuildTransform(forEncryption: true);

    /// <inheritdoc />
    protected override SerpentTransform CreateDecryptor() => BuildTransform(forEncryption: false);

    private SerpentTransform BuildTransform(bool forEncryption)
    {
        var algorithm = new Serpent256
        {
            Mode = CipherMode.ECB,
            BlockMode = CipherModeKind.ECB,
            Padding = PaddingMode.PKCS7,
            Key = _key,
            IV = _iv,
            Tweak = _tweak,
        };

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (SerpentTransform)transform;
    }
}
