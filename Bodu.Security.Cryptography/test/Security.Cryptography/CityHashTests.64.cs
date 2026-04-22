// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="CityHash64" /> hash algorithm.
/// </summary>
/// <remarks>
/// <para>
/// Known-answer test (KAT) vectors are encoded as upper-case hexadecimal strings representing the
/// little-endian 8-byte output produced by <see cref="CityHash64" />.
/// </para>
/// <para>
/// <b>KAT coverage status:</b> The empty-input vector is analytically exact — when <c>len</c> = 0,
/// the algorithm's <c>Hash64Len0to16</c> path returns the constant <c>K2</c>
/// (<c>0x9ae16a3b2f90404f</c>) directly, with no arithmetic. All remaining named-input and
/// incremental KAT values require running the implementation, as <see cref="CityHash64" /> involves
/// 64×64-bit multiply operations throughout every non-trivial code path. Populate the dictionaries in
/// <see cref="GetExpectedHashesForNamedInputs" /> and <see cref="GetExpectedHashesForIncrementalInput" />
/// by executing the following for each standard input, then hard-coding the result:
/// </para>
/// <code language="csharp">
/// using var alg = new CityHash64();
/// Console.WriteLine(Convert.ToHexString(alg.ComputeHash(input)));
/// </code>
/// <para>
/// The base-class <see cref="HashAlgorithmTests{TTest,TAlgorithm,TVariant}" /> automatically drives
/// KAT tests only for inputs whose names appear in both <see cref="HashAlgorithmTests{TTest,TAlgorithm,TVariant}.SharedInputs" />
/// and the dictionaries below, so partial population does not cause test failures — it simply limits coverage.
/// </para>
/// </remarks>
[TestClass]
public partial class CityHash64Tests
    : Security.Cryptography.CityHashTests<CityHash64Tests, CityHash64>
{
    /// <inheritdoc />
    protected override CityHash64 CreateAlgorithm() => new CityHash64();

    /// <inheritdoc />
    protected override CityHash64 CreateAlgorithm(SingleTestVariant variant) => CreateAlgorithm();

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new HashAlgorithmSpecification
        {
            HashSize = 64,
            InputBlockSize = 1,
            OutputBlockSize = 1,
            BoundaryLengths = [16, 32, 64, 65],
            MinNonZeroBytesForLongInput = 7,   // expect all but at most one byte to be non-zero
        };

    /// <inheritdoc />
    private static readonly IReadOnlyDictionary<string, byte[]> CustomInputs = new Dictionary<string, byte[]>
    {
        ["Tiger"] = Encoding.UTF8.GetBytes("Tiger")
    };

    protected override IEnumerable<KnownAnswerTest> GetTestVectors(SingleTestVariant variant)
    {
        return base.GetTestVectors(variant);
    }

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) =>
        new Dictionary<string, string>
        {
            ["Empty"] = "4F40902F3B6AE19A",
            ["ABC"] = "5A364348181F2D03",
            ["Zeros_16"] = "1558EDCF4D2D9030",
            ["QuickBrownFox"] = "16035F69EEA357AE",
            ["Sequential_0_255"] = "B81550581C6E4657",
        };

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
        new[]
        {
            "4F40902F3B6AE19A",  // []
            "544BE9F5ED5660BE",  // [0x00]
            "758D03ED6546A0C2",  // [0x00, 0x01]
            "9AA4EBE9223DA194",  // [0x00, 0x01, 0x02]
            "40E5588989FDBF82",  // [0x00, 0x01, 0x02, 0x03]
            "49C13277E8A9BFB4",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "33234AE9D8BCFD92",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "A1A6B00DF2BFE0A2",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "983BE9E8E1135AAD",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "4FD84A0E151E3781",  // ...
            "EAFFD6B2B24D709B",
            "DD3A801D3C2B21F3",
            "7D3DFCAE33DFD59F",
            "923C3D855CCFC5C1",
            "7146BBB27BA8D94B",
            "9DBD435955512A86",
        };
}
