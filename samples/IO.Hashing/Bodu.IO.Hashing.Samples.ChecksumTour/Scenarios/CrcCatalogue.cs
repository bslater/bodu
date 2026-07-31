// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcCatalogue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.Samples.ChecksumTour.Scenarios;

/// <summary>
/// Demonstrates the parametric CRC design: one <see cref="Crc" /> engine, driven by an immutable
/// <see cref="CrcStandard" /> parameter bundle, covers the entire RevEng catalogue — so "which
/// CRC does this protocol use?" is answered by picking a catalogue entry, never by writing a new
/// implementation.
/// </summary>
public static class CrcCatalogue
{
    /// <summary>
    /// Enumerates the catalogue and runs several well-known standards over the RevEng check input.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- The CRC catalogue: one engine, every standard ---");

        var all = Enum.GetValues<CrcStandards>();
        Console.WriteLine($"catalogue: {all.Length} standards, CRC-3 to CRC-64");

        // "123456789" is the input every RevEng catalogue entry publishes its check value for.
        var checkInput = "123456789"u8;

        // Each CrcStandard carries the full CRC parameterization - width, polynomial, initial value,
        // input/output reflection, and final XOR - so the one engine reproduces any catalogue entry.
        foreach (var standard in new[]
        {
            CrcStandard.CRC8_SMBUS,
            CrcStandard.CRC16_MODBUS,
            CrcStandard.CRC16_XMODEM,
            CrcStandard.CRC32_ISOHDLC,
            CrcStandard.CRC64_XZ,
        })
        {
            var crc = new Crc(standard);
            var digest = crc.ComputeHash(checkInput); // one-shot convenience over Append + GetHashAndReset

            Console.WriteLine(
                $"  {standard.Name,-18} width {standard.Size,2}, poly 0x{standard.Polynomial:X}, " +
                $"reflect {(standard.ReflectIn ? "in" : "--")}/{(standard.ReflectOut ? "out" : "---")} " +
                $"-> {Convert.ToHexString(digest)}");
        }

        // Digest bytes follow the System.IO.Hashing little-endian convention: the published
        // CRC-32/ISO-HDLC check value 0xCBF43926 appears above as the byte sequence 26 39 F4 CB.
        Console.WriteLine("(digest bytes are little-endian: 26 39 F4 CB above == the published check 0xCBF43926)");

        // The same standard can also be resolved from its catalogue name.
        var byName = CrcStandard.FromName("CRC-32/ISO-HDLC");
        Console.WriteLine($"FromName(\"CRC-32/ISO-HDLC\") == CRC32_ISOHDLC -> {byName.Equals(CrcStandard.CRC32_ISOHDLC)}");

        Console.WriteLine();
    }
}
