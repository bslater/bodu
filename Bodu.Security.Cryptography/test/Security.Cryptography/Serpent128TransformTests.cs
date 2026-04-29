// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128TransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    /// <inheritdoc />
    protected override Serpent128Transform CreateAlgorithm()
    {
        var algorithm = new Serpent128();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (Serpent128Transform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flattens both <see cref="TweakableBlockCipherVariant" /> values; only
    /// <see cref="TweakableBlockCipherVariant.DefaultKeyAndTweak" /> contributes a published Bouncy Castle
    /// vector — the zeroed variant returns an empty list and is silently skipped by the harness.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<TweakableBlockCipherVariant>().SelectMany(Serpent128KnownAnswers.For);

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
}
