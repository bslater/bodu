// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.32.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Fletcher32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fletcher32Tests
    : FletcherTests<Fletcher32Tests, Fletcher32>
{

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented Fletcher-32 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        "00000000", "00000000", "00010001", "00040003",
        "000A0006", "0014000A", "0023000F", "00380015",
        "0054001C", "00780024", "00A5002D", "00DC0037",
        "011E0042", "016C004E", "01C7005B", "02300069",
        "02A80078", "03300088", "03C90099", "047400AB",
        "053200BE", "060400D2", "06EB00E7", "07E800FD",
        "08FC0114", "0A28012C", "0B6D0145", "0CCC015F",
        "0E46017A", "0FDC0196", "118F01B3", "136001D1",
        "155001F0", "17600210",
    ];
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 4,
            AlgorithmName = "Fletcher-32",
            BlockSizeBytes = 2,
            KnownAnswers = new()
            {
                Empty = "00000000",
                Abc = "018A00C6",

                // QuickBrownFox suppressed — tracked by issue #167 (Adler/Fletcher KAT mismatch
                // observed on PR #166 CI: index 1 expected 0xCD, actual 0xDC). Restore once the
                // root cause is identified and fixed.
                QuickBrownFox = "5BA30FD9",
                Zeros16 = "00000000",
                Sequential0To255 = "2B2A7E81",
            },
        };

}
