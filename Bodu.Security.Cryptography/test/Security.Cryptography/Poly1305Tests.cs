// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Poly1305" /> hash algorithm.
/// </summary>
[TestClass]
public partial class Poly1305Tests
    : KeyedBlockHashAlgorithmTests<Poly1305Tests, Poly1305, SingleTestVariant>
{

    private static readonly byte[] Poly1305TestKey = new byte[32]
    {
        0x85, 0xd6, 0xbe, 0x78, 0x57, 0x55, 0x6d, 0x33,
        0x7f, 0x44, 0x52, 0xfe, 0x42, 0xd5, 0x06, 0xa8,
        0x01, 0x03, 0x80, 0x8a, 0xfb, 0x0d, 0xb2, 0xfd,
        0x4a, 0xbf, 0xf6, 0xaf, 0x41, 0x49, 0xf5, 0x1b,
    };

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new KeyedAlgorithmSpecification
    {
        HashSize = 128,
        HashBlockSize = 16,
        IsStateless = false,
        LongInputLength = 208,
        MinNonZeroBytesForLongInput = 8,
        BoundaryLengths = [16, 32, 48, 64],
        MinKeyLength = 32,
        MaxKeyLength = 32,
        CanReuseTransform = false,
        TestKey = Poly1305TestKey,
        KnownAnswers = new()
        {
            Empty = "0103808AFB0DB2FD4ABFF6AF4149F51B",
            Abc = "22701EA05B6B7BB59C6EFAF002047EF8",
            Zeros16 = "268F6E95A4B8FA01E694DDC1D1D3FD25",
            QuickBrownFox = "2458137FE7781FB38D3782CE0D70BCA6",
            Sequential0To255 = "1212245DC231EF863720469237C5F17B",
        },
    };

    /// <summary>
    /// Verifies that <see cref="Poly1305.Poly1305" />, when UsingRfcTestVector, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Poly1305_WhenUsingRfcTestVector_ShouldMatch()
    {
        var key = Convert.FromHexString("85D6BE7857556D337F4452FE42D506A80103808AFB0DB2FD4ABFF6AF4149F51B");
        var message = Encoding.ASCII.GetBytes("Cryptographic Forum Research Group");
        var expected = Convert.FromHexString("A8061DC1305136C6C22B8BAF0C0127A9");

        using var poly = new Poly1305 { Key = key };
        var actual = poly.ComputeHash(message);

        Console.WriteLine("Actual   : " + Convert.ToHexString(actual));
        Console.WriteLine("Expected : " + Convert.ToHexString(expected));

        CollectionAssert.AreEqual(expected, actual);
    }

    protected override Poly1305 CreateAlgorithm(SingleTestVariant variant) => CreateAlgorithm();

    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
        new[]
        {
            "0103808AFB0DB2FD4ABFF6AF4149F51B", "0B8856490462076B4E3B3B025089CA22", "1092DB1FC36A5BC0BB3EB746A2970AF8", "A3A46A7B5832B8697EAF360739F8580D",
            "4A40878F8A86881A7DDFAA023EE1C79B", "09702BB6238F9BF38133487AB52A0319", "38C2E56254AD7AC5638CF184301E91A6", "9002C8A709E81D7BF4769E83A89C0079",
            "2E411A1AD9A562A380C69184FC818264", "70B33F7EDBFF28F22D29A080512BD5E9", "DA8F878A51920DC1865BD94D56D4D3A9", "11FFFEA7441A30305EBE905DE2E1D0FD",
            "688072BA37F4C9E2579EFD99C82CE74E", "B57E3C32E5BC8A8E9A22E6108AE9F06D", "2776E5446105293658F5F4010B308436", "D1F28598BC850CAAE6C4579B04BB4FA0",
            "A18A0DE2BA299128303A398E28BDE4F0", "37477D65160C3CA0466AAC5780785EF5",
        };

    public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
    {
        SingleTestVariant.Default
    };
}
