// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcCatalogKatTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.IO.Hashing.Contracts;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Drives <see cref="CrcCatalogKat" /> rows against the public <see cref="Crc" /> +
/// <see cref="CrcStandard" /> surface. Each KAT row carries the full RevEng-catalogue parameter set
/// (width, polynomial, initial value, reflect-in / reflect-out, XOR-out, and the published check)
/// and asserts that a custom <see cref="CrcStandard" /> constructed from those parameters hashes the
/// canonical input <c>"123456789"</c> to the documented check value.
///
/// <para>
/// The auto-generated <see cref="CrcTests.CatalogCheckVectors" /> data source remains the source of
/// truth for the full 187-entry catalogue. This KAT-driven test sits alongside it and exercises a
/// small representative subset through the parameter-based <see cref="CrcStandard" /> constructor,
/// proving that the standalone parameter API stays in sync with the named-standard catalogue.
/// </para>
/// </summary>
[TestClass]
public sealed class CrcCatalogKatTests
{
    private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

    private static IReadOnlyList<CrcCatalogKat> Kats { get; } =
    [
        new(
            Name: "CRC-32/ISO-HDLC",
            Width: 32,
            Polynomial: 0x04C11DB7UL,
            Init: 0xFFFFFFFFUL,
            RefIn: true,
            RefOut: true,
            XorOut: 0xFFFFFFFFUL,
            Check: 0xCBF43926UL),

        new(
            Name: "CRC-32/ISCSI (Castagnoli)",
            Width: 32,
            Polynomial: 0x1EDC6F41UL,
            Init: 0xFFFFFFFFUL,
            RefIn: true,
            RefOut: true,
            XorOut: 0xFFFFFFFFUL,
            Check: 0xE3069283UL),

        new(
            Name: "CRC-16/MODBUS",
            Width: 16,
            Polynomial: 0x8005UL,
            Init: 0xFFFFUL,
            RefIn: true,
            RefOut: true,
            XorOut: 0x0000UL,
            Check: 0x4B37UL),

        new(
            Name: "CRC-16/KERMIT",
            Width: 16,
            Polynomial: 0x1021UL,
            Init: 0x0000UL,
            RefIn: true,
            RefOut: true,
            XorOut: 0x0000UL,
            Check: 0x2189UL),

        new(
            Name: "CRC-8/SMBUS",
            Width: 8,
            Polynomial: 0x07UL,
            Init: 0x00UL,
            RefIn: false,
            RefOut: false,
            XorOut: 0x00UL,
            Check: 0xF4UL),
    ];

    /// <summary>
    /// Verifies that every <see cref="CrcCatalogKat" /> row, when used to construct a custom
    /// <see cref="CrcStandard" />, produces the documented check value for the canonical RevEng
    /// input <c>"123456789"</c>.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCatalogParametersSupplied_ShouldYieldDocumentedCheck()
    {
        foreach (CrcCatalogKat kat in Kats)
        {
            CrcStandard standard = new(
                name: kat.Name,
                size: kat.Width,
                polynomial: kat.Polynomial,
                initialValue: kat.Init,
                reflectIn: kat.RefIn,
                reflectOut: kat.RefOut,
                xOrOut: kat.XorOut);

            Crc crc = new(standard);
            byte[] hash = crc.ComputeHash(CheckInput);

            ulong actual = 0UL;
            for (int i = 0; i < hash.Length; i++)
                actual |= (ulong)hash[i] << (i * 8);

            Assert.AreEqual(
                kat.Check,
                actual,
                $"CRC catalog KAT '{kat.Name}': computed check 0x{actual:X} differs from expected 0x{kat.Check:X}.");
        }
    }
}
