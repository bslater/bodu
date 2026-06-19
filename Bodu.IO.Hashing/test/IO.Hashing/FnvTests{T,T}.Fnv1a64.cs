// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests{T,T}.Fnv1a64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Fnv1a64" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fnv1a64Tests
    : FnvTests<Fnv1a64Tests, Fnv1a64>
{

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented FNV-1a 64-bit known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        "CBF29CE484222325", "AF63BD4C8601B7DF", "08328707B4EB6E3A", "D949AA186C0C4928",
        "4475327F98E05411", "3378E3D0C52EDFAF", "A54AC5BF0EA60DDE", "841BDBA5E4298608",
        "A4DC49E2B28ECB7D", "B11D013568A3B7CF", "9800D3C0CE314572", "7AAD489E5DB90AE8",
        "2D7D4819416D7FB9", "B96137EA2D10008F", "103284EA9230DCE6", "B6B4C29669075E38",
        "7C84DC9477851775", "1ADA35471726C09F", "C78915CC56D9314A", "E7216237930ED888",
        "CC927F6EE23A0F61", "D6FDFE6A68A843CF", "F9DB28CFD5EB4B6E", "7AB1D1287ED13CE8",
        "4D6366CF7D8AA54D", "0A8D07925296EF6F", "849353A25678E582", "BF3CB4D8EF6E2148",
        "62489F9ED822A009", "24074FE942D603AF", "0E707B5C91A84776", "3169064B80F155B8",
        "E6CB594C1A148AC5", "4013A15050E8031F", "C95E59797A3D825A", "68D4816AB684B1E8",
        "A5C9B354237A1BF1", "2FD78CF8487D4AEF", "C88F53E32CE6153E", "B1A0A3054AF5D7C8",
        "C9CCF0FE5FBDEB1D", "A528B53CAFB6AB0F", "5AD71C1E9364D192", "C0567FF474505BA8",
        "2356EF61A48B7F99", "983877EA990A008F", "B1F465A20BFF1346", "615C1A5A626DF9B8",
        "DD7A5E9540DF1B95", "360A5C9D3B1BF95F", "EF94CD2B7088D5EA", "A2AE78D038835E08",
        "F1D584D007391E41", "26EF227C460EC0CF", "3718932B0B11E8CE", "B0AB0A23CF6EDD68",
        "A17F99D97962286D", "CDFAC18941CA886F", "CB9F323ACB25AA22", "252C71E7310006C8",
        "2A8C7ED8430BCEE9", "588A5E79ED1073EF", "83925B2DD2F4CDD6", "867ED6DD75F9E138",
        "8368214F77995EE5", "E34F3F08399BD25F",
    ];
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        AlgorithmName = "FNV-1a-64",
        MinNonZeroBytesForLongInput = 6,
        KnownAnswers = new()
        {
            Empty = "CBF29CE484222325",
            Abc = "FA2FE219A07442EB",
            QuickBrownFox = "F3F9B7F5E7E47110",
            Zeros16 = "88201FB960FF6465",
            Sequential0To255 = "49CC0AA461DC8C38",
        },
    };

}
