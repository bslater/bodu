// -----------------------------------------------------------------------
// <copyright file="Bernstein.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public enum BernsteinHashVariant
    {
        Default,
        Modified
    }

    /// <summary>
    /// Contains unit tests for the <see cref="Adler" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class BernsteinTests
        : Security.Cryptography.HashAlgorithmTests<BernsteinTests, Bernstein, BernsteinHashVariant>
    {
        /// <inheritdoc />
        protected override Bernstein CreateAlgorithm() => new Bernstein();

        public override IEnumerable<BernsteinHashVariant> GetHashAlgorithmVariants() => new[]
        {
            BernsteinHashVariant.Default,
            BernsteinHashVariant.Modified
        };

        protected override Bernstein CreateAlgorithm(BernsteinHashVariant variant) =>
            variant switch
            {
                BernsteinHashVariant.Default => this.CreateAlgorithm(),
                BernsteinHashVariant.Modified => new Bernstein
                {
                    UseModifiedAlgorithm = true
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(BernsteinHashVariant variant) =>
            variant switch
            {
                BernsteinHashVariant.Default => new Dictionary<string, string>
                {
                    ["Empty"] = "00001505",
                    ["ABC"] = "0B87D02B",
                    ["Zeros_16"] = "BDCB7F05",
                    ["QuickBrownFox"] = "34CC38DE",
                    ["Sequential_0_255"] = "9FD43AC6",
                },
                BernsteinHashVariant.Modified => new Dictionary<string, string>
                {
                    ["Empty"] = "00001505",
                    ["ABC"] = "0B87B6A5",
                    ["Zeros_16"] = "BDCB7F05",
                    ["QuickBrownFox"] = "B679B80A",
                    ["Sequential_0_255"] = "4CCB76BA",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(BernsteinHashVariant variant) =>
            variant switch
            {
                BernsteinHashVariant.Default => new[]
                {
                    "00001505",
                    "0002B5A5",
                    "00596A46",
                    "0B86B308",
                    "7C5D140B",
                    "07FF956F",
                    "07F24354",
                    "063AADDA",
                    "CD906921",
                    "7F9D8D49",
                    "734F3672",
                    "DD3604BC",
                    "83F69C47",
                    "02CA2533",
                    "5C0ECBA0",
                    "DDE83FAE"
                },
                BernsteinHashVariant.Modified => new[]
                {
                    "00001505",
                    "0002B5A5",
                    "00596A44",
                    "0B86B2C6",
                    "7C5D0B85",
                    "07FE7C21",
                    "07CE0044",
                    "018E08C2",
                    "334F2105",
                    "9D3341AD",
                    "439B7744",
                    "B70A5FCE",
                    "98565985",
                    "A3218A29",
                    "0752CF44",
                    "F1ACB7CA",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };
    }
}