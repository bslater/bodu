// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecordCensus.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff.Samples.BiffBasics.Scenarios;

/// <summary>
/// Demonstrates the physical view: <see cref="BiffReader.Read" /> frames every record of the stream — the codec
/// names some of them through <see cref="BiffRecordType" />, and the rest are still counted by identifier — while
/// the <c>BOF</c> records establish the version and mark each substream.
/// </summary>
public static class RecordCensus
{
    /// <summary>
    /// Walks the whole workbook stream, listing substreams and tallying record types.
    /// </summary>
    /// <param name="stream">The workbook stream bytes.</param>
    public static void Run(byte[] stream)
    {
        Console.WriteLine("--- Record census ---");

        var reader = new BiffReader(stream);
        var counts = new SortedDictionary<ushort, int>();
        int records = 0;

        while (reader.Read())
        {
            records++;
            counts[reader.RecordId] = counts.GetValueOrDefault(reader.RecordId) + 1;

            // A BOF opens a substream: the globals first, then each sheet. The version is known from the first one.
            if (reader.RecordType == BiffRecordType.Bof)
            {
                BiffBofRecord bof = reader.GetBof();
                Console.WriteLine($"  BOF at {reader.RecordStartIndex,7}: {bof.Version} {bof.SubstreamType} (build {bof.Build}, year {bof.Year})");
            }
        }

        Console.WriteLine($"  {records} records, {stream.Length} bytes, version {reader.Version}, code page {reader.CodePage}");
        Console.WriteLine();
        Console.WriteLine("  Most frequent record types:");
        foreach ((ushort id, int count) in counts.OrderByDescending(pair => pair.Value).Take(8))
        {
            var type = (BiffRecordType)id;
            string name = Enum.IsDefined(type) ? type.ToString() : "(not named by the codec)";
            Console.WriteLine($"    0x{id:X4} {name,-24} {count,6}");
        }

        Console.WriteLine();
    }
}
