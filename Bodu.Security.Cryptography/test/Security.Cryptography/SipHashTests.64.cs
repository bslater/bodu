// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="SipHash64" /> hash algorithm.
/// </summary>
[TestClass]
public partial class SipHash64Tests
    : Security.Cryptography.SipHashTests<SipHash64Tests, SipHash64>
{
    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SipHashVariant variant) => new KeyedAlgorithmSpecification
    {
        HashSize = 64,
        HashBlockSize = 8,
        IsStateless = false,
        LongInputLength = 200,
        MinNonZeroBytesForLongInput = 6,   // 8 output bytes; SipHash has strong PRF properties so most bytes should be non-zero
        BoundaryLengths = [1, 8, 16, 64],
        MinKeyLength = 16,
        MaxKeyLength = 16,
        ValidKeyLengths = [16],
        TestKey = SipHashTestKey,
        KnownAnswers = variant switch
        {
            SipHashVariant.SipHash_2_4 => new HashAlgorithmKnownAnswers
            {
                Empty = "310E0EDD47DB6F72",
                Abc = "E848C7790EAA99A2",
                Zeros16 = "017755EFC0D3A098",
                QuickBrownFox = "E46F1FDC05612752",
                Sequential0To255 = "1AB24DC7FE69C1A9",
            },
            SipHashVariant.SipHash_4_8 => new HashAlgorithmKnownAnswers
            {
                Empty = "41DA38992B0579C8",
                Abc = "618F256DAD66F19A",
                Zeros16 = "640CDE74B926C33E",
                QuickBrownFox = "70CB440B22B8D9F6",
                Sequential0To255 = "23ED19F6EF85AC97",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        },
    };

    /// <inheritdoc />
    protected override SipHash64 CreateAlgorithm() => new SipHash64
    {
        Key = SipHashTestKey,
        CompressionRounds = 2,
        FinalizationRounds = 4,
    };

    protected override SipHash64 CreateAlgorithm(SipHashVariant variant) =>
        variant switch
        {
            SipHashVariant.SipHash_2_4 => CreateAlgorithm(),
            SipHashVariant.SipHash_4_8 => new SipHash64
            {
                Key = SipHashTestKey,
                CompressionRounds = 4,
                FinalizationRounds = 8,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };

    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SipHashVariant variant) =>
        variant switch
        {
            SipHashVariant.SipHash_2_4 => new[]
            {
                "310E0EDD47DB6F72", "FD67DC93C539F874", "5A4FA9D909806C0D", "2D7EFBD796666785",
                "B7877127E09427CF", "8DA699CD64557618", "CEE3FE586E46C9CB", "37D1018BF50002AB",
                "6224939A79F5F593", "B0E4A90BDF82009E",
            },
            SipHashVariant.SipHash_4_8 => new[]
            {
                "41DA38992B0579C8", "51B89552F91459C8", "923716F0BEDDC333", "6A46D47D6547C105",
                "C238592B4AC1FA48", "F6C2D7D9CF5247E1", "6BB6BC34C835558E", "47D73F715ABEFD4E",
                "20B58B9C072FDB50", "36319AF35EE11253",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };
}
