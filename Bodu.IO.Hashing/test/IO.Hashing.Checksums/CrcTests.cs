// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

using System.Text;

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
    protected override IEnumerable<string> GetIncrementalHashValue(CrcTestVariant variant) => variant switch
        {
            CrcTestVariant.Crc8_SMBUS => new[]
            {
                "00", "00", "07", "1B", "48", "E3", "BC", "2F",
                "D8", "3E", "85", "A4", "44", "FF", "D0", "14",
            },
            CrcTestVariant.Crc16_ARC => new[]
            {
                "0000", "0000", "C1C0", "8051", "10A1", "A10F", "0EBB", "BAC6",
                "0671", "F0C4", "0442", "C3C4", "C596", "5656", "17FB", "3ACA",
            },
            CrcTestVariant.Crc32_IsoHdlc => new[]
            {
                "00000000", "8DEF02D2", "6922DE36", "7F895408",
                "1386B98B", "CCD35A51", "4ACFEB30", "F90958AD",
                "9F68AA88", "0243E1BC", "46D76C45", "E18E2DAD",
                "65C97092", "B846FEE6", "C856EF69", "5E676CA0",
            },
            CrcTestVariant.Crc64_Ecma182 => new[]
            {
                "0000000000000000", "0000000000000000", "9336EAA9EBE1F042", "2FB25A00BCE9E42A",
                "F3CB1ECE9E6005F8", "B21D702AD45A8D58", "3215BDB1E4E5C7A8", "5415F7A09699D1A3",
                "CCC7A6A5DAE42AA2", "8B0767B5C7C1E307", "47D31426ADAE75BD", "960ABF2C9F8531A7",
                "72DC3CF1DC2150D0", "6C97A42914E9B6C1", "830C0E2133594341", "FBCC990CC7B91885",
                "553BAFAB912DC4F9", "11E4C9ACE0AA3EF4", "7095D1D82871ECD5", "8892B7B2748BAC4E",
                "322F32767C451689", "D0C0D1646DAA271F", "9864919E39622D30", "271231EDF36DCA2D",
                "A91D61CFB075AD77", "25ED4B42D6CB4EA7", "BB87EEC69AD3C24C", "399E9CC9BE4539B2",
                "3EA90075978753D3", "36050E7F53F83C63", "59C1581472B10E12", "616C064ADBD8E4D5",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
        };

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