// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Samples.HybridEncryption.Scenarios;

namespace Bodu.Security.Cryptography.Samples.HybridEncryption;

/// <summary>
/// Entry point for the HPKE sample: RFC 9180 Hybrid Public Key Encryption — the single-shot seal/open pair, the four
/// establishment modes and what each authenticates, the multi-message sender and receiver contexts, and the suite
/// surface with secret export including the export-only AEAD.
/// </summary>
/// <remarks>
/// HPKE draws a fresh ephemeral key inside every setup, so encapsulations and ciphertexts differ on every run by
/// design. The scenarios therefore print only what does not vary — round-trip results, sizes, suite identifiers, and
/// rejection outcomes — which keeps the sample deterministic without misrepresenting the construction.
/// </remarks>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Security.Cryptography.Samples.HybridEncryption");
        Console.WriteLine("===================================================");
        Console.WriteLine();

        SingleShot.Run();
        EstablishmentModes.Run();
        MultiMessageContext.Run();
        SuitesAndExport.Run();

        Console.WriteLine("Done.");
    }
}
