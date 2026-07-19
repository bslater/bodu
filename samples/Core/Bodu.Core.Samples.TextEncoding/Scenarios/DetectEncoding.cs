// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DetectEncoding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text;

namespace Bodu.Core.Samples.TextEncoding.Scenarios;

/// <summary>
/// Demonstrates <see cref="EncodingDetection" /> byte-order-mark sniffing over the committed <c>Data/</c>
/// fixtures. Each fixture holds the same phrase ("Hello, Bodu café") written in a different Unicode encoding;
/// <see cref="EncodingDetection.TryDetectByPreamble" /> reads only the leading bytes to name the encoding, and
/// the sample then strips the preamble and decodes the payload back to text.
/// </summary>
public static class DetectEncoding
{
    // The fixtures live next to the executable because the csproj copies Data/ to the output directory.
    private static readonly string DataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");

    /// <summary>
    /// Sniffs the BOM of each fixture, reports the detected encoding, and decodes the remaining bytes.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- EncodingDetection: byte-order-mark sniffing ---");

        // The three BOM-carrying fixtures plus one deliberately BOM-less file to show the negative case.
        foreach (var name in new[] { "utf8-bom.txt", "utf16le-bom.txt", "utf16be-bom.txt", "plain-utf8.txt" })
        {
            var bytes = File.ReadAllBytes(Path.Combine(DataDirectory, name));

            // TryDetectByPreamble inspects only the first few bytes and never allocates.
            if (EncodingDetection.TryDetectByPreamble(bytes, out var encoding))
            {
                // StripPreamble returns the payload with the BOM removed; decode that to recover the text.
                var payload = encoding.StripPreamble(bytes);
                var text = encoding.GetString(payload);
                Console.WriteLine($"{name,-16}: {encoding.GetDisplayName(),-22} BOM={encoding.GetPreambleLength()}B  text=\"{text}\"");
            }
            else
            {
                // No recognized BOM. There is no content heuristic in EncodingDetection, so the caller decides
                // the default - UTF-8 is the modern, safe assumption for a BOM-less byte stream.
                var text = Encoding.UTF8.GetString(bytes);
                Console.WriteLine($"{name,-16}: (no BOM)               fallback UTF-8   text=\"{text}\"");
            }
        }

        Console.WriteLine();
    }
}
