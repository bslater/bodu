// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128TransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests against the
/// canonical <see cref="Serpent128Transform" /> implementation.
/// </summary>
[TestClass]
internal sealed class Serpent128TransformTests
    : BlockCipherTransformTests<Serpent128TransformTests, Serpent128Transform>
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Initializes a new instance with a freshly generated key and IV cached so that
    /// <see cref="CreateEncryptor" /> and <see cref="CreateDecryptor" /> produce paired transforms.
    /// </summary>
    public Serpent128TransformTests()
    {
        using var seed = new Serpent128();
        seed.GenerateKey();
        seed.GenerateIV();
        _key = seed.Key;
        _iv = seed.IV;
    }

    /// <inheritdoc />
    protected override Serpent128Transform CreateAlgorithm() => CreateEncryptor();

    /// <inheritdoc />
    protected override Serpent128Transform CreateEncryptor() => BuildTransform(forEncryption: true);

    /// <inheritdoc />
    protected override Serpent128Transform CreateDecryptor() => BuildTransform(forEncryption: false);

    /// <inheritdoc />
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Serpent128KnownAnswers.For(SingleTestVariant.Default);

    /// <inheritdoc />
    protected override Serpent128Transform CreateTransformForKnownAnswer(BlockCipherKnownAnswer answer, bool forEncryption)
    {
        var algorithm = new Serpent128
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None,
        };
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (Serpent128Transform)transform;
    }

    private Serpent128Transform BuildTransform(bool forEncryption)
    {
        var algorithm = new Serpent128
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.PKCS7,
            Key = _key,
            IV = _iv,
        };

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (Serpent128Transform)transform;
    }
}
