// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.32.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// <remarks>
    /// Entries are the documented CityHash32 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "7AD156DC", "5427A9C0", "B678162A", "E292C9D7",
        "32116E61", "D4376EFE", "7E894551", "5D84EACF",
        "D6D20FEB", "E0D6D37C", "5022E514", "28A11D93",
        "1CEF42DE", "0474DD8A", "D06B9769", "4205CDCA",
        "87BFAE17", "DACB9B1E", "4B0C68BA", "DABFCFF7",
        "962A1AC4", "872B3F4A", "B7D77D4B", "F5E4E01D",
        "A46ACF60", "78DF6D2E", "60DFA517", "A4390110",
        "318167EF", "7D7058C1", "571DD9D3", "285334E4",
        "15339468", "9E6D4EE1",
    };
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        KnownAnswers = new()
        {
            Empty = "7AD156DC",
            Abc = "1325B816",
            QuickBrownFox = "10C839A3",
            Zeros16 = "85BE0CC5",
            Sequential0To255 = "9F906E96",
            Additional = CityHashTests.CityHashGoogleReferenceKnownAnswers.CityHash32,
        },
    };

}
