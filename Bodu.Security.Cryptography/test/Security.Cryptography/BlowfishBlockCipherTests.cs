// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishBlockCipherTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

[TestClass]
internal sealed partial class BlowfishBlockCipherTests
    : BlockCipherTests<BlowfishBlockCipherTests, BlowfishBlockCipher, SingleTestVariant>
{
    /// <inerhitdocs/>
    protected override BlockCipherSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        BlockSize = 8,
        KeySize = 8,
        TestKey = TestHelpers.GenerateIncrementalByteSequence(0, 8),
    };

    /// <inerhitdocs/>
    protected override BlowfishBlockCipher CreateBlockCipher(SingleTestVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        return new BlowfishBlockCipher(specification.TestKey);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<BlockCipherKnownAnswer> GetKnownAnswers(SingleTestVariant variant) =>
        KnownAnswersFor(variant);

    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new BlowfishBlockCipher(answer.Key!);
}
