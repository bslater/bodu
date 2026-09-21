// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Samples.CipherModesAndPadding.Scenarios;

namespace Bodu.Security.Cryptography.Samples.CipherModesAndPadding;

/// <summary>
/// Entry point for the cipher-modes-and-padding sample: the layer beneath <c>SymmetricAlgorithm.Mode</c> — the
/// <c>IPaddingStrategy</c> implementations as standalone objects, the confidentiality mode transforms driven directly
/// over an <c>IBlockCipher</c>, and the specialist modes (ciphertext stealing, XTS, and the nonce-misuse-resistant
/// authenticated modes). Keys and IVs are fixed so every line of output is reproducible.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Security.Cryptography.Samples.CipherModesAndPadding");
        Console.WriteLine("=======================================================");
        Console.WriteLine();

        PaddingStrategies.Run();
        ModeTransforms.Run();
        SpecialistModes.Run();

        Console.WriteLine("Done.");
    }
}
