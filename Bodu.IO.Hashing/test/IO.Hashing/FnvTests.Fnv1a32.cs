// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.Fnv1a32.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Fnv1a32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fnv1a32Tests
    : FnvTests<Fnv1a32Tests, Fnv1a32>
{

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented FNV-1a 32-bit known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        "811C9DC5", "050C5D1F", "1076963A", "22AE7A28",
        "C3AA51B1", "BA1E9FEF", "E835BD5E", "E4991188",
        "6BF6A41D", "0A444D0F", "2F854072", "46C47CE8",
        "4A509959", "51E160CF", "A7CB5166", "8D1126B8",
        "C8FFF215", "6FE9FDDF", "FB5A8B4A", "07895B88",
        "783B3501", "5A34900F", "1ABED8EE", "126F8E68",
        "849D51ED", "B8A804AF", "667F6A82", "F294CD48",
        "333F39A9", "6187D7EF", "7AD8F1F6", "4B84D038",
        "0913AD65", "8EF9C39F",
    ];
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        AlgorithmName = "FNV-1a-32",
        MinNonZeroBytesForLongInput = 3,
        KnownAnswers = new()
        {
            Empty = "811C9DC5",
            Abc = "5C842F6B",
            QuickBrownFox = "048FFF90",
            Zeros16 = "69691905",
            Sequential0To255 = "1C2213B8",
        },
    };

}
