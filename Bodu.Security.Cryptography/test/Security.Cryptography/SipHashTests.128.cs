// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.128.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="SipHash128" /> hash algorithm.
/// </summary>
[TestClass]
public partial class SipHash128Tests
    : Security.Cryptography.SipHashTests<SipHash128Tests, SipHash128>
{
    /// <inheritdoc />
    protected override SipHash128 CreateAlgorithm() => new SipHash128
    {
        Key = SipHashTestKey,
        CompressionRounds = 2,
        FinalizationRounds = 4,
    };

    private static readonly byte[] HelloInput = Encoding.UTF8.GetBytes("Hello");

    protected override HashAlgorithmSpecification GetSpecification(SipHashVariant variant) => new KeyedAlgorithmSpecification
    {
        HashSize = 128,
        HashBlockSize = 8,
        IsStateless = false,
        LongInputLength = 200,
        MinNonZeroBytesForLongInput = 8,   // 16 output bytes; SipHash has strong PRF properties so most bytes should be non-zero
        BoundaryLengths = [1, 8, 16, 64],
        MinKeyLength = 16,
        MaxKeyLength = 16,
        ValidKeyLengths = [16],
        TestKey = SipHashTestKey,
        KnownAnswers = variant switch
        {
            SipHashVariant.SipHash_2_4 => new HashAlgorithmKnownAnswers
            {
                Empty = "A3817F04BA25A8E66DF67214C7550293",
                Abc = "6EDFC93C6A8C85920C6D1BFE0413F575",
                Zeros16 = "D60D3284A18EBD5AF3D0F02A078007CD",
                QuickBrownFox = "7628C9301AA4412555E65227CD31964E",
                Sequential0To255 = "1C9BB67528165F8E468248E3799B0EAB",
                Additional =
                [
                    new HashAlgorithmKnownAnswer
                    {
                        Name = "Hello",
                        Input = HelloInput,
                        ExpectedHex = "C9E2FA57B43C46560D0F6C0657D05731",
                    },
                ],
            },
            SipHashVariant.SipHash_4_8 => new HashAlgorithmKnownAnswers
            {
                Empty = "1F64CE586DA904E9CFECE85483A70A6C",
                Abc = "2A74871B2DB4FB6B7F7167F798A760BD",
                Zeros16 = "2393F374C9F5E28B5CEC1E15B0D61114",
                QuickBrownFox = "3DEDE5965E71E3A16C7231C2A12B244F",
                Sequential0To255 = "C7BF2FFE16C9026C3FE93166ABD4D257",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        },
    };

    protected override SipHash128 CreateAlgorithm(SipHashVariant variant) =>
        variant switch
        {
            SipHashVariant.SipHash_2_4 => CreateAlgorithm(),
            SipHashVariant.SipHash_4_8 => new SipHash128
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
                "A3817F04BA25A8E66DF67214C7550293", "DA87C1D86B99AF44347659119B22FC45", "8177228DA4A45DC7FCA38BDEF60AFFE4", "9C70B60C5267A94E5F33B6B02985ED51",
                "F88164C12D9C8FAF7D0F6E7C7BCD5579", "1368875980776F8854527A07690E9627", "14EECA338B208613485EA0308FD7A15E", "A1F1EBBED8DBC153C0B84AA61FF08239",
                "3B62A9BA6258F5610F83E264F31497B4", "264499060AD9BAABC47F8B02BB6D71ED", 
            },
            SipHashVariant.SipHash_4_8 => new[]
            {
                "1F64CE586DA904E9CFECE85483A70A6C", "47345DA8EF4C79476AF27CA791C7A280", "E1495FA396CA2DC62273815F188221A4", "C7A273844AC54E835A9CB67F81057602",
                "541F52BBF43ECE4E2A95C8E01F656DEF", "17973BD40DF34815244F990CBF12BE5D", "6B0B360D563280CDB17D56C908E1F5FF", "ED00E13B184BF1C2726B8B54FFD2EEE0",
                "A7D946138FF9EDF5364A5A23AFCAE063", "9E7314B7545CECA38B9A5549E4FB0BE8", 
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };
}
