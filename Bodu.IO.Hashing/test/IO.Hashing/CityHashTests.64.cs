// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="CityHash64" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class CityHash64Tests
    : CityHashTests<CityHash64Tests, CityHash64>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        BoundaryLengths = new[] { 16, 32, 64, 65 },
        MinNonZeroBytesForLongInput = 7,
        KnownAnswers = new()
        {
            Empty = "4F40902F3B6AE19A",
            Abc = "5A364348181F2D03",
            QuickBrownFox = "16035F69EEA357AE",
            Zeros16 = "1558EDCF4D2D9030",
            Sequential0To255 = "B81550581C6E4657",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented CityHash64 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "4F40902F3B6AE19A", "544BE9F5ED5660BE", "758D03ED6546A0C2", "9AA4EBE9223DA194",
        "40E5588989FDBF82", "49C13277E8A9BFB4", "33234AE9D8BCFD92", "A1A6B00DF2BFE0A2",
        "983BE9E8E1135AAD", "4FD84A0E151E3781", "EAFFD6B2B24D709B", "DD3A801D3C2B21F3",
        "7D3DFCAE33DFD59F", "923C3D855CCFC5C1", "7146BBB27BA8D94B", "9DBD435955512A86",
        "D45641A3A025FD0E", "1C1F0DF2F8A6B6BB", "E6571F098BC1F879", "31DF11557EAB9793",
        "E9A016BFF1E7D78D", "24FAC9CE4F9DEDEB", "20F058E435D4E628", "DCFD98E234E36206",
        "C76ED1CB3D313B3F", "3E4CCC8340C69FB4", "ED9D4EEDDEDB1313", "F619EADABE4D7E42",
        "A6EF238A050760A5", "9FDF2AC8ED1F0896", "A70ED5EBE9B0AB48", "4169EF27AF50D9FB",
    };
}
