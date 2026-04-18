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
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            InputBlockSize = 1,
            OutputBlockSize = 1,
            IsStateless = false,
            LongInputLength = 200,
            BoundaryLengths = [1, 8, 16, 64],
            HashSize = 128,
            MinNonZeroBytesForLongInput = 6,    // 16 output bytes; conservative given Snefru's weaker diffusion
        };

        /// <inheritdoc />
        protected override Snefru128 CreateAlgorithm() => new Snefru128();

        protected override Snefru128 CreateAlgorithm(SingleTestVariant variant) => new Snefru128();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "AA2532A1422095F6E8DBFF85FD6EF2BC",  // []
            "7DFE4B884ACBCE230EEAAC73DE1A6E7E",  // [0x00]
            "909BD105BA9534D89AB51D8D8ABAECC6",  // [0x00, 0x01]
            "7FD12E780DFCED473C2E9EB400CB9C3A",  // [0x00, 0x01, 0x02]
            "B7407A7142728644B691FDF727CAC6D0",  // [0x00, 0x01, 0x02, 0x03]
            "3AB81D7C50FC0978BF7D9E1E73DE3D6D",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "FCC962B5441AD38232740D2B9C103959",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "C512EDF6CD760397F5A07D08839702C4",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "414EE6E6C09B2EFA188B0BDD482B73B3",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "04C66200906F654BFC1C778F4A9E5510",  // ...
            "D50E046AD9FA976555E80FC519F9CDF4",
            "3327FF18EE26268F94D42540D70CB7F2",
            "9C1E8886122851777D3A00FD7C66C7B7",
            "A095699139C344B59ED5114AFAE2F6F0",
            "2E553868E7A65615A722D6DB5124EDFC",
            "C572F4B011238DC58E44EFA290BDDDB2",
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