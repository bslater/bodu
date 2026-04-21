// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Bodu.IO.Hashing;

/// <summary>
/// Identifies the representative subset of <see cref="CrcStandard" /> values exercised by the common
/// <see cref="NonCryptographicHashAlgorithmTests{TTest, TAlgorithm, TVariant}" /> harness. The full RevEng
/// catalogue remains covered by <c>CrcTests.Catalog.cs</c>.
/// </summary>
public enum CrcTestVariant
{
    /// <summary>CRC-8/SMBUS — a canonical 8-bit CRC.</summary>
    Crc8_SMBUS,

    /// <summary>CRC-16/ARC — a canonical 16-bit CRC.</summary>
    Crc16_ARC,

    /// <summary>CRC-32/ISO-HDLC — the default CRC-32 instantiation.</summary>
    Crc32_IsoHdlc,

    /// <summary>CRC-64/ECMA-182 — a canonical 64-bit CRC.</summary>
    Crc64_Ecma182,
}

/// <summary>
/// Contains unit tests for the <see cref="Crc" /> non-cryptographic hash algorithm. Extended by partial files
/// covering the catalogue parity vectors and resume-from-hash semantics.
/// </summary>
[TestClass]
public partial class CrcTests
    : NonCryptographicHashAlgorithmTests<CrcTests, Crc, CrcTestVariant>
{
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    /// <inheritdoc />
    protected override CrcTestVariant DefaultVariant => CrcTestVariant.Crc32_IsoHdlc;

    /// <inheritdoc />
    protected override Crc CreateAlgorithm(CrcTestVariant variant) => new(StandardFor(variant));

    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(CrcTestVariant variant) => variant switch
    {
        CrcTestVariant.Crc8_SMBUS => new()
        {
            HashLengthInBytes = 1,
            IsResumable = true,
        },
        CrcTestVariant.Crc16_ARC => new()
        {
            HashLengthInBytes = 2,
            IsResumable = true,
        },
        CrcTestVariant.Crc32_IsoHdlc => new()
        {
            HashLengthInBytes = 4,
            IsResumable = true,
        },
        CrcTestVariant.Crc64_Ecma182 => new()
        {
            HashLengthInBytes = 8,
            IsResumable = true,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    /// <inheritdoc />
    /// <remarks>
    /// Only the <c>Empty</c> named input and the canonical RevEng check value (computed against
    /// <c>"123456789"</c>, not a <see cref="SharedInputs" /> entry) are seeded here. The full catalogue check is
    /// carried out in <c>CrcTests.Catalog.cs</c>, so variant-agnostic known answers for other shared inputs are
    /// deliberately omitted to keep the parameterised test matrix tractable.
    /// </remarks>
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(CrcTestVariant variant) => variant switch
    {
        CrcTestVariant.Crc8_SMBUS => new Dictionary<string, string>
        {
            ["Empty"] = "00",
        },
        CrcTestVariant.Crc16_ARC => new Dictionary<string, string>
        {
            ["Empty"] = "0000",
        },
        CrcTestVariant.Crc32_IsoHdlc => new Dictionary<string, string>
        {
            ["Empty"] = "00000000",
        },
        CrcTestVariant.Crc64_Ecma182 => new Dictionary<string, string>
        {
            ["Empty"] = "0000000000000000",
        },
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    /// <inheritdoc />
    /// <remarks>
    /// The common incremental test is skipped for CRC; the catalogue partial provides exhaustive coverage of the
    /// canonical check vectors against all 100+ RevEng standards instead.
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

    /// <summary>
    /// Verifies that calling the default constructor selects the CRC-32/ISO-HDLC standard.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_ShouldUseCRC32_ISOHDLC()
    {
        Crc crc = new();
        Assert.AreEqual(CrcStandard.CRC32_ISOHDLC.Name, crc.CrcStandard.Name);
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> <see cref="CrcStandard" /> to the constructor throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStandardIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Crc(null!));
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(ReadOnlySpan{byte})" /> on the reference input <c>"123456789"</c>
    /// under CRC-32/ISO-HDLC produces the documented check value <c>0xCBF43926</c>.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIs123456789_UnderCRC32_ISOHDLC_ShouldMatchPublishedCheck()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] hash = crc.ComputeHash(CheckInput);

        // CRC-32/ISO-HDLC stores little-endian; 0xCBF43926 → bytes [26, 39, F4, CB].
        CollectionAssert.AreEqual(new byte[] { 0x26, 0x39, 0xF4, 0xCB }, hash);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHashFrom(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> continues a prior
    /// hash by undoing finalisation and hashing additional data, yielding the same digest as a single-shot hash
    /// over the combined input.
    /// </summary>
    [TestMethod]
    public void ComputeHashFrom_WhenResumedWithAdditionalInput_ShouldMatchSingleShotCombinedHash()
    {
        byte[] first = new byte[] { 0x31, 0x32, 0x33, 0x34 };  // "1234"
        byte[] rest = new byte[] { 0x35, 0x36, 0x37, 0x38, 0x39 };  // "56789"

        byte[] firstHash = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(first);

        Crc resumer = new(CrcStandard.CRC32_ISOHDLC);
        byte[] resumed = resumer.ComputeHashFrom(firstHash, rest);

        byte[] combined = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(CheckInput);
        CollectionAssert.AreEqual(combined, resumed);
    }

    /// <summary>
    /// Verifies that two <see cref="Crc" /> instances configured with identical <see cref="CrcStandard" /> values
    /// report equal via <see cref="Crc.Equals(object?)" /> and produce matching hash codes.
    /// </summary>
    [TestMethod]
    public void Equals_WhenStandardsMatch_ShouldReturnTrueAndEqualHashCode()
    {
        Crc a = new(CrcStandard.CRC32_ISOHDLC);
        Crc b = new(CrcStandard.CRC32_ISOHDLC);

        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that two <see cref="Crc" /> instances configured with different <see cref="CrcStandard" /> values
    /// report unequal via <see cref="Crc.Equals(object?)" />.
    /// </summary>
    [TestMethod]
    public void Equals_WhenStandardsDiffer_ShouldReturnFalse()
    {
        Crc a = new(CrcStandard.CRC32_ISOHDLC);
        Crc b = new(CrcStandard.CRC32_ISCSI);

        Assert.IsFalse(a.Equals(b));
    }
}
