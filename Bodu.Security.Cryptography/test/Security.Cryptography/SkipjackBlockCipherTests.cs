// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    internal sealed partial class SkipjackBlockCipherTests
        : BlockCipherTests<SkipjackBlockCipherTests, SkipjackBlockCipher, SingleTestVariant>
    {
        /// <inheritdocs/>
        protected override BlockCipherSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            BlockSize = 8,
            KeySize = 10,
            TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0, 10),
        };

        /// <inheritdocs/>
        public override IEnumerable<SingleTestVariant> GetBlockCipherVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdocs/>
        protected override SkipjackBlockCipher CreateBlockCipher(SingleTestVariant variant)
        {
            var specification = GetSpecification(variant);
            return new SkipjackBlockCipher(specification.TestKey);
        }

        /// <inheritdocs/>
        protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SingleTestVariant variant)
        {
            yield return new KnownAnswerTest
            {
                Name = "All-zero plaintext / key bit 0 set",
                Input = Convert.FromHexString("0000000000000000"),
                ExpectedOutput = Convert.FromHexString("E378FE4157A66452"),
                CipherFactory = () => new SkipjackBlockCipher(Convert.FromHexString("80000000000000000000")),
            };

            yield return new KnownAnswerTest
            {
                Name = "All-zero plaintext / key bit 1 set",
                Input = Convert.FromHexString("0000000000000000"),
                ExpectedOutput = Convert.FromHexString("61CE4785762E8980"),
                CipherFactory = () => new SkipjackBlockCipher(Convert.FromHexString("40000000000000000000")),
            };

            yield return new KnownAnswerTest
            {
                Name = "All-zero plaintext / key byte 1 high bit set",
                Input = Convert.FromHexString("0000000000000000"),
                ExpectedOutput = Convert.FromHexString("F76307829359FC11"),
                CipherFactory = () => new SkipjackBlockCipher(Convert.FromHexString("00800000000000000000")),
            };
        }
    }
}