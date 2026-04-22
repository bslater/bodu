// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.Fnv1a32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Fnv1a32" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Fnv1a32Tests
    : Security.Cryptography.FnvTests<Fnv1a32Tests, Fnv1a32>
{
    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashSize = 32,
        InputBlockSize = 1,
        OutputBlockSize = 1,
        IsStateless = false,
        LongInputLength = 200,
        MinNonZeroBytesForLongInput = 3,   // 4 output bytes; FNV-1a has slightly better avalanche than FNV-1
        BoundaryLengths = [1, 8, 16, 64],
    };

    /// <inheritdoc />
    protected override Fnv1a32 CreateAlgorithm() => new Fnv1a32();

    protected override Fnv1a32 CreateAlgorithm(SingleTestVariant variant) => new Fnv1a32();

    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "811C9DC5",  // []
        "050C5D1F",  // [0x00]
        "1076963A",  // [0x00, 0x01]
        "22AE7A28",  // [0x00, 0x01, 0x02]
        "C3AA51B1",  // [0x00, 0x01, 0x02, 0x03]
        "BA1E9FEF",  // [0x00, 0x01, 0x02, 0x03, 0x04]
        "E835BD5E",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
        "E4991188",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
        "6BF6A41D",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
        "0A444D0F",  // ...
        "2F854072",
        "46C47CE8",
        "4A509959",
        "51E160CF",
        "A7CB5166",
        "8D1126B8",
    };

    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
    {
        ["Empty"] = "811C9DC5",
        ["ABC"] = "5C842F6B",
        ["Zeros_16"] = "69691905",
        ["QuickBrownFox"] = "048FFF90",
        ["Sequential_0_255"] = "1C2213B8",
    };
}
