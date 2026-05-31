// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein1024KnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="Skein1024" /> known-answer test vectors, transcribed verbatim from the Skein 1.3 / NIST
/// CD <c>skein_golden_kat.txt</c> reference distribution.
/// </summary>
/// <remarks>
/// Skein-1024-1024 ships with a richer message-length set covering empty, one-byte, one-block, and two-block messages.
/// The 384- and 512-bit truncations carry one 1024-bit incrementing-message vector each.
/// </remarks>
internal static class Skein1024KnownAnswers
{
    private static readonly KeyedHashAlgorithmKnownAnswer[] Empty = [];

    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />, or an empty array when the NIST CD KAT
    /// publishes no vectors for that (output, mode) combination.
    /// </summary>
    public static IReadOnlyList<KeyedHashAlgorithmKnownAnswer> For(Skein1024TestVariant variant) => variant switch
    {
        Skein1024TestVariant.Hash_384 => Hash384,
        Skein1024TestVariant.Mac_384 => Mac384,
        Skein1024TestVariant.Hash_512 => Hash512,
        Skein1024TestVariant.Mac_512 => Mac512,
        Skein1024TestVariant.Hash_1024 => Hash1024,
        Skein1024TestVariant.Mac_1024 => Mac1024,
        _ => Empty,
    };

    private const string IncrementingOneBlockSkein1024 =
        "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
        "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0" +
        "BFBEBDBCBBBAB9B8B7B6B5B4B3B2B1B0AFAEADACABAAA9A8A7A6A5A4A3A2A1A0" +
        "9F9E9D9C9B9A999897969594939291908F8E8D8C8B8A89888786858483828180";

    private const string IncrementingTwoBlocksSkein1024 =
        IncrementingOneBlockSkein1024 +
        "7F7E7D7C7B7A797877767574737271706F6E6D6C6B6A696867666564636261605F5E5D5C5B5A595857565554535251504F4E4D4C4B4A49484746454443424140" +
        "3F3E3D3C3B3A393837363534333231302F2E2D2C2B2A292827262524232221201F1E1D1C1B1A191817161514131211100F0E0D0C0B0A09080706050403020100";

    // 1024-bit truncation messages reuse the one-block sequence.
    private const string Incrementing1024Bits = IncrementingOneBlockSkein1024;

    private const string MacOneBlockSkein1024 =
        "D3090C72167517F7C7AD82A70C2FD3F6443F608301591E598EADB195E8357135" +
        "BA26FEDE2EE187417F816048D00FC23512737A2113709A77E4170C49A94B7FDF" +
        "F45FF579A72287743102E7766C35CA5ABC5DFE2F63A1E726CE5FBD2926DB03A2" +
        "DD18B03FC1508A9AAC45EB362440203A323E09EDEE6324EE2E37B4432C1867ED";

    private const string MacTwoBlocksSkein1024 =
        MacOneBlockSkein1024 +
        "696E6C9DB1E6ABEA026288954A9C2D5758D7C5DB7C9E48AA3D21CAE3D977A7C3926066AA393DBD538DD0C30DA8916C8757F24C18488014668A2627163A37B261" +
        "833DC2F8C3C56B1B2E0BE21FD3FBDB507B2950B77A6CC02EFB393E57419383A920767BCA2C972107AA61384542D47CBFB82CFE5C415389D1B0A2D74E2C5DA851";

