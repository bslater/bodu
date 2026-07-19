// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Samples.CustomHash.Scenarios;

namespace Bodu.Security.Cryptography.Samples.CustomHash;

/// <summary>
/// Entry point for the custom-hash sample: a consumer-authored <see cref="AdditiveDigest" /> built on the
/// library's <c>BlockHashAlgorithm</c> base, hashed through the standard <c>HashAlgorithm</c> surface and
/// shown composing identically beside a shipped hash. Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Security.Cryptography.Samples.CustomHash");
        Console.WriteLine("============================================");
        Console.WriteLine();

        ImplementAndHash.Run();
        BesideTheBuiltIns.Run();

        Console.WriteLine("Done.");
    }
}
