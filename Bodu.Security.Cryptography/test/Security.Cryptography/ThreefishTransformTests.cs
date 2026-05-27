// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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

    /// <inheritdoc />
    /// <remarks>
    /// Aggregates the curated Threefish-256, Threefish-512, and Threefish-1024 KAT data sets — flattening both
    /// <see cref="TweakableBlockCipherVariant" /> rows for each size — so every Threefish block size is exercised
    /// at the transform tier through the same vector pipeline.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<TweakableBlockCipherVariant>()
            .SelectMany(variant =>
                Threefish256KnownAnswers.For(variant)
                    .Concat(Threefish512KnownAnswers.For(variant))
                    .Concat(Threefish1024KnownAnswers.For(variant)));

    /// <inheritdoc />
    protected override ThreefishTransform CreateTransformForKnownAnswer(BlockCipherKnownAnswer answer, bool forEncryption)
    {
        Threefish algorithm = answer.Plaintext.Length switch
        {
            32 => new Threefish256(),
            64 => new Threefish512(),
            128 => new Threefish1024(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(answer),
                answer.Plaintext.Length,
                "Threefish KAT plaintext length must be 32, 64, or 128 bytes."),
        };

        algorithm.Mode = CipherMode.ECB;
        algorithm.BlockMode = CipherModeKind.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        algorithm.Tweak = answer.Tweak!;

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (ThreefishTransform)transform;
    }

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