    private const string Mac1024BitsSkein1024 = MacOneBlockSkein1024;

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash384 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Incrementing1024Bits),
            ExpectedHex =
                "A550F3071A8826044FF5F14E88AA86938087A10C155102C09D3B3E3BBF5C96B0" +
                "FE9C1C705E5D0BACCDC98FED102542E5",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac384 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Mac1024BitsSkein1024),
            Key = Convert.FromHexString(
                "CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E" +
                "920244C66E02D5F0DAD3E94C42BB65F0D14157DECF4105EF5609D5B0984457C1" +
                "935DF3061FF06E9F204192BA11E5BB2CAC0430C1C370CB3D113FEA5EC1021EB8" +
                "75E5946D7A96AC69A1626C6206B7252736F24253C9EE9B85EB852DFC814631346C" +
                "042EB4187AA1C015A4767032C0BB28F076B66485F51531C12E948F47DBC2CB90" +
                "4A4B75D1E8A6D931DAB4A07E0A54D1BB5B55E602141746BD09FB15E8F01A8D74" +
                "E9E63959CB37336BC1B896EC78DA734C15E362DB04368FBBA280F20A043E0D09" +
                "41E9F5193E1B360A33C43B266524880125222E648F05F28BE34BA3CABFC9C544"),
            ExpectedHex =
                "98AF454D7FA3706DFAAFBF58C3F9944868B57F68F493987347A69FCE19865FEB" +
                "BA0407A16B4E82065035651F0B1E0327",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash512 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Incrementing1024Bits),
            ExpectedHex =
                "57E3DE8CA38A69C9405ABF2A4063B4855C775B6D6C464725D325FAF27EB6F15F" +
                "086B11DA99E252ACFCF3BBE62E08BC10252850C40BB4766C32C10D998DB27B10",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac512 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Mac1024BitsSkein1024),
            Key = [],
            ExpectedHex =
                "0B50658B7F45ECC7CF211D5E2D16A8AE5764B28271C136C8B03C1CC308ABACE9" +
                "EECAFF8584CCE97A9AB75804B1250A30A76D69139B47A433E9FAEBE6A4B7DD10",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash1024 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_Empty",
            Profile = "NIST CD KAT",
            Input = [],
            ExpectedHex =
                "0FFF9563BB3279289227AC77D319B6FFF8D7E9F09DA1247B72A0A265CD6D2A62" +
                "645AD547ED8193DB48CFF847C06494A03F55666D3B47EB4C20456C9373C86297" +
                "D630D5578EBD34CB40991578F9F52B18003EFA35D3DA6553FF35DB91B81AB890" +
                "BEC1B189B7F52CB2A783EBB7D823D725B0B4A71F6824E88F68F982EEFC6D19C6",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_OneByte",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString("FF"),
            ExpectedHex =
                "E62C05802EA0152407CDD8787FDA9E35703DE862A4FBC119CFF8590AFE79250B" +
                "CCC8B3FAF1BD2422AB5C0D263FB2F8AFB3F796F048000381531B6F00D85161BC" +
                "0FFF4BEF2486B1EBCD3773FABF50AD4AD5639AF9040E3F29C6C931301BF79832" +
                "E9DA09857E831E82EF8B4691C235656515D437D2BDA33BCEC001C67FFDE15BA8",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_OneBlock",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(IncrementingOneBlockSkein1024),
            ExpectedHex =
                "1F3E02C46FB80A3FCD2DFBBC7C173800B40C60C2354AF551189EBF433C3D85F9" +
                "FF1803E6D920493179ED7AE7FCE69C3581A5A2F82D3E0C7A295574D0CD7D217C" +
                "484D2F6313D59A7718EAD07D0729C24851D7E7D2491B902D489194E6B7D369DB" +
                "0AB7AA106F0EE0A39A42EFC54F18D93776080985F907574F995EC6A37153A578",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_TwoBlocks",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(IncrementingTwoBlocksSkein1024),
            ExpectedHex =
                "842A53C99C12B0CF80CF69491BE5E2F7515DE8733B6EA9422DFD676665B5FA42" +
                "FFB3A9C48C217777950848CECDB48F640F81FB92BEF6F88F7A85C1F7CD1446C9" +
                "161C0AFE8F25AE444F40D3680081C35AA43F640FD5FA3C3C030BCC06ABAC01D0" +
                "98BCC984EBD8322712921E00B1BA07D6D01F26907050255EF2C8E24F716C52A5",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac1024 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_OneBlock",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(MacOneBlockSkein1024),
            Key = Convert.FromHexString(
                "CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E" +
                "920244C66E02D5F0DAD3E94C42BB65F0D14157DECF4105EF5609D5B0984457C1"),
            ExpectedHex =
                "211AC479E9961141DA3AAC19D320A1DBBBFAD55D2DCE87E6A345FCD58E368275" +
                "97378432B482D89BAD44DDDB13E6AD86E0EE1E0882B4EB0CD6A181E9685E18DD" +
                "302EBB3AA74502C06254DCADFB2BD45D288F82366B7AFC3BC0F6B1A3C2E8F84D" +
                "37FBEDD07A3F8FCFF84FAF24C53C11DA600AAA118E76CFDCB366D0B3F7729DCE",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_TwoBlocks",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(MacTwoBlocksSkein1024),
            Key = Convert.FromHexString(
                "CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E" +
                "920244C66E02D5F0DAD3E94C42BB65F0D14157DECF4105EF5609D5B0984457C1" +
                "935DF3061FF06E9F204192BA11E5BB2CAC0430C1C370CB3D113FEA5EC1021EB8" +
                "75E5946D7A96AC69A1626C6206B7252736F24253C9EE9B85EB852DFC814631346C"),
            ExpectedHex =
                "46A42B0D7B8679F8FCEA156C072CF9833C468A7D59AC5E5D326957D60DFE1CDF" +
                "B27EB54C760B9E049FDA47F0B847AC68D6B340C02C39D4A18C1BDFECE3F405FA" +
                "E8AA848BDBEFE3A4C277A095E921228618D3BE8BD1999A071682810DE748440A" +
                "D416A97742CC9E8A9B85455B1D76472CF562F525116698D5CD0A35DDF86E7F8A",
        },
    ];
}
