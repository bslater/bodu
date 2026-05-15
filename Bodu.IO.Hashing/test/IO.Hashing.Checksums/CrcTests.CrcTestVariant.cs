// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.CrcTestVariant.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

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
