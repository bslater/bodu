// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

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

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(BernsteinHashVariant variant) =>
        new HashAlgorithmSpecification
        {
            HashSize = 32,
            InputBlockSize = 1,
            OutputBlockSize = 1,
        };

    protected override Bernstein CreateAlgorithm(BernsteinHashVariant variant) =>
        variant switch
        {
            BernsteinHashVariant.Default => CreateAlgorithm(),
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
                "00001505",  // []
                "0002B5A5",  // [0x00]
                "00596A46",  // [0x00, 0x01]
                "0B86B308",  // [0x00, 0x01, 0x02]
                "7C5D140B",  // [0x00, 0x01, 0x02, 0x03]
                "07FF956F",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                "07F24354",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                "063AADDA",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                "CD906921",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                "7F9D8D49",  // ...
                "734F3672",
                "DD3604BC",
                "83F69C47",
                "02CA2533",
                "5C0ECBA0",
                "DDE83FAE",
            },
            BernsteinHashVariant.Modified => new[]
            {
                "00001505",  // []
                "0002B5A5",  // [0x00]
                "00596A44",  // [0x00, 0x01]
                "0B86B2C6",  // [0x00, 0x01, 0x02]
                "7C5D0B85",  // [0x00, 0x01, 0x02, 0x03]
                "07FE7C21",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                "07CE0044",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                "018E08C2",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                "334F2105",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                "9D3341AD",  // ...
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
