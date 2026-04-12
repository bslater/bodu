// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JSHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Snefru128" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Snefru128Tests
        : Security.Cryptography.SnefruTests<Snefru128Tests, Snefru128>
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdoc />
        protected override Snefru128 CreateAlgorithm() => new Snefru128();

        protected override Snefru128 CreateAlgorithm(SingleTestVariant variant) => new Snefru128();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "AA2532A1422095F6E8DBFF85FD6EF2BC",
            "7DFE4B884ACBCE230EEAAC73DE1A6E7E",
            "909BD105BA9534D89AB51D8D8ABAECC6",
            "7FD12E780DFCED473C2E9EB400CB9C3A",
            "B7407A7142728644B691FDF727CAC6D0",
            "3AB81D7C50FC0978BF7D9E1E73DE3D6D",
            "FCC962B5441AD38232740D2B9C103959",
            "C512EDF6CD760397F5A07D08839702C4",
            "414EE6E6C09B2EFA188B0BDD482B73B3",
            "04C66200906F654BFC1C778F4A9E5510",
            "D50E046AD9FA976555E80FC519F9CDF4",
            "3327FF18EE26268F94D42540D70CB7F2",
            "9C1E8886122851777D3A00FD7C66C7B7",
            "A095699139C344B59ED5114AFAE2F6F0",
            "2E553868E7A65615A722D6DB5124EDFC",
            "C572F4B011238DC58E44EFA290BDDDB2",
            "B7A3BF8A0A82812281AE90281CAC7D69",
            "F769214420AF44A3D7A0846788B9E503",
            "10D7DD115EE1BB72B03BD5DCB5ADB212",
            "7C1F43518F9C75F2141C996BD6BE5BF6",
            "F54B2B6034238F5E07EF68F8139F3361",
            "4251023D2728602FA5204DD5F5D33B84",
            "6DCD6963113F3823DCD6062B188C2B0C",
            "5A7AD5080D7930F3C07C45B0004882F2",
            "A9A8E5B2873D3FF4CAEE851A2DF8399F",
            "546E32578CED52756E4553E027942EBD",
            "ACFC435FCA1E1CA1EE39B7897AFA1EB7",
            "8A49F3B8AF2B4FCD9C856B7E42BC2C9D",
            "4EA497CC7AB3A50850A3E65CC8286317",
            "9CAAABF8D277D538DD457D58377CB324",
            "D31A9B4B3F091635DB4AF8F61B7173F9",
            "37EB4FD6A91B188A624912CA5D7C1697",
            "AC68543A6B10DEEB2E0DD19D906D9E59",
            "FD7AC8EE9B19E25C322874BAB684CA52",
            "EF8C5EBC059B895242466E3F7EBF3494",
            "10E1F08ADAF2FD6EC64E6695C61E03D5",
            "1A7C83D34F34FD60B722E1AA7DE58134",
            "BF3F41CC023EB1F0D4D59BA76071C0A4",
            "090258E418A73AAF5D5584352359501B",
            "2293DCA32F10FFEBF84B1BD183DE2AA0",
            "3C725D98C1E4708C01089EDF96351405",
            "62D57BE95350BADB70BDAAD498A1467C",
            "063FB9A428F32B9AAB59189154BA8D00",
            "1C51B3DA5F79DDED73C1358FD1B1E9CF",
            "4F8B10E99C5A08D037AA789ACBBDCF7B",
            "83A7C02A7308D143A4B2A37ADF8EA9DA",
            "D529519BA981B2E9E53BF353D9A239EE",
            "9F4FC9BF5F5B63A0AA13CDB11892A449",
            "0732C97F1E7637FE75447BBDD5BAC4A9",
            "386E5CD6956FC040E500A1F7B8A67191",
            "E1684D203C16E4B40D3431AE09501020",
            "B95E39944909EEB6915983D9E7FB5038",
            "1A82160443FFA6EE601F6EFAA40D7A38",
            "231E1539BE3D434E45A3EC32F6731351",
            "8209F885FDCDA7E2CF458E22DB829863",
            "B464CE870F44E4BA5A1867A5847FE291",
            "7B82C4B07B59D317D569F219247666DE",
            "F15C940FB6F4DB49AAB2D4782D82FDD1",
            "380B8BD3C40A4C9578C13C9A8055B22A",
            "FB7564D4D4F2932C89609BC66A523616",
            "FF2FB75E1CA19FD47E592B68A1D9FA5B",
            "3E29504244762A7A6231206646BBBFCF",
            "09373D29E46487225480D97A1E04EB73",
            "193E2FFAE3DC6A5AABB7FD6B46E203BB",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "AA2532A1422095F6E8DBFF85FD6EF2BC",
            ["ABC"] = "26C6CC5A5789D5F737335B305DE80218",
            ["Zeros_16"] = "F2DDD3750BE35F20E0557F47E8B59C39",
            ["QuickBrownFox"] = "59D9539D0DD96D635B5BDBD1395BB86C",
            ["Sequential_0_255"] = "9FBED4C571EF6E8EEFA0B7F8353C6540",

            ["a"] = "BF5CE540AE51BC50399F96746C5A15BD",
            ["1234567890"] = "D9204ED80BB8430C0B9C244FE485814A",
        };
    }
}