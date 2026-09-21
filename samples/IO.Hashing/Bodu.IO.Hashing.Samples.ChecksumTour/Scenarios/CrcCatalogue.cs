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
        SampleConsole.Scenario(
            "The CRC catalogue - one engine, every standard",
            what: "Reports the catalogue size, then computes the published check value for five standards across " +
                  "four widths, showing each one's polynomial and reflection settings, and resolves a standard by " +
                  "name.",
            why: "\"CRC-32\" names a family, not an algorithm. The width, polynomial, initial value, reflection " +
                 "and final XOR all vary between standards, and two implementations that disagree on any of them " +
                 "produce different digests for the same bytes - which is why interoperating with a device or " +
                 "format means matching its exact standard rather than reaching for whatever CRC is nearest. One " +
                 "parameterised engine plus a catalogue makes that a lookup instead of a reimplementation.",
            expect: "Each standard reproduces its published check value over the canonical \"123456789\" input, " +
                    "which is how a CRC implementation is conventionally verified. Note the digest bytes are " +
                    "little-endian, so CRC-32/ISO-HDLC prints 2639F4CB for the published 0xCBF43926.");

        var all = Enum.GetValues<CrcStandards>();
        Console.WriteLine($"  catalogue: {all.Length} standards, CRC-3 to CRC-64"
            + "  (every entry is a parameter row, not a code path - supporting a new protocol adds no implementation)");

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

            // Each row prints the parameters that define the standard beside the digest they
            // produce, so the digest is traceable to the parameterization rather than to the engine.
            Console.WriteLine(
                $"  {standard.Name,-18} width {standard.Size,2}, poly 0x{standard.Polynomial:X}, " +
                $"reflect {(standard.ReflectIn ? "in" : "--")}/{(standard.ReflectOut ? "out" : "---")} " +
                $"-> {Convert.ToHexString(digest)}");
        }

        Console.WriteLine("  (every digest above is that standard's published check value - reproducing them is how a CRC implementation is conventionally verified)");

        // Digest bytes follow the System.IO.Hashing little-endian convention: the published
        // CRC-32/ISO-HDLC check value 0xCBF43926 appears above as the byte sequence 26 39 F4 CB.
        Console.WriteLine("  (digest bytes are little-endian: 26 39 F4 CB above == the published check 0xCBF43926)");

        // The same standard can also be resolved from its catalogue name.
        var byName = CrcStandard.FromName("CRC-32/ISO-HDLC");
        Console.WriteLine($"  FromName(\"CRC-32/ISO-HDLC\") == CRC32_ISOHDLC -> {byName.Equals(CrcStandard.CRC32_ISOHDLC)}"
            + "  (expected True - a spec naming its CRC in prose resolves to the same value as the named constant)");

        Console.WriteLine();
    }
}
