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
        InputBlockSize = 8,
        OutputBlockSize = 8,
        IsStateless = false,
        LongInputLength = 200,
        MinNonZeroBytesForLongInput = 6,   // 8 output bytes; SipHash has strong PRF properties so most bytes should be non-zero
        BoundaryLengths = [1, 8, 16, 64],
        MinKeyLength = 16,
        MaxKeyLength = 16,
        ValidKeyLengths = [16],
        TestKey = SipHashTestKey,
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

    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SipHashVariant variant) =>
        variant switch
        {
            SipHashVariant.SipHash_2_4 => new Dictionary<string, string>
            {
                ["Empty"] = "310E0EDD47DB6F72",
                ["ABC"] = "E848C7790EAA99A2",
                ["Zeros_16"] = "017755EFC0D3A098",
                ["QuickBrownFox"] = "E46F1FDC05612752",
                ["Sequential_0_255"] = "1AB24DC7FE69C1A9",
            },
            SipHashVariant.SipHash_4_8 => new Dictionary<string, string>
            {
                ["Empty"] = "41DA38992B0579C8",
                ["ABC"] = "618F256DAD66F19A",
                ["Zeros_16"] = "640CDE74B926C33E",
                ["QuickBrownFox"] = "70CB440B22B8D9F6",
                ["Sequential_0_255"] = "23ED19F6EF85AC97",
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
                "6224939A79F5F593", "B0E4A90BDF82009E", "F3B9DD94C5BB5D7A", "A7AD6B22462FB3F4",
                "FBE50E86BC8F1E75", "903D84C02756EA14", "EEF27A8E90CA23F7", "E545BE4961CA29A1",
                "DB9BC2577FCC2A3F", "9447BE2CF5E99A69", "9CD38D96F0B3C14B", "BD6179A71DC96DBB",
                "98EEA21AF25CD6BE", "C7673B2EB0CBF2D0", "883EA3E395675393", "C8CE5CCD8C030CA8",
                "94AF49F6C650ADB8", "EAB8858ADE92E1BC", "F315BB5BB835D817", "ADCF6B0763612E2F",
                "A5C91DA7ACAA4DDE", "716595876650A2A6", "28EF495C53A387AD", "42C341D8FA92D832",
                "CE7CF2722F512771", "E37859F94623F3A7", "381205BB1AB0E012", "AE97A10FD434E015",
                "B4A31508BEFF4D31", "81396229F0907902", "4D0CF49EE5D4DCCA", "5C73336A76D8BF9A",
                "D0A704536BA93E0E", "925958FCD6420CAD", "A915C29BC8067318", "952B79F3BC0AA6D4",
                "F21DF2E41D4535F9", "87577519048F53A9", "10A56CF5DFCD9ADB", "EB75095CCD986CD0",
                "51A9CB9ECBA312E6", "96AFADFC2CE666C7", "72FE52975A4364EE", "5A1645B276D592A1",
                "B274CB8EBF87870A", "6F9BB4203DE7B381", "EAECB2A30B22A87F", "9924A43CC1315724",
                "BD838D3AAFBF8DB7", "0B1A2A3265D51AEA", "135079A3231CE660", "932B2846E4D70666",
                "E1915F5CB1ECA46C", "F325965CA16D629F", "575FF28E60381BE5", "724506EB4C328A95",
                "D8CA02850BC4D2AC", "2459A291DBBEB636", 
            },
            SipHashVariant.SipHash_4_8 => new[]
            {
                "41DA38992B0579C8", "51B89552F91459C8", "923716F0BEDDC333", "6A46D47D6547C105",
                "C238592B4AC1FA48", "F6C2D7D9CF5247E1", "6BB6BC34C835558E", "47D73F715ABEFD4E",
                "20B58B9C072FDB50", "36319AF35EE11253", "48A9D0DB0A8D848F", "CC69396036040A81",
                "4B6D68537AA79761", "293796E9F2C95069", "88431BEAA7629A68", "E0A6A97DD589D383",
                "559CF55380B2AC70", "D5B7C5117AE3794E", "5A3C454634AD102B", "C0A480AFA35A3DBC",
                "78C22709E5284BC8", "EF2670460DEBD69D", "D976EF86A9D084D8", "E3D9811819EAD0E8",
                "89333CB53EEAEC16", "31156C5F647349C6", "A54CCE35357632A4", "065D8925C0A7D2FE",
                "2BBBAA82221A3A8B", "870BFBCE64097B70", "40D8E0F96495EE8B", "79FCA7F40BFADF12",
                "000BFBF22F769ED2", "40685591F8E522FA", "2BE6FE74D8149D0D", "BA7E2F0E0B7560ED",
                "02E9E384EDA7E197", "C4E80A62952763B6", "8327EDC65D5C6DD3", "79FC64D164A42FC0",
                "154A7511CBFC614E", "8B148D7CECA0E66F", "DFEE69B654C403FA", "C58F36A6697BB7C9",
                "A6C5BE9C05C63121", "B58A8759FBCD8931", "D7683A6704CCC425", "CB6AE6E1E5A2448D",
                "6E26695B3A3A5173", "787107CF9F33AC4A", "167590DAD97B7484", "006B681EF06BF306",
                "1C9B300266EFCFA6", "288D2F88D1B0B34B", "E01106BDACF56BFE", "C0101F0E5B6E0328",
                "C3A791455B1B1C0A", "5707AFE19E0B3A0F", "E65A7229FE53594F", "002F9DB9AB1AAF4C",
                "5928CB5044C10606", "D53801967B857321", "05DB364F1A0999CC", "E67784BC5503DE23",
                "F5B85D4C89A03FFC", "F9DE00DD83D997E7", 
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };
}
