// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Samples.Text.Encoding.EncodingTour.Scenarios;

namespace Bodu.Samples.Text.Encoding.EncodingTour;

/// <summary>
/// Entry point for the encoding-tour sample: the <c>Bodu.Text.Encoding</c> catalogue — the base
/// families and their variants, the formatting/parse-style knobs, the checksummed schemes that
/// detect corruption (Base58Check, Bech32), the Guid convenience surface, and the
/// name-addressable <c>BinaryEncodings</c> registry. Every scenario is pure computation over
/// fixed payloads: offline and deterministic.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Encoding.Samples.EncodingTour");
        Console.WriteLine("=======================================");
        Console.WriteLine();

        VariantsTour.Run();
        FormattingAndStyles.Run();
        ChecksummedSchemes.Run();
        GuidConvenience.Run();
        EncodingRegistry.Run();

        Console.WriteLine("Done.");
    }
}
