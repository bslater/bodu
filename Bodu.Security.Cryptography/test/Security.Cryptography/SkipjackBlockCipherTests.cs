// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        KnownAnswers = KnownAnswersFor(variant),
    };

    /// <inheritdocs/>
    protected override SkipjackBlockCipher CreateBlockCipher(SingleTestVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        return new SkipjackBlockCipher(specification.TestKey);
    }


    /// <inheritdoc />
    protected override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        new SkipjackBlockCipher(answer.Key!);
}
