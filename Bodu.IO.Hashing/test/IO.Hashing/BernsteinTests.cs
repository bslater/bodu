// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BernsteinTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Bernstein" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class BernsteinTests
    : NonCryptographicHashAlgorithmTests<BernsteinTests, Bernstein, BernsteinHashVariant>
{

    /// <inheritdoc />
    protected override Bernstein CreateAlgorithm(BernsteinHashVariant variant) => variant switch
    {
        BernsteinHashVariant.Default => new Bernstein(),
        BernsteinHashVariant.Modified => new Bernstein(Bernstein.DefaultInitialValue, useModifiedAlgorithm: true),
        _ => throw new ArgumentOutOfRangeException(nameof(variant)),
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented djb2 known-answer sequences for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(BernsteinHashVariant variant) => variant switch
    {
        BernsteinHashVariant.Default => new[]
        {
            "00001505", "0002B5A5", "00596A46", "0B86B308",
            "7C5D140B", "07FF956F", "07F24354", "063AADDA",
            "CD906921", "7F9D8D49", "734F3672", "DD3604BC",
            "83F69C47", "02CA2533", "5C0ECBA0", "DDE83FAE",
            "9AF0357D", "F8F6E52D", "17D38ADE", "1244E6B0",
            "5AE1BCC3", "B7195537", "9A43FC2C", "E2C381C2",
            "3B33BA19", "A1AAFD51", "D70AA78A", "B85F98E4",
            "C452B57F", "4EA9657B", "23D614F8", "9E98B416",
            "71AF36F5", "A79615B5",
        },
        BernsteinHashVariant.Modified =>
        [
            "00001505", "0002B5A5", "00596A44", "0B86B2C6",
            "7C5D0B85", "07FE7C21", "07CE0044", "018E08C2",
            "334F2105", "9D3341AD", "439B7744", "B70A5FCE",
            "98565985", "A3218A29", "0752CF44", "F1ACB7CA",
            "2743B105", "0FB9D1B5", "06F40844", "E57510D6",
            "94172B85", "16FC9C31", "F6902244", "C8946AD2",
            "DB21C505", "3F5A65BD", "2AA71D44", "7F8AC5DE",
            "70E38185", "8D53B239", "37C9F944", "310921DA",
            "522D5D05", "97D8FD85",
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(variant)),
    };

    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(BernsteinHashVariant variant) => variant switch
    {
        BernsteinHashVariant.Default => new()
        {
            HashLengthInBytes = 4,
            KnownAnswers = new()
            {
                Empty = "00001505",
                Abc = "0B87D02B",
                QuickBrownFox = "34CC38DE",
                Zeros16 = "BDCB7F05",
                Sequential0To255 = "9FD43AC6",
            },
        },
        BernsteinHashVariant.Modified => new()
        {
            HashLengthInBytes = 4,
            KnownAnswers = new()
            {
                Empty = "00001505",
                Abc = "0B87B6A5",
                QuickBrownFox = "B679B80A",
                Zeros16 = "BDCB7F05",
                Sequential0To255 = "4CCB76BA",
            },
        },
        _ => throw new ArgumentOutOfRangeException(nameof(variant)),
    };

}
