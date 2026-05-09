// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    /// <inheritdoc />
    protected override CamelliaTransform CreateAlgorithm()
    {
        var algorithm = new Camellia();
        algorithm.GenerateKey();
        algorithm.GenerateIV();
        return (CamelliaTransform)algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Flattens the per-key-size RFC 3713 Appendix A vectors across <see cref="BlockCipherKeyVariant.Key128" />,
    /// <see cref="BlockCipherKeyVariant.Key192" />, and <see cref="BlockCipherKeyVariant.Key256" /> so each runs
    /// through the Transform-layer harness in turn.
    /// </remarks>
    protected override IEnumerable<BlockCipherKnownAnswer> GetKnownAnswers() =>
        Enum.GetValues<BlockCipherKeyVariant>().SelectMany(CamelliaKnownAnswers.For);

    /// <inheritdoc />
    protected override CamelliaTransform CreateTransformForKnownAnswer(BlockCipherKnownAnswer answer, bool forEncryption)
    {
        var algorithm = new Camellia
        {
            Mode = CipherMode.ECB,
            Padding = PaddingMode.None,
        };
        algorithm.Key = answer.Key!;
        algorithm.IV = new byte[algorithm.BlockSize / 8];

        ICryptoTransform transform = forEncryption
            ? algorithm.CreateEncryptor()
            : algorithm.CreateDecryptor();
        return (CamelliaTransform)transform;
    }
}
