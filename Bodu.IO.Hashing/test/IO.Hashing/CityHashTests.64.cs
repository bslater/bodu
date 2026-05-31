// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.64.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// <remarks>
    /// Entries are the documented CityHash64 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        "4F40902F3B6AE19A", "544BE9F5ED5660BE", "758D03ED6546A0C2", "9AA4EBE9223DA194",
        "40E5588989FDBF82", "49C13277E8A9BFB4", "33234AE9D8BCFD92", "A1A6B00DF2BFE0A2",
        "983BE9E8E1135AAD", "4FD84A0E151E3781", "EAFFD6B2B24D709B", "DD3A801D3C2B21F3",
        "7D3DFCAE33DFD59F", "923C3D855CCFC5C1", "7146BBB27BA8D94B", "9DBD435955512A86",
        "D45641A3A025FD0E", "1C1F0DF2F8A6B6BB", "E6571F098BC1F879", "31DF11557EAB9793",
        "E9A016BFF1E7D78D", "24FAC9CE4F9DEDEB", "20F058E435D4E628", "DCFD98E234E36206",
        "C76ED1CB3D313B3F", "3E4CCC8340C69FB4", "ED9D4EEDDEDB1313", "F619EADABE4D7E42",
        "A6EF238A050760A5", "9FDF2AC8ED1F0896", "A70ED5EBE9B0AB48", "4169EF27AF50D9FB",
        "49DF2C9799819D1A", "BADA22BC8C37E146", "22150DE7ABC3BAB5", "008300F3A39DE528",
        "0131A4EEC4DF1100", "4337BD952B774536", "19C81A20FA469962", "03B36F3CBAFAF6A5",
        "1C3DE9657ADDF736", "A7ED1E8A040374C0", "A3434F74893C4DCA", "3E5102ED318DF54C",
        "017A9DD50BC0079E", "19053CAF4265775E", "C6875DBCA4D262FA", "4AE10EA77748F6B7",
        "25463F44AC9E09C7", "117A041D741CB160", "C99493EE92602873", "29B9A4B5D9A87778",
        "A82347E82CCB89D5", "8755023E0F245CA8", "CD52BFB6CD8ED9C3", "21A0483932C690F8",
        "3F33A4303668A596", "CC4686CDED6841A8", "0EF7FDCCF0E6F167", "B6EAFFEDC684CB84",
        "69DB68798FFC54E9", "036BEFD2C21292B2", "E39B97203B73202F", "EFA6AD777A9230AF",
        "A5DCC75E0FB89AE9", "2EDD8304999C58AC",
    ];
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        BoundaryLengths = [16, 32, 64, 65],
        MinNonZeroBytesForLongInput = 7,
        KnownAnswers = new()
        {
            Empty = "4F40902F3B6AE19A",
            Abc = "5A364348181F2D03",
            QuickBrownFox = "7DCAFE28497268C2",
            Zeros16 = "1558EDCF4D2D9030",
            Sequential0To255 = "B81550581C6E4657",
            Additional = CityHashTests.CityHashGoogleReferenceKnownAnswers.CityHash64,
        },
    };

}
