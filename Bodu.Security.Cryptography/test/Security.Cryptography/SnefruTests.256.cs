// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SnefruTests.256.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Snefru256" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Snefru256Tests
    : Security.Cryptography.SnefruTests<Snefru256Tests, Snefru256>
{
    /// <inheritdoc />
    public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() =>
    [
        SingleTestVariant.Default
    ];

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        InputBlockSize = 1,
        OutputBlockSize = 1,
        IsStateless = false,
        LongInputLength = 200,
        BoundaryLengths = [1, 8, 16, 64],
        HashSize = 256,
        MinNonZeroBytesForLongInput = 13,   // 32 output bytes; ~40% threshold, conservative for Snefru
        KnownAnswers = new()
        {
            Empty = "A4DF4C0A4AF3DAD3B7E9F4200144F74D6F44F875AB32715F5664119D676F8D19",
            Abc = "BB01E1770CFBC7D39187A068274E9553E552DEDD354C4EC35506E1559A3FD15B",
            Zeros16 = "FD7F2B6794AF20F0BA2861A3155CF8E905811B2EA126A4C2B89D7D9FE9F70FB0",
            QuickBrownFox = "674CAA75F9D8FD2089856B95E93A4FB42FA6C8702F8980E11D97A142D76CB358",
            Sequential0To255 = "03216CBBB3014EBFA292F57DED01B93E2378D1001A03DA563A89B50F822140F0",
            Additional =
            [
                new HashAlgorithmKnownAnswer { Name = "a", Input = SnefruLetterAInput, ExpectedHex = "45161589AC317BE0CEBA70DB2573DDDA6E668A31984B39BF65E4B664B584C63D" },
                new HashAlgorithmKnownAnswer { Name = "1234567890", Input = SnefruRepeatedDigitsInput, ExpectedHex = "D5FCE38A152A2D9B83AB44C29306EE45AB0AED0E38C957EC431DAB6ED6BB71B8" },
            ],
        },
    };

    /// <inheritdoc />
    protected override Snefru256 CreateAlgorithm(SingleTestVariant variant) => new();

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        "A4DF4C0A4AF3DAD3B7E9F4200144F74D6F44F875AB32715F5664119D676F8D19", "D40C2A1AC28B11A875157CCB3BB2E75FBAC5138CA354005381080F67BCA0093B", "2D750C03D40A3DD50D581423F5FF80704846D8F292EA758EC0DF1CC980DB568A", "82F59BA1DF602A8816DB44C97012BB450C13D5DF819F89FFB55B560F71D7A99F",
        "892CDB1D966B2BE94CDD972742A2E301132ED2CEE2A98EE657A55EF62FA3AF87", "36D9CC40EE7E48CC2F6DC1E4EA690333DCD94CA90B936A83585E9EBA8B4492DD", "3C1359AFAA47DECCEEE51F23D84F08B51466CB9B30D766BC189A8EB5F5714B51", "E919DADD3E5A3006B71BC41A711EAE04686E819ABCDD1404B09ED8AE39F932B0",
        "C82D114072B9A438DC260E5B4520E8A7F5193866BCCA15C472654A01588D9E6C", "D6FE83E9347370286BBAE9C8057F6D1ADAF0562D4F6C6B5AA53E02F73C28A593", "73155AF9B40A31194FD3973D0FF3094ADDEF02D7D1499F9DDA971DAF69C8BC2B", "55EF00BEDAF6376853A3948AC6AB8A76E176C9469CECC46A4FFC293AB661C840",
        "FC3295D797BCD941D06E788EE0A2A1A3435726668F335CCC6081A444BA963DCF", "4796C91C0598FCF4A395783E79EF8F478BBD8D080A90E42DDA0991C6773D3487", "7FD08F6A104A7C074D137BCDAF6CC774EE702D4E62B2013117FD8587C9BA22F0", "59D61A0343A41E83C703BFCAB40EF950517CD154C903FE56B7007593EA79C539",
        "7A59B68845E8D8D1FE240873FAD650A32F9163C5091FEBC57B2214603BB504A4", "16DD55D3E3D27578CB16CC87C16C74BC46A7E556EBC452BF02C773BB5CF5578E",
    ];

}
