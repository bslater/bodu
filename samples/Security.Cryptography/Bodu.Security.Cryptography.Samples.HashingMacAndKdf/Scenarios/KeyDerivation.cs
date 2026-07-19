// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyDerivation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Derives keys from fixed inputs with the library's key-derivation functions — HKDF (extract-then-expand),
/// the memory-hard password hash Argon2id, and scrypt — all with fixed salts so the derived keys reproduce
/// on every run.
/// </summary>
public static class KeyDerivation
{
    private static readonly byte[] Password = Encoding.ASCII.GetBytes("correct horse battery staple");
    private static readonly byte[] Salt = Encoding.ASCII.GetBytes("fixed-sample-salt");
    private static readonly byte[] Info = Encoding.ASCII.GetBytes("bodu-sample|v1|encryption-key");

    /// <summary>
    /// Runs each KDF over the fixed inputs and prints the 32-byte derived key as lowercase hex.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Key derivation (fixed salt) ---");
        Console.WriteLine();

        // HKDF: a two-step extract-then-expand KDF over a chosen HMAC hash. Cheap and deterministic.
        var hkdf = Hkdf.DeriveKey(HashAlgorithmName.SHA256, Password, outputLength: 32, salt: Salt, info: Info);
        Console.WriteLine($"  HKDF-SHA256          : {Hex.ToHex(hkdf)}");

        // Argon2id: a memory-hard password hash. Small cost parameters keep the sample fast.
        var argon2Parameters = new Argon2Parameters
        {
            MemoryKiB = 64,
            Iterations = 2,
            Parallelism = 1,
            TagLength = 32,
        };
        var argon2 = Argon2id.DeriveKey(Password, Salt, argon2Parameters);
        Console.WriteLine($"  Argon2id (64KiB,t=2) : {Hex.ToHex(argon2)}");

        // scrypt: the earlier memory-hard KDF, parameterised by cost N, block size r, and parallelism p.
        var scrypt = Scrypt.DeriveKey(Password, Salt, costN: 1024, blockSizeR: 8, parallelization: 1, length: 32);
        Console.WriteLine($"  scrypt (N=1024,r=8)  : {Hex.ToHex(scrypt)}");

        Console.WriteLine();
    }
}
