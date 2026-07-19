// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf;

/// <summary>
/// Entry point for the hashing / MAC / KDF sample: unkeyed cryptographic hashes, keyed hashes and a
/// one-time MAC, extendable-output functions, incremental hashing with verify, key-derivation functions,
/// and one-time passwords — all over fixed inputs and keys so every line of output is deterministic.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Security.Cryptography.Samples.HashingMacAndKdf");
        Console.WriteLine("==================================================");
        Console.WriteLine();

        CryptographicHashes.Run();
        KeyedHashesAndMac.Run();
        ExtendableOutput.Run();
        StreamingAndVerify.Run();
        KeyDerivation.Run();
        OneTimePasswords.Run();

        Console.WriteLine("Done.");
    }
}
