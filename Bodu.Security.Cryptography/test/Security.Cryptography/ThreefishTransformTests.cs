// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests against the
/// <see cref="ThreefishTransform" /> implementation. The contract suite runs against <see cref="Threefish256" /> — the smallest
/// of the three Threefish variants — because every variant shares the same transform type and the contract behaviour is
/// independent of block size. Crypto-correctness for all three variants is anchored at the block-cipher tier through
/// <see cref="Threefish256CipherTests" /> / <see cref="Threefish512CipherTests" /> / <see cref="Threefish1024CipherTests" />.
/// </summary>
[TestClass]
internal sealed class ThreefishTransformTests
    : BlockCipherTransformTests<ThreefishTransformTests, ThreefishTransform>
{
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly byte[] _tweak;

    /// <summary>
    /// Initializes a new instance with a freshly generated key, IV, and tweak cached so that
    /// <see cref="CreateEncryptor" /> and <see cref="CreateDecryptor" /> produce paired transforms.
    /// </summary>
    public ThreefishTransformTests()
    {
        using var seed = new Threefish256();
        seed.GenerateKey();
        seed.GenerateIV();
        seed.GenerateTweak();
        _key = seed.Key;
        _iv = seed.IV;
        _tweak = seed.Tweak;
    }

    /// <inheritdoc />
    protected override ThreefishTransform CreateAlgorithm() => CreateEncryptor();

    /// <inheritdoc />
    protected override ThreefishTransform CreateEncryptor() => BuildTransform(forEncryption: true);

    /// <inheritdoc />
    protected override ThreefishTransform CreateDecryptor() => BuildTransform(forEncryption: false);

    private ThreefishTransform BuildTransform(bool forEncryption)
    {
        var algorithm = new Threefish256
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
        return (ThreefishTransform)transform;
    }
}
