// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;
using static Bodu.Security.Cryptography.SipHash64Tests;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="SipHash64" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class SipHash64Tests
        : Security.Cryptography.SipHashTests<SipHash64Tests, SipHash64>
    {
        /// <inheritdoc />
        protected override int ExpectedInputBlockSize => 8;

        /// <inheritdoc />
        protected override int ExpectedOutputBlockSize => 8;

        /// <inheritdoc />
        protected override int MaximumLegalKeyLength => ExpectedKeySize;

        /// <inheritdoc />
        protected override int MinimumLegalKeyLength => ExpectedKeySize;

        /// <inheritdoc />
        protected override IReadOnlyList<int> ValidKeyLengths => new[] { ExpectedKeySize };

        /// <inheritdoc />
        protected override SipHash64 CreateAlgorithm() => new SipHash64
        {
            Key = GetDeterministicKey(),
            CompressionRounds = 2,
            FinalizationRounds = 4,
        };

        protected override SipHash64 CreateAlgorithm(SipHashVariant variant) =>
            variant switch
            {
                SipHashVariant.SipHash_2_4 => this.CreateAlgorithm(),
                SipHashVariant.SipHash_4_8 => new SipHash64
                {
                    Key = GetDeterministicKey(),
                    CompressionRounds = 4,
                    FinalizationRounds = 8,
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SipHashVariant variant) =>
            variant switch
            {
                SipHashVariant.SipHash_2_4 => new Dictionary<string, string>
                {
                    ["Empty"] = "310E0EDD47DB6F72",
                    ["ABC"] = "E848C7790EAA99A2",
                    ["Zeros_16"] = "017755EFC0D3A098",
                    ["QuickBrownFox"] = "E46F1FDC05612752",
                    ["Sequential_0_255"] = "1AB24DC7FE69C1A9",
                },
                SipHashVariant.SipHash_4_8 => new Dictionary<string, string>
                {
                    ["Empty"] = "41DA38992B0579C8",
                    ["ABC"] = "618F256DAD66F19A",
                    ["Zeros_16"] = "640CDE74B926C33E",
                    ["QuickBrownFox"] = "70CB440B22B8D9F6",
                    ["Sequential_0_255"] = "23ED19F6EF85AC97",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SipHashVariant variant) =>
            variant switch
            {
                SipHashVariant.SipHash_2_4 => new[]
                {
                    "310E0EDD47DB6F72",
                    "FD67DC93C539F874",
                    "5A4FA9D909806C0D",
                    "2D7EFBD796666785",
                    "B7877127E09427CF",
                    "8DA699CD64557618",
                    "CEE3FE586E46C9CB",
                    "37D1018BF50002AB",
                    "6224939A79F5F593",
                    "B0E4A90BDF82009E",
                    "F3B9DD94C5BB5D7A",
                    "A7AD6B22462FB3F4",
                    "FBE50E86BC8F1E75",
                    "903D84C02756EA14",
                    "EEF27A8E90CA23F7",
                    "E545BE4961CA29A1",
                    "DB9BC2577FCC2A3F",
                    "9447BE2CF5E99A69",
                    "9CD38D96F0B3C14B",
                    "BD6179A71DC96DBB",
                    "98EEA21AF25CD6BE",
                    "C7673B2EB0CBF2D0",
                    "883EA3E395675393",
                    "C8CE5CCD8C030CA8",
                    "94AF49F6C650ADB8",
                    "EAB8858ADE92E1BC",
                    "F315BB5BB835D817",
                    "ADCF6B0763612E2F",
                    "A5C91DA7ACAA4DDE",
                    "716595876650A2A6",
                    "28EF495C53A387AD",
                    "42C341D8FA92D832",
                },
                SipHashVariant.SipHash_4_8 => new[]
                {
                    "41DA38992B0579C8",
                    "51B89552F91459C8",
                    "923716F0BEDDC333",
                    "6A46D47D6547C105",
                    "C238592B4AC1FA48",
                    "F6C2D7D9CF5247E1",
                    "6BB6BC34C835558E",
                    "47D73F715ABEFD4E",
                    "20B58B9C072FDB50",
                    "36319AF35EE11253",
                    "48A9D0DB0A8D848F",
                    "CC69396036040A81",
                    "4B6D68537AA79761",
                    "293796E9F2C95069",
                    "88431BEAA7629A68",
                    "E0A6A97DD589D383",
                    "559CF55380B2AC70",
                    "D5B7C5117AE3794E",
                    "5A3C454634AD102B",
                    "C0A480AFA35A3DBC",
                    "78C22709E5284BC8",
                    "EF2670460DEBD69D",
                    "D976EF86A9D084D8",
                    "E3D9811819EAD0E8",
                    "89333CB53EEAEC16",
                    "31156C5F647349C6",
                    "A54CCE35357632A4",
                    "065D8925C0A7D2FE",
                    "2BBBAA82221A3A8B",
                    "870BFBCE64097B70",
                    "40D8E0F96495EE8B",
                    "79FCA7F40BFADF12",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };
    }
}