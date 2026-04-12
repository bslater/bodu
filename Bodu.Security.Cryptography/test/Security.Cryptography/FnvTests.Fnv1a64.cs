// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Fnv1a64" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Fnv1a64Tests
        : Security.Cryptography.FnvTests<Fnv1a64Tests, Fnv1a64>
    {
        /// <inheritdoc />
        protected override int ExpectedInputBlockSize => 1;

        /// <inheritdoc />
        protected override int ExpectedOutputBlockSize => 1;

        /// <inheritdoc />
        protected override Fnv1a64 CreateAlgorithm() => new Fnv1a64();

        protected override Fnv1a64 CreateAlgorithm(SingleTestVariant variant) => new Fnv1a64();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "CBF29CE484222325",
            "AF63BD4C8601B7DF",
            "08328707B4EB6E3A",
            "D949AA186C0C4928",
            "4475327F98E05411",
            "3378E3D0C52EDFAF",
            "A54AC5BF0EA60DDE",
            "841BDBA5E4298608",
            "A4DC49E2B28ECB7D",
            "B11D013568A3B7CF",
            "9800D3C0CE314572",
            "7AAD489E5DB90AE8",
            "2D7D4819416D7FB9",
            "B96137EA2D10008F",
            "103284EA9230DCE6",
            "B6B4C29669075E38",
            "7C84DC9477851775",
            "1ADA35471726C09F",
            "C78915CC56D9314A",
            "E7216237930ED888",
            "CC927F6EE23A0F61",
            "D6FDFE6A68A843CF",
            "F9DB28CFD5EB4B6E",
            "7AB1D1287ED13CE8",
            "4D6366CF7D8AA54D",
            "0A8D07925296EF6F",
            "849353A25678E582",
            "BF3CB4D8EF6E2148",
            "62489F9ED822A009",
            "24074FE942D603AF",
            "0E707B5C91A84776",
            "3169064B80F155B8",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "CBF29CE484222325",
            ["ABC"] = "FA2FE219A07442EB",
            ["Zeros_16"] = "88201FB960FF6465",
            ["QuickBrownFox"] = "F3F9B7F5E7E47110",
            ["Sequential_0_255"] = "49CC0AA461DC8C38",
        };
    }
}