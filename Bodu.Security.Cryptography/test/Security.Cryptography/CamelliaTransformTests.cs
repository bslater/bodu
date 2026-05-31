// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Exercises the <see cref="CamelliaTransform" /> implementation against the shared
/// <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> suite.
/// </summary>
[TestClass]
internal sealed class CamelliaTransformTests
    : BlockCipherTransformTests<CamelliaTransformTests, CamelliaTransform>
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Initializes a new instance with a freshly generated key and IV cached so that
    /// <see cref="CreateEncryptor" /> and <see cref="CreateDecryptor" /> produce paired transforms.
    /// </summary>
    public CamelliaTransformTests()
    {
        using var seed = new Camellia();
        seed.GenerateKey();
        seed.GenerateIV();
        _key = seed.Key;
        _iv = seed.IV;
    }

    /// <inheritdoc />
    protected override CamelliaTransform CreateAlgorithm() => CreateEncryptor();

    /// <inheritdoc />
    protected override CamelliaTransform CreateEncryptor() => BuildTransform(forEncryption: true);

    /// <inheritdoc />
    protected override CamelliaTransform CreateDecryptor() => BuildTransform(forEncryption: false);

    private CamelliaTransform BuildTransform(bool forEncryption)
    {
        var algorithm = new Camellia
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7,
            Key = _key,
            IV = _iv,
        };

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (CamelliaTransform)transform;
    }
}
