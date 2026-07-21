// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Samples.SymmetricAndAead.Scenarios;

namespace Bodu.Security.Cryptography.Samples.SymmetricAndAead;

/// <summary>
/// Entry point for the symmetric / AEAD sample: block ciphers, block-cipher modes, the Ascon-AEAD128
/// authenticated cipher, AEAD modes over AES, and additive stream ciphers — all over fixed keys, IVs,
/// nonces, and associated data so every line of output is deterministic.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Security.Cryptography.Samples.SymmetricAndAead");
        Console.WriteLine("==================================================");
        Console.WriteLine();

        BlockCiphers.Run();
        CipherModes.Run();
        AeadAscon.Run();
        AeadModes.Run();
        StreamCiphers.Run();

        Console.WriteLine("Done.");
    }
}
