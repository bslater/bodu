// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishTransformTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests
/// against the <see cref="BlowfishTransform" /> implementation.
/// </summary>
[TestClass]
internal sealed class BlowfishTransformTests
    : BlockCipherTransformTests<BlowfishTransformTests, BlowfishTransform>
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Initializes a new instance with a freshly generated key and IV cached so that
    /// <see cref="CreateEncryptor" /> and <see cref="CreateDecryptor" /> produce paired transforms.
    /// </summary>
    public BlowfishTransformTests()
    {
        using var seed = new Blowfish();
        seed.GenerateKey();
        seed.GenerateIV();
        _key = seed.Key;
        _iv = seed.IV;
    }

    /// <inheritdoc />
    protected override BlowfishTransform CreateAlgorithm() => CreateEncryptor();

    /// <inheritdoc />
    protected override BlowfishTransform CreateEncryptor() => BuildTransform(forEncryption: true);

    /// <inheritdoc />
    protected override BlowfishTransform CreateDecryptor() => BuildTransform(forEncryption: false);

    private BlowfishTransform BuildTransform(bool forEncryption)
    {
        var algorithm = new Blowfish
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7,
            Key = _key,
            IV = _iv,
        };

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (BlowfishTransform)transform;
    }
}
