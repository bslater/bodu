// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;
using System.Security.Cryptography;
using System.Text;
using static Bodu.Security.Cryptography.Poly1305Tests;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Poly1305" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Poly1305Tests
        : KeyedHashAlgorithmTests<Poly1305Tests, Poly1305, SingleTestVariant>
    {
        protected override int ExpectedKeySize => 32;

        /// <inheritdoc />
        protected override byte[] GenerateUniqueKey()
        {
            byte[] key = new byte[ExpectedKeySize];
            CryptoHelpers.FillWithRandomNonZeroBytes(key);
            return key;
        }

        /// <inheritdoc />
        protected override int ExpectedInputBlockSize => 16;

        /// <inheritdoc />
        protected override int ExpectedOutputBlockSize => 16;

        /// <inheritdoc />
        protected override int MaximumLegalKeyLength => ExpectedKeySize;

        /// <inheritdoc />
        protected override int MinimumLegalKeyLength => ExpectedKeySize;

        /// <inheritdoc />
        protected override IReadOnlyList<int> ValidKeyLengths => new[] { ExpectedKeySize };

        [TestMethod]
        public void Poly1305_WhenUsingRfcTestVector_ShouldMatch()
        {
            var key = Convert.FromHexString("85D6BE7857556D337F4452FE42D506A80103808AFB0DB2FD4ABFF6AF4149F51B");
            var message = Encoding.ASCII.GetBytes("Cryptographic Forum Research Group");
            var expected = Convert.FromHexString("A8061DC1305136C6C22B8BAF0C0127A9");

            using var poly = new Poly1305 { Key = key };
            var actual = poly.ComputeHash(message);

            Console.WriteLine("Actual   : " + Convert.ToHexString(actual));
            Console.WriteLine("Expected : " + Convert.ToHexString(expected));

            CollectionAssert.AreEqual(expected, actual);
        }

        protected override Poly1305 CreateAlgorithm(SingleTestVariant variant) => this.CreateAlgorithm();

        private static readonly IReadOnlyDictionary<string, byte[]> CustomInputs = new Dictionary<string, byte[]>
        {
        };

        protected override IEnumerable<KnownAnswerTest> GetTestVectors(SingleTestVariant variant)
        {
            foreach (var vector in base.GetTestVectors(variant))
                yield return vector;

            var expected = GetExpectedHashesForNamedInputs(variant);
            foreach (var (name, input) in CustomInputs)
            {
                if (expected.TryGetValue(name, out var hex))
                {
                    yield return new KnownAnswerTest
                    {
                        Name = name,
                        Input = input,
                        ExpectedOutput = Convert.FromHexString(hex)
                    };
                }
            }
        }

        protected override IEnumerable<string> GetFieldsToExcludeFromDisposeValidation()
        {
            var list = new List<string>(base.GetFieldsToExcludeFromDisposeValidation());
            list.AddRange([
                "BlockSizeBytes"
            ]);

            return list;
        }

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) =>
             new Dictionary<string, string>
             {
                 ["Empty"] = "101112131415161718191A1B1C1D1E1F",
                 ["ABC"] = "5315EE9D662FF8C089521BE4AC753E07",
                 ["Zeros_16"] = "5092D416599BDD1F62A4E6286BADEF31",
                 ["QuickBrownFox"] = "83E7092E4BFAE6BD64E6AD70EF279E1B",
                 ["Sequential_0_255"] = "4816CA3A2F556AFDF5D1A395518DA0FE",
             };

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
             new[]
                {
                    "101112131415161718191A1B1C1D1E1F",
                    "1F11131517191B1D1F21232527292B2D",
                    "F3231316191C1F2225282B2E3134373A",
                    "560826171C21262B30353A3F44494E53",
                    "C33B1D2A1E262E363E464E565E666E76",
                    "BF3B2234312935414D5965717D8995A1",
                    "C085B20A4D3C394A5B6C7D8E9FB0C1D2",
                    "4B974B2BF66B4C4F667D94ABC2D9F007",
                    "DBED6A13A7E58E626C8AA8C6E402213F",
                    "EB068E40DE26DAB77F91B7DD032A5076",
                    "F65F323019ADABD4E7A4BFEE1D4D7CAB",
                    "8176D55FD5F58036D61FD3F7306AA3DC",
                    "02C8F44C907ED75AC8DF600B3B7FC307",
                    "FED10D75C7C42CBF3B62F2AB4E8ADA2A",
                    "F0119E55F845FEE0AD24050F029EE643",
                    "5305236CA07FC93D9CA416B23664FA50",
                    "A2291A363DEF0B53845FA4126A6AD364",
                    "F735C97F7308FD79222447FE76A96872",
                    "F749D9A0A54B51DF98ABDFA731754560",
                    "725BEDB1C880983804296F49E53A1D4A",
                    "E8A711C6DAA5D083629AF3E08FF8ED2D",
                    "D4AC2FFDEEB8F7BEB1FD6A6C2FACB509",
                    "B1E7C4EC38CD0BE8EF50D3E9C15372DB",
                    "04D64E12F92920FD1A922A5745ED21A1",
                    "48F54AEBAFBC8F1131BF6EB2B776C258",
                    "F8C236F5D702F49345D69DF916EE5100",
                    "94BC8FADEF79CAC9DAEAB52A6151CE95",
                    "925FD391749F9030E292CA43949E3517",
                    "7C297F1FE4F0C345D96B8558AED38582",
                    "C89710D4BBEBE1863DF32F26C3EEBCD5",
                    "F127052D790D68718CA647A2A303D90E",
                    "7C57DAA799D3D38243034A4AF1F6ED2B",
                    "E4A30DC29ABBA238E086B49B2916F440",
                    "45B320CBEFF5D7B485F3487C4D74DADD",
                    "71C8400C51797C7A6CFB71C6B80088AC",
                    "0407562D95DE032438E981F80C772067",
                    "7E6CA742B7236CAFE6BA761048D5A10B",
                    "5A76DEA6CC46B31A766E4E0C68190A98",
                    "18A278AF435CD763E40107EA6A41570A",
                    "336DF3D91DE6EC882F739EA74E4B8760",
                    "2B55CCA3D891899E55C0124311359898",
                    "7BD7808AF1DC064E6BE761BAB0FC87B0",
                    "A3718E0BE544E29C2DFD890B2BA054A6",
                    "1EA172A4334799084ED29F347E1DFC77",
                    "6CE3AAD25761A90E4AC4874AA8727C23",
                    "03B6B413D010902C9F504B45BE9DD3A6",
                    "6D960DE519D3CADFCAF467DACBB3FFFF",
                    "200233C4B225D7A54A2E5B8732D4152D",
                    "9776A22E188632FC9B7AA2C96F0C4943",
                    "302FCFAB563EA5172989C7683708755E",
                    "8845FF0CE8019A3D801181535355F30D",
                    "38B1153E4C976137AC6F1116487C4C99",
                    "B62F94547EFCF902ABA176AE137B7EFE",
                    "823EE4E5942F619E7AA5AE1AB44F873B",
                    "1C5B8307384695071979B75827F8644E",
                    "0403EF362CFDAB3C841A8F666B721535",
                    "ABB3A4F1EBC17553BA8733427EBC96ED",
                    "A0EA21B5F5110C30D1BEA2E95DD4E675",
                    "4F25E4FEC66AEC97C0D5DA5A08B803CC",
                    "42E1684CDD499408FAD7F1937B65EBED",
                    "F49B2D1BB62C81FFFAE206ABB5DA9BD9",
                    "E0D2AFE8CE9030FA4074E3D2CC15138D",
                    "86036D32A5F31F7649090581072D4F06",
                    "61ABE275B6D2CCF0911FE932877A6643",
                };

        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };
    }
}