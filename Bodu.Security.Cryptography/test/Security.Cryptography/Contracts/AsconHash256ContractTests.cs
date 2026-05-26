// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconHash256ContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Contracts;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Drives <see cref="CryptoHashContractTests{THash}" /> against <see cref="AsconHash256" /> with
/// representative empty / 'ABC' / quick-brown-fox reference digests from the NIST SP 800-232 ASCON
/// reference. Bespoke coverage (incremental input, finalisation reuse, lifecycle) remains in the
/// existing <c>AsconHash256Tests.*</c> partials.
/// </summary>
[TestClass]
public sealed class AsconHash256ContractTests : CryptoHashContractTests<AsconHash256>
{
    /// <inheritdoc />
    protected override byte[] ComputeHash(byte[] input, int outputLengthBytes, byte[]? key, byte[]? customization)
    {
        using AsconHash256 algorithm = new();
        return algorithm.ComputeHash(input);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<CryptoHashKat> KnownAnswers { get; } = new CryptoHashKat[]
    {
        new(
            "Ascon-Hash256 empty",
            Input: Array.Empty<byte>(),
            ExpectedDigest: Convert.FromHexString("0B3BE5850F2F6B98CAF29F8FDEA89B64A1FA70AA249B8F839BD53BAA304D92B2"),
            OutputLengthBytes: 32,
            Key: null),

        new(
            "Ascon-Hash256 'ABC'",
            Input: System.Text.Encoding.ASCII.GetBytes("ABC"),
            ExpectedDigest: Convert.FromHexString("AF5724830636A475C9843106DC4EE6414DC893635A45A9DE95805C36F8596DB2"),
            OutputLengthBytes: 32,
            Key: null),

        new(
            "Ascon-Hash256 quick brown fox",
            Input: System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"),
            ExpectedDigest: Convert.FromHexString("23414503BF4BDE7AD0E85AEC94C22AE2D7CD807996B537F9564FC2974053F139"),
            OutputLengthBytes: 32,
            Key: null),
    };
}
