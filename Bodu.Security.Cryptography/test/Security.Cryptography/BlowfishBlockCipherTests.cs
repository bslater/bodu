// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishBlockCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    [TestClass]
    internal sealed partial class BlowfishBlockCipherTests
        : BlockCipherTests<BlowfishBlockCipherTests, BlowfishBlockCipher, SingleTestVariant>
    {
        /// <inerhitdocs/>
        protected override BlockCipherSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            BlockSize = 8,
            KeySize = 8,
            TestKey = CryptoTestUtilities.CreateIncrementalByteSequence(0, 8),
        };

        /// <inerhitdocs/>
        public override IEnumerable<SingleTestVariant> GetBlockCipherVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inerhitdocs/>
        protected override BlowfishBlockCipher CreateBlockCipher(SingleTestVariant variant)
        {
            var specification = GetSpecification(variant);
            return new BlowfishBlockCipher(specification.TestKey);
        }

        /// <inerhitdocs/>
        protected override IEnumerable<KnownAnswerTest> GetKnownAnswerTests(SingleTestVariant variant)
        {
            // Published test vectors from Eric Young, as listed on Schneier's website:
            // https://www.schneier.com/wp-content/uploads/2015/12/vectors-2.txt
            // All assume standard 16-round Blowfish ECB mode.

            yield return new KnownAnswerTest
            {
                Name = "Blowfish / key=0000000000000000",
                Input = Convert.FromHexString("0000000000000000"),
                ExpectedOutput = Convert.FromHexString("4EF997456198DD78"),
                CipherFactory = () => new BlowfishBlockCipher(Convert.FromHexString("0000000000000000")),
            };
            yield return new KnownAnswerTest
            {
                Name = "Blowfish / key=FFFFFFFFFFFFFFFF",
                Input = Convert.FromHexString("FFFFFFFFFFFFFFFF"),
                ExpectedOutput = Convert.FromHexString("51866FD5B85ECB8A"),
                CipherFactory = () => new BlowfishBlockCipher(Convert.FromHexString("FFFFFFFFFFFFFFFF")),
            };
            yield return new KnownAnswerTest
            {
                Name = "Blowfish / key=0123456789ABCDEF",
                Input = Convert.FromHexString("1111111111111111"),
                ExpectedOutput = Convert.FromHexString("61F9C3802281B096"),
                CipherFactory = () => new BlowfishBlockCipher(Convert.FromHexString("0123456789ABCDEF")),
            };
            yield return new KnownAnswerTest
            {
                Name = "Blowfish / key=1111111111111111",
                Input = Convert.FromHexString("0123456789ABCDEF"),
                ExpectedOutput = Convert.FromHexString("7D0CC630AFDA1EC7"),
                CipherFactory = () => new BlowfishBlockCipher(Convert.FromHexString("1111111111111111")),
            };
            yield return new KnownAnswerTest
            {
                Name = "Blowfish / key=FEDCBA9876543210",
                Input = Convert.FromHexString("0123456789ABCDEF"),
                ExpectedOutput = Convert.FromHexString("0ACEAB0FC6A0A28D"),
                CipherFactory = () => new BlowfishBlockCipher(Convert.FromHexString("FEDCBA9876543210")),
            };
            yield return new KnownAnswerTest
            {
                Name = "Blowfish / key=7CA110454A1A6E57",
                Input = Convert.FromHexString("01A1D6D039776742"),
                ExpectedOutput = Convert.FromHexString("59C68245EB05282B"),
                CipherFactory = () => new BlowfishBlockCipher(Convert.FromHexString("7CA110454A1A6E57")),
            };
        }
    }
}