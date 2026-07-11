// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Samples.CustomCheckDigit.Scenarios;

namespace Bodu.IO.Hashing.Samples.CustomCheckDigit;

/// <summary>
/// Entry point for the custom check-digit sample: implementing the library's
/// <c>CheckDigitAlgorithm</c> contract yourself — a weighted mod-10 SKU scheme — and exercising
/// it through its own surface and beside the built-in algorithms. The companion
/// <c>*.CustomCheckDigit.Test</c> project derives the shared
/// <c>CheckDigitContractTests&lt;T&gt;</c> base to prove the implementation.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.IO.Hashing.Samples.CustomCheckDigit");
        Console.WriteLine("========================================");
        Console.WriteLine();

        IssueAndValidate.Run();
        BesideTheBuiltIns.Run();

        Console.WriteLine("Done.");
    }
}
