// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Samples.ChecksumTour.Scenarios;

namespace Bodu.IO.Hashing.Samples.ChecksumTour;

/// <summary>
/// Entry point for the checksum-tour sample: the <c>Bodu.IO.Hashing</c> integrity surface — the
/// parametric CRC engine over its RevEng catalogue, the checksum families side by side, the
/// streaming and resumable APIs, and the classic non-cryptographic hash functions. Everything
/// runs offline against fixed inputs and the committed <c>Data/pangrams.txt</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.IO.Hashing.Samples.ChecksumTour");
        Console.WriteLine("====================================");
        Console.WriteLine();

        CrcCatalogue.Run();
        ChecksumFamilies.Run();
        StreamingResumable.Run();
        NonCryptoHashes.Run();

        Console.WriteLine("Done.");
    }
}
