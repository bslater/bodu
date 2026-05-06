// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.Fnv164.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Fnv164" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fnv164Tests
    : FnvTests<Fnv164Tests, Fnv164>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        AlgorithmName = "FNV-1-64",
        MinNonZeroBytesForLongInput = 6,
        KnownAnswers = new()
        {
            Empty = "CBF29CE484222325",
            Abc = "D86FEA186B53126B",
            QuickBrownFox = "A8B2F3117DE37ACE",
            Zeros16 = "88201FB960FF6465",
            Sequential0To255 = "46F4BC763E8FD1BE",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented FNV-1 64-bit known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "CBF29CE484222325", "AF63BD4C8601B7DF", "08328807B4EB6FEC", "D94D11186C0F2E06",
        "4D22127F9DCB3431", "DC199FD92049AF47", "4939E4F1DD34D5A0", "A235A6FAE0C6FEE6",
        "6829A24BF22320D5", "21DF9C0C71B0C9E7", "3FC010252F67138C", "BA6EFB2F8C2636EE",
        "F0CBBFCB24EF5661", "198D472FC2AFC6DF", "1AD6D527D0AEECE0", "49F912A7993C80AE",
        "EEBB60C961CEA7A5", "770D1B313226DD4F", "72228398380A0D2C", "FAB2C7A7391461D6",
        "122B1725FDA23EB1", "8177068DFAB086D7", "ADC6FA40F9F51F40", "3E3A7C68BB8419D6",
        "417B3BF6A177E6B5", "BC4D99145EBD0597", "B4E0B49CFB307F8C", "8A5272BED368BAFE",
        "72D3F8413AF5BD81", "13EC57D72F91022F", "6B9B71A5D366B5C0", "3FD7DCC63786D55E",
        "02A182D05A1C8EA5", "94FFED091E86627F", "B542457EDE595DEC", "58FA0293D1DA9826",
        "0B6A872D967088B1", "D68C6376A13844E7", "C8D1E993F29D14A0", "D9C4866540EA0BC6",
        "F2FC240D4DB20155", "9472979B05784347", "B6FAE36A4B5A518C", "46A1F99E0A7890CE",
        "7DCBF58BCADE0E21", "9FA05D89B752023F", "8F8130028059D120", "3255B440189E594E",
        "25FA96E9D511BFA5", "9A8E14550D28A56F", "C811FB855C111BAC", "07AA0F9B71120576",
        "17FDFB21219F47B1", "63D96A4C21A6D1F7", "5141965D2E76C680", "89390055F3D34BB6",
        "FF27480D5009A675", "9965E39F0065D8F7", "0DFABE2DAD0FAB8C", "D0BCB39D11A07EDE",
        "512411E4F3B79301", "97DD690A20EECA8F", "FC040B35F5C230C0", "FD0FCCB098F8D67E",
        "FAAF4E13EED47825", "CC59D0DED308269F",
    };
}
