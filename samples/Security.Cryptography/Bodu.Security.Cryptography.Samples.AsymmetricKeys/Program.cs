// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Samples.AsymmetricKeys.Scenarios;

namespace Bodu.Security.Cryptography.Samples.AsymmetricKeys;

/// <summary>
/// Entry point for the asymmetric-key sample: X25519 key agreement and Ed25519 signatures from fixed
/// imported keys (fully reproducible), and the post-quantum ML-KEM and ML-DSA families with freshly
/// generated keys (where only the derived agreement / verification outcomes are deterministic and printed).
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Security.Cryptography.Samples.AsymmetricKeys");
        Console.WriteLine("================================================");
        Console.WriteLine();

        KeyAgreementX25519.Run();
        SignaturesEd25519.Run();
        KemMlKem.Run();
        SignaturesMlDsa.Run();

        Console.WriteLine("Done.");
    }
}
