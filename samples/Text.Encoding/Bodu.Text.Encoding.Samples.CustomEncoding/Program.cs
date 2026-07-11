// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Samples.Text.Encoding.CustomEncoding.Scenarios;

namespace Bodu.Samples.Text.Encoding.CustomEncoding;

/// <summary>
/// Entry point for the custom-encoding sample: implementing <c>IBinaryEncoding</c> yourself —
/// a Base36 codec — and exercising it through the same interface the built-in catalogue
/// exposes. The companion <c>*.CustomEncoding.Test</c> project derives the library's
/// <c>BinaryEncodingContractTests&lt;T&gt;</c> base to prove the implementation honours the
/// shared contract.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Encoding.Samples.CustomEncoding");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        EncodeDecode.Run();
        RegistryComparison.Run();

        Console.WriteLine("Done.");
    }
}
