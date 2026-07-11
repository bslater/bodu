// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Samples.CheckDigits.Scenarios;

namespace Bodu.IO.Hashing.Samples.CheckDigits;

/// <summary>
/// Entry point for the check-digit sample: the <c>Bodu.IO.Hashing.CheckDigits</c> identifier
/// surface — validating real-world identifier formats (IBAN, ISBN, EAN, payment cards, bank
/// routing numbers), generating the check digit for a payload, and the error classes different
/// schemes can and cannot detect. Everything runs offline over fixed example identifiers.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.IO.Hashing.Samples.CheckDigits");
        Console.WriteLine("===================================");
        Console.WriteLine();

        ValidateIdentifiers.Run();
        ComputeAndAppend.Run();
        TransposedDigits.Run();

        Console.WriteLine("Done.");
    }
}
