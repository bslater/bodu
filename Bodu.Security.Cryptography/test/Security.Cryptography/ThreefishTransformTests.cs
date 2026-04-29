// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Concrete test class that exercises the <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> base tests
/// against the <see cref="ThreefishTransform" /> implementation.
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
    /// Flattens both <see cref="TweakableBlockCipherVariant" /> values for the 256-bit Threefish block size,
    /// the size targeted by <see cref="ThreefishTransform" />.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<TweakableBlockCipherVariant>().SelectMany(Threefish256KnownAnswers.For);

    /// <inheritdoc />
    protected override ThreefishTransform CreateTransformForKnownAnswer(BlockCipherKnownAnswer answer, bool forEncryption)
    {
        var algorithm = new Threefish256
        {
            Mode = CipherMode.ECB,
            BlockMode = CipherBlockMode.ECB,
            Padding = PaddingMode.None,
        };
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];
        algorithm.Tweak = answer.Tweak!;

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (ThreefishTransform)transform;
    }
}
