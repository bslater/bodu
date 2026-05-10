// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="CityHash32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class CityHash32Tests
    : CityHashTests<CityHash32Tests, CityHash32>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        KnownAnswers = new()
        {
            Empty = "02400040",
            Abc = "CB5A67A8",
            QuickBrownFox = "38D83018",
            Zeros16 = "00400040",
            Sequential0To255 = "3CB48141",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented CityHash32 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "02400040", "E5B0A856", "0280CFE1", "1BCEE319",
        "B91E6445", "EA55F979", "84122C7E", "67F85183",
        "107293FA", "D5996B59", "CD20F5EB", "B59EBF6C",
        "973A5B10", "5220AAAB", "5CA20E63", "AE1B9001",
        "D7B8AE17", "07958098", "A460E823", "47740687",
        "68D70638", "ABF9D8B7", "0A68527B", "B92EB499",
        "29A61AE9", "F8EFB3C7", "A41C3912", "54C8DC58",
        "DFCCA00D", "50AB0BAD", "C5C2F9F6", "2F7D54FF",
        "449BB5CE", "63836625",
    };
}
