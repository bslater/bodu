// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Text;

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Crc" /> non-cryptographic hash algorithm. Extended by partial files
/// covering the catalogue parity vectors and resume-from-hash semantics.
/// </summary>
[TestClass]
public partial class CrcTests
    : NonCryptographicHashAlgorithmTests<CrcTests, Crc, CrcTestVariant>
{
    private static readonly byte[] RevEngCheckInput = Encoding.ASCII.GetBytes("123456789");

    /// <inheritdoc />
    protected override CrcTestVariant DefaultVariant => CrcTestVariant.Crc32_IsoHdlc;

    /// <inheritdoc />
    protected override Crc CreateAlgorithm(CrcTestVariant variant) => new(StandardFor(variant));

    /// <inheritdoc />
    /// <remarks>
    /// Each variant seeds <see cref="NonCryptographicHashKnownAnswers.Empty" /> with the algorithm's empty-input
    /// digest and contributes the RevEng catalogue <c>"123456789"</c> check vector via
    /// <see cref="NonCryptographicHashKnownAnswers.Additional" />. Broader shared-input coverage for CRC is
    /// deferred to <c>CrcTests.Catalog.cs</c>, which exercises every catalogued CRC standard in one pass.
    /// </remarks>
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(CrcTestVariant variant) => variant switch
    {
        CrcTestVariant.Crc8_SMBUS => new()
        {
            HashLengthInBytes = 1,
            IsResumable = true,
            KnownAnswers = new()
            {
                Empty = "00",
                Additional =
                [
                    new()
                    {
                        Name = "RevEng/123456789",
                        Input = RevEngCheckInput,
                        ExpectedHex = "F4",
                    },
                ],
            },
        },
        CrcTestVariant.Crc16_ARC => new()
        {
            HashLengthInBytes = 2,
            IsResumable = true,
            KnownAnswers = new()
            {
                Empty = "0000",
                Additional =
                [
                    new()
                    {
                        Name = "RevEng/123456789",
                        Input = RevEngCheckInput,
                        ExpectedHex = "3DBB",
                    },
                ],
            },
        },
        CrcTestVariant.Crc32_IsoHdlc => new()
        {
            HashLengthInBytes = 4,
            IsResumable = true,
            KnownAnswers = new()
            {
                Empty = "00000000",
                Additional =
                [
                    new()
                    {
                        Name = "RevEng/123456789",
                        Input = RevEngCheckInput,
                        ExpectedHex = "2639F4CB",
                    },
                ],
            },
        },
        CrcTestVariant.Crc64_Ecma182 => new()
        {
            HashLengthInBytes = 8,
            IsResumable = true,
            KnownAnswers = new()
            {
                Empty = "0000000000000000",
                Additional =
                [
                    new()
                    {
                        Name = "RevEng/123456789",
                        Input = RevEngCheckInput,
                        ExpectedHex = "4773490B5FDF406C",
                    },
                ],
            },
        },
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    /// <inheritdoc />
    /// <remarks>
    /// The common incremental test is skipped for CRC; the catalogue partial provides exhaustive coverage of
    /// the canonical check vectors against all 100+ RevEng standards instead.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(CrcTestVariant variant) => Array.Empty<string>();

    private static CrcStandard StandardFor(CrcTestVariant variant) => variant switch
    {
        CrcTestVariant.Crc8_SMBUS => CrcStandard.CRC8_SMBUS,
        CrcTestVariant.Crc16_ARC => CrcStandard.CRC16_ARC,
        CrcTestVariant.Crc32_IsoHdlc => CrcStandard.CRC32_ISOHDLC,
        CrcTestVariant.Crc64_Ecma182 => CrcStandard.CRC64_ECMA182,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    /// <summary>
    /// Verifies that <see cref="Crc.HashLengthInBytes" /> matches the width derived from the configured
    /// <see cref="CrcStandard.Size" /> (rounded up to the nearest byte) for the representative variants.
    /// </summary>
    [TestMethod]
    public void HashLengthInBytes_WhenConstructedWithStandard_ShouldBeWidthRoundedUp()
    {
        Assert.AreEqual(4, new Crc(CrcStandard.CRC32_ISOHDLC).HashLengthInBytes);
        Assert.AreEqual(2, new Crc(CrcStandard.CRC16_ARC).HashLengthInBytes);
        Assert.AreEqual(1, new Crc(CrcStandard.CRC8_SMBUS).HashLengthInBytes);
        Assert.AreEqual(8, new Crc(CrcStandard.CRC64_ECMA182).HashLengthInBytes);
    }
}
