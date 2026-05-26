// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Pjw32Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Pjw32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Pjw32Tests
    : NonCryptographicHashAlgorithmTests<Pjw32Tests, Pjw32, SingleTestVariant>
{

    /// <inheritdoc />
    protected override Pjw32 CreateAlgorithm(SingleTestVariant variant) => new();

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented PJW-32 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        "00000000", "00000000", "00000001", "00000012",
        "00000123", "00001234", "00012345", "00123456",
        "01234567", "02345679", "0345679B", "045679B9",
        "05679B9F", "0679B9F9", "079B9F9B", "09B9F9B9",
        "0B9F9B96", "09F9B97B", "0F9B97C8", "09B97C9D",
        "0B97C9EA", "097C9EBF", "07C9EC0C", "0C9EC0D1",
        "09EC0D2B", "0EC0D2C1", "0C0D2C27", "00D2C286",
        "0D2C287B", "02C287C1", "0C287C2F", "0287C302",
        "087C303D", "07C303F8",
    ];

    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 4,
            BoundaryLengths = [1, 8, 16, 64],
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 2,
            KnownAnswers = new()
            {
                Empty = "00000000",
                Abc = "00004563",
                QuickBrownFox = "021B6694",
                Zeros16 = "00000000",
            },
        };

}
