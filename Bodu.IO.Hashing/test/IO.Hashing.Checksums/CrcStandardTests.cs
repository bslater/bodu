// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for <see cref="CrcStandard" /> covering construction guards, equality semantics, and the
/// <see cref="System.Runtime.Serialization.ISerializable" /> round-trip contract.
/// </summary>
[TestClass]
public partial class CrcStandardTests
{

    /// <summary>
    /// The aliases the CRC RevEng catalogue publishes for the standards <see cref="CrcStandards" /> covers, each with
    /// the standard it names: 74 aliases of 39 standards, as the catalogue lists them beside each canonical name.
    /// </summary>
    private static readonly ValidKat<string, CrcStandards>[] s_publishedAliases =
    [
        new("CRC-4/ITU", "CRC-4/ITU", CrcStandards.CRC4_G704),
        new("CRC-5/EPC", "CRC-5/EPC", CrcStandards.CRC5_EPCC1G2),
        new("CRC-5/ITU", "CRC-5/ITU", CrcStandards.CRC5_G704),
        new("CRC-6/ITU", "CRC-6/ITU", CrcStandards.CRC6_G704),
        new("CRC-7", "CRC-7", CrcStandards.CRC7_MMC),
        new("CRC-8/ITU", "CRC-8/ITU", CrcStandards.CRC8_I4321),
        new("CRC-8/MAXIM", "CRC-8/MAXIM", CrcStandards.CRC8_MAXIMDOW),
        new("DOW-CRC", "DOW-CRC", CrcStandards.CRC8_MAXIMDOW),
        new("CRC-8", "CRC-8", CrcStandards.CRC8_SMBUS),
        new("CRC-8/AES", "CRC-8/AES", CrcStandards.CRC8_TECH3250),
        new("CRC-8/EBU", "CRC-8/EBU", CrcStandards.CRC8_TECH3250),
        new("CRC-10", "CRC-10", CrcStandards.CRC10_ATM),
        new("CRC-10/I-610", "CRC-10/I-610", CrcStandards.CRC10_ATM),
        new("CRC-11", "CRC-11", CrcStandards.CRC11_FLEXRAY),
        new("X-CRC-12", "X-CRC-12", CrcStandards.CRC12_DECT),
        new("CRC-12/3GPP", "CRC-12/3GPP", CrcStandards.CRC12_UMTS),
        new("CRC-15", "CRC-15", CrcStandards.CRC15_CAN),
        new("ARC", "ARC", CrcStandards.CRC16_ARC),
        new("CRC-16", "CRC-16", CrcStandards.CRC16_ARC),
        new("CRC-16/LHA", "CRC-16/LHA", CrcStandards.CRC16_ARC),
        new("CRC-IBM", "CRC-IBM", CrcStandards.CRC16_ARC),
        new("R-CRC-16", "R-CRC-16", CrcStandards.CRC16_DECTR),
        new("X-CRC-16", "X-CRC-16", CrcStandards.CRC16_DECTX),
        new("CRC-16/DARC", "CRC-16/DARC", CrcStandards.CRC16_GENIBUS),
        new("CRC-16/EPC", "CRC-16/EPC", CrcStandards.CRC16_GENIBUS),
        new("CRC-16/EPC-C1G2", "CRC-16/EPC-C1G2", CrcStandards.CRC16_GENIBUS),
        new("CRC-16/I-CODE", "CRC-16/I-CODE", CrcStandards.CRC16_GENIBUS),
        new("CRC-16/AUTOSAR", "CRC-16/AUTOSAR", CrcStandards.CRC16_IBM3740),
        new("CRC-16/CCITT-FALSE", "CRC-16/CCITT-FALSE", CrcStandards.CRC16_IBM3740),
        new("CRC-16/ISO-HDLC", "CRC-16/ISO-HDLC", CrcStandards.CRC16_IBMSDLC),
        new("CRC-16/ISO-IEC-14443-3-B", "CRC-16/ISO-IEC-14443-3-B", CrcStandards.CRC16_IBMSDLC),
        new("CRC-16/X-25", "CRC-16/X-25", CrcStandards.CRC16_IBMSDLC),
        new("CRC-B", "CRC-B", CrcStandards.CRC16_IBMSDLC),
        new("X-25", "X-25", CrcStandards.CRC16_IBMSDLC),
        new("CRC-A", "CRC-A", CrcStandards.CRC16_ISOIEC144433A),
        new("CRC-16/BLUETOOTH", "CRC-16/BLUETOOTH", CrcStandards.CRC16_KERMIT),
        new("CRC-16/CCITT", "CRC-16/CCITT", CrcStandards.CRC16_KERMIT),
        new("CRC-16/CCITT-TRUE", "CRC-16/CCITT-TRUE", CrcStandards.CRC16_KERMIT),
        new("CRC-16/V-41-LSB", "CRC-16/V-41-LSB", CrcStandards.CRC16_KERMIT),
        new("CRC-CCITT", "CRC-CCITT", CrcStandards.CRC16_KERMIT),
        new("KERMIT", "KERMIT", CrcStandards.CRC16_KERMIT),
        new("CRC-16/MAXIM", "CRC-16/MAXIM", CrcStandards.CRC16_MAXIMDOW),
        new("MODBUS", "MODBUS", CrcStandards.CRC16_MODBUS),
        new("CRC-16/IEC-61158-2", "CRC-16/IEC-61158-2", CrcStandards.CRC16_PROFIBUS),
        new("CRC-16/AUG-CCITT", "CRC-16/AUG-CCITT", CrcStandards.CRC16_SPIFUJITSU),
        new("CRC-16/BUYPASS", "CRC-16/BUYPASS", CrcStandards.CRC16_UMTS),
        new("CRC-16/VERIFONE", "CRC-16/VERIFONE", CrcStandards.CRC16_UMTS),
        new("CRC-16/ACORN", "CRC-16/ACORN", CrcStandards.CRC16_XMODEM),
        new("CRC-16/LTE", "CRC-16/LTE", CrcStandards.CRC16_XMODEM),
        new("CRC-16/V-41-MSB", "CRC-16/V-41-MSB", CrcStandards.CRC16_XMODEM),
        new("XMODEM", "XMODEM", CrcStandards.CRC16_XMODEM),
        new("ZMODEM", "ZMODEM", CrcStandards.CRC16_XMODEM),
        new("CRC-24", "CRC-24", CrcStandards.CRC24_OPENPGP),
        new("CRC-32Q", "CRC-32Q", CrcStandards.CRC32_AIXM),
        new("CRC-32D", "CRC-32D", CrcStandards.CRC32_BASE91D),
        new("CRC-32/AAL5", "CRC-32/AAL5", CrcStandards.CRC32_BZIP2),
        new("CRC-32/DECT-B", "CRC-32/DECT-B", CrcStandards.CRC32_BZIP2),
        new("B-CRC-32", "B-CRC-32", CrcStandards.CRC32_BZIP2),
        new("CKSUM", "CKSUM", CrcStandards.CRC32_CKSUM),
        new("CRC-32/POSIX", "CRC-32/POSIX", CrcStandards.CRC32_CKSUM),
        new("CRC-32/BASE91-C", "CRC-32/BASE91-C", CrcStandards.CRC32_ISCSI),
        new("CRC-32/CASTAGNOLI", "CRC-32/CASTAGNOLI", CrcStandards.CRC32_ISCSI),
        new("CRC-32/INTERLAKEN", "CRC-32/INTERLAKEN", CrcStandards.CRC32_ISCSI),
        new("CRC-32C", "CRC-32C", CrcStandards.CRC32_ISCSI),
        new("CRC-32/NVME", "CRC-32/NVME", CrcStandards.CRC32_ISCSI),
        new("CRC-32", "CRC-32", CrcStandards.CRC32_ISOHDLC),
        new("CRC-32/ADCCP", "CRC-32/ADCCP", CrcStandards.CRC32_ISOHDLC),
        new("CRC-32/V-42", "CRC-32/V-42", CrcStandards.CRC32_ISOHDLC),
        new("CRC-32/XZ", "CRC-32/XZ", CrcStandards.CRC32_ISOHDLC),
        new("PKZIP", "PKZIP", CrcStandards.CRC32_ISOHDLC),
        new("JAMCRC", "JAMCRC", CrcStandards.CRC32_JAMCRC),
        new("XFER", "XFER", CrcStandards.CRC32_XFER),
        new("CRC-64", "CRC-64", CrcStandards.CRC64_ECMA182),
        new("CRC-64/GO-ECMA", "CRC-64/GO-ECMA", CrcStandards.CRC64_XZ),
    ];

    /// <summary>
    /// Gets the published RevEng aliases as <c>[DynamicData]</c> rows.
    /// </summary>
    /// <value>One row per alias, carrying the alias as its input and the standard it names as its expected value.</value>
    public static IEnumerable<object[]> PublishedAliases =>
        s_publishedAliases.Select(kat => new object[] { kat });

    private static CrcStandard CreateReference(
        string name = "Test",
        int size = 32,
        ulong polynomial = 0x1021UL,
        ulong initialValue = 0xFFFFUL,
        bool reflectIn = true,
        bool reflectOut = true,
        ulong xOrOut = 0xFFFFUL)
        => new(name, size, polynomial, initialValue, reflectIn, reflectOut, xOrOut);

}
