// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class SkipjackBlockCipherTests
        : BlockCipherTests<SkipjackBlockCipherTests, SkipjackBlockCipher, SingleTestVariant>
    {
        protected override int ExpectedBlockSize => 8;

        public override IEnumerable<SingleTestVariant> GetBlockCipherVariants() => new[]
        {
            SingleTestVariant.Default
        };

        protected override SkipjackBlockCipher CreateBlockCipher(SingleTestVariant variant)
        {
            byte[] key = new byte[10];
            CryptoHelpers.FillWithRandomNonZeroBytes(key);
            return new SkipjackBlockCipher(key);
        }

        protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SingleTestVariant variant) =>
            Array.Empty<KnownAnswerTest>();
    }
}
