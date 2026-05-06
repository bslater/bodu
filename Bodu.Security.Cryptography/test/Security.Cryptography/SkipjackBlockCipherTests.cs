// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

[TestClass]
internal sealed partial class SkipjackBlockCipherTests
    : BlockCipherTests<SkipjackBlockCipherTests, SkipjackBlockCipher, SingleTestVariant>
{
    /// <inheritdocs/>
    protected override BlockCipherSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        BlockSize = 8,
        KeySize = 10,
        TestKey = TestHelpers.GenerateIncrementalByteSequence(0, 10),
    };

    /// <inheritdocs/>
    protected override SkipjackBlockCipher CreateBlockCipher(SingleTestVariant variant)
    {
        var specification = GetSpecification(variant);
        return new SkipjackBlockCipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(SingleTestVariant variant) =>
        SkipjackKnownAnswers.For(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new SkipjackBlockCipher(answer.Key!);
}
