// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Core.Samples.TextEncoding.Scenarios;

namespace Bodu.Core.Samples.TextEncoding;

/// <summary>
/// Entry point for the text-encoding sample: the <c>Bodu.Text</c> encoding surfaces from <c>Bodu.Core</c> —
/// <c>EncodingDetection</c> for byte-order-mark sniffing, <c>EncodingExtensions</c> for transcoding, preamble
/// handling and fallback selection, and <c>StringEncodingExtensions</c> for pooled / span-based string encoding.
/// It reads a handful of committed byte fixtures under <c>Data/</c>, so the output is identical on every run.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Core.Samples.TextEncoding");
        Console.WriteLine("==============================");
        Console.WriteLine();

        DetectEncoding.Run();
        TranscodeAndFallback.Run();
        PooledStringEncoding.Run();

        Console.WriteLine("Done.");
    }
}
