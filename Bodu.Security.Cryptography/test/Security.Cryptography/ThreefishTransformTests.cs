// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests
/// against the <see cref="ThreefishTransform" /> implementation across all three Threefish block sizes (256, 512, and
/// 1024 bits). The transform type is shared across the three engines, so a single test class anchored on the curated
/// KAT data files for each size is the canonical coverage shape.
/// </summary>
[TestClass]
internal sealed class ThreefishTransformTests
    : BlockCipherTransformTests<ThreefishTransformTests, ThreefishTransform>
{
    /// <inheritdoc />
    protected override ThreefishTransform CreateAlgorithm()
    {
        var algorithm = new Threefish256();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        algorithm.GenerateTweak();
        return (ThreefishTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV, algorithm.Tweak);
    }

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
        algorithm.BlockMode = CipherBlockMode.ECB;
        algorithm.Padding = PaddingMode.None;
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        algorithm.Tweak = answer.Tweak!;

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (ThreefishTransform)transform;
    }
}
