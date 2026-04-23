// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.Fnv164.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Fnv164" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Fnv164Tests
    : Security.Cryptography.FnvTests<Fnv164Tests, Fnv164>
{
    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashSize = 64,
        InputBlockSize = 1,
        OutputBlockSize = 1,
        IsStateless = false,
        LongInputLength = 200,
        MinNonZeroBytesForLongInput = 6,   // 8 output bytes; FNV-1 64-bit has excellent avalanche so nearly all bytes should be non-zero
        BoundaryLengths = [1, 8, 16, 64],
    };

    /// <inheritdoc />
    protected override Fnv164 CreateAlgorithm() => new Fnv164();

    protected override Fnv164 CreateAlgorithm(SingleTestVariant variant) => new Fnv164();

    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "CBF29CE484222325",  // []
        "AF63BD4C8601B7DF",  // [0x00]
        "08328807B4EB6FEC",  // [0x00, 0x01]
        "D94D11186C0F2E06",  // [0x00, 0x01, 0x02]
        "4D22127F9DCB3431",  // [0x00, 0x01, 0x02, 0x03]
        "DC199FD92049AF47",  // [0x00, 0x01, 0x02, 0x03, 0x04]
        "4939E4F1DD34D5A0",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
        "A235A6FAE0C6FEE6",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
        "6829A24BF22320D5",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
        "21DF9C0C71B0C9E7",  // ...
        "3FC010252F67138C",
        "BA6EFB2F8C2636EE",
        "F0CBBFCB24EF5661",
        "198D472FC2AFC6DF",
        "1AD6D527D0AEECE0",
        "49F912A7993C80AE",
    };

    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
    {
        ["Empty"] = "CBF29CE484222325",
        ["ABC"] = "D86FEA186B53126B",
        ["Zeros_16"] = "88201FB960FF6465",
        ["QuickBrownFox"] = "A8B2F3117DE37ACE",
        ["Sequential_0_255"] = "46F4BC763E8FD1BE",
    };
}
