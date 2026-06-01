// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MurmurHash3Tests.32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="MurmurHash3_32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class MurmurHash3_32Tests
    : MurmurHash3Tests<MurmurHash3_32Tests, MurmurHash3_32>
{

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        "00000000", "B7284E51", "C0A2E170", "D7D0D451",
        "39ECC0F4", "CBDCA4CC", "02C31C90", "14497E8D",
        "73D661D1", "507AE2E7", "FB5EBAD5", "FC4BBBF3",
        "8FA84E06", "0800F060", "AD5EC5F7", "2D95D65B",
        "DD731519", "C68DE5CB", "E9533425", "CDFF67E4",
        "442031A6", "02B27167", "1986018C", "B6CA6786",
        "0F512B8C", "17D762F4", "D5A214FD", "C6A83941",
        "FA94E20C", "BCE56079", "AEABF074", "D66A4264",
        "3876C3CA", "7A866054",
    ];
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        KnownAnswers = new()
        {
            // Seed = 0. Verified against the reference MurmurHash3_x86_32 implementation.
            Empty = "00000000",
            Abc = "35C5C143",
            QuickBrownFox = "23F74F2E",
            Zeros16 = "F8CD3481",
            Sequential0To255 = "00B63463",
        },
    };

}
