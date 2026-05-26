// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="Tiger" /> in its canonical
/// 192-bit Tiger1 (original Anderson/Biham reference) form. The contract base verifies that ComputeHash
/// reproduces the documented digest and that the requested output length is honoured. Bespoke Tiger
/// coverage (Tiger2 variant, 128/160-bit truncations, incremental input sequences) lives in
/// <c>TigerTests.*</c>.
/// </summary>
[TestClass]
public sealed class TigerContractTests : CryptoHashContractTests<Tiger>
{
    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using Tiger algorithm = new()
        {
            Variant = TigerHashingVariant.Tiger,
        };
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } = new CryptoHashKat[]
    {
        new(
            "Tiger1 empty",
            Input: Array.Empty<byte>(),
            ExpectedDigest: Convert.FromHexString("3293AC630C13F0245F92BBB1766E16167A4E58492DDE73F3"),
            OutputLengthBytes: 24,
            Key: null),

        new(
            "Tiger1 'ABC'",
            Input: System.Text.Encoding.ASCII.GetBytes("ABC"),
            ExpectedDigest: Convert.FromHexString("C7188DAEE93509ECCE198DE5A43C8DB47210DB7E8D8BB8DD"),
            OutputLengthBytes: 24,
            Key: null),

        new(
            "Tiger1 quick brown fox",
            Input: System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"),
            ExpectedDigest: Convert.FromHexString("6D12A41E72E644F017B6F0E2F7B44C6285F06DD5D2C5B075"),
            OutputLengthBytes: 24,
            Key: null),
    };
}
