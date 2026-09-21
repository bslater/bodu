// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PasswordHashing.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Demonstrates the password-hashing surface that <see cref="KeyDerivation" /> does not reach: the three Argon2
/// variants side by side, the PHC-encoded <c>Hash</c> / <c>Verify</c> pair intended for credential storage, and
/// <see cref="ScryptParameters" /> as a named parameter set.
/// </summary>
public static class PasswordHashing
{
    /// <summary>The password every derivation is taken over.</summary>
    private static readonly byte[] Password = Encoding.UTF8.GetBytes("correct horse battery staple");

    /// <summary>A fixed salt. In real use this is random per credential and stored alongside the hash.</summary>
    private static readonly byte[] Salt = Encoding.ASCII.GetBytes("fixed-password-salt");

    /// <summary>Deliberately small cost parameters so the sample stays fast.</summary>
    private static readonly Argon2Parameters Parameters = new()
    {
        MemoryKiB = 64,
        Iterations = 2,
        Parallelism = 1,
        TagLength = 32,
    };

    /// <summary>
    /// Derives under each Argon2 variant, then round-trips a stored credential and shows scrypt's parameter object.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Password hashing (Argon2 variants and encoded hashes) ---");

        // Argon2 has three variants, and the choice is about what the attacker is assumed to have.
        //   Argon2d  - data-dependent memory access: strongest against GPU cracking, but its access pattern leaks
        //              through timing, so it is unsafe where side channels matter.
        //   Argon2i  - data-independent access: side-channel resistant, weaker against time-memory trade-offs.
        //   Argon2id - a hybrid, and the one to reach for by default (RFC 9106's recommendation).
        Console.WriteLine($"  parameters    : {Parameters.MemoryKiB} KiB, {Parameters.Iterations} iterations, parallelism {Parameters.Parallelism}, {Parameters.TagLength}-byte tag");
        Console.WriteLine($"  Argon2d       : {Hex.ToHex(Argon2d.DeriveKey(Password, Salt, Parameters))}");
        Console.WriteLine($"  Argon2i       : {Hex.ToHex(Argon2i.DeriveKey(Password, Salt, Parameters))}");
        Console.WriteLine($"  Argon2id      : {Hex.ToHex(Argon2id.DeriveKey(Password, Salt, Parameters))} (the default choice)");
        Console.WriteLine("  ^ three different functions over one password - the variant is not a tuning knob.");

        Console.WriteLine();
        RunEncodedHash();
        RunScryptParameters();

        Console.WriteLine();
    }

    /// <summary>
    /// Shows the PHC string format that carries its own parameters, and verification against it.
    /// </summary>
    private static void RunEncodedHash()
    {
        Console.WriteLine("  Encoded hashes (what you actually store):");

        // DeriveKey returns raw bytes, which leaves the caller to store the salt and every cost parameter alongside
        // them - and to keep them in step when the costs are raised later. Hash instead returns the PHC string
        // format, which carries the variant, version, parameters and salt inside the value itself.
        var encoded = Argon2id.Hash(Password, Salt, Parameters);
        Console.WriteLine($"    stored value: {encoded}");

        // Verify re-derives using the parameters read back out of the string, so old credentials keep verifying after
        // the cost parameters are raised for new ones.
        Console.WriteLine($"    correct password: {Argon2id.Verify(encoded, Password)}");
        Console.WriteLine($"    wrong password  : {Argon2id.Verify(encoded, Encoding.UTF8.GetBytes("correct horse battery stapl3"))}");

        // The variant is part of the encoded value, so a hash produced by one variant does not verify under another.
        var argon2iEncoded = Argon2i.Hash(Password, Salt, Parameters);
        Console.WriteLine($"    Argon2i value   : {argon2iEncoded}");
        Console.WriteLine($"    verified by Argon2i : {Argon2i.Verify(argon2iEncoded, Password)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Shows scrypt driven by a named parameter set rather than four positional integers.
    /// </summary>
    private static void RunScryptParameters()
    {
        Console.WriteLine("  ScryptParameters:");

        // The static DeriveKey overloads take costN, blockSizeR and parallelization as bare integers, which is easy to
        // transpose and impossible to read at the call site. ScryptParameters names them, and its required init
        // properties mean none can be omitted. An instance binds one parameter set once and exposes it through
        // Parameters, so a configured cost can be passed around and inspected rather than re-specified per call.
        var parameters = new ScryptParameters
        {
            CostN = 1024,
            BlockSizeR = 8,
            Parallelization = 1,
        };

        var scrypt = new Scrypt(parameters);
        var derived = scrypt.GetBytes(Password, Salt, 32);

        Console.WriteLine($"    bound parameters: N={scrypt.Parameters.CostN}, r={scrypt.Parameters.BlockSizeR}, p={scrypt.Parameters.Parallelization}");
        Console.WriteLine($"    derived key     : {Hex.ToHex(derived)}");

        // The same instance serves repeated derivations, and agrees with the positional static overload.
        var again = scrypt.GetBytes(Password, Salt, 32);
        var viaStatic = Scrypt.DeriveKey(Password, Salt, costN: 1024, blockSizeR: 8, parallelization: 1, length: 32);
        Console.WriteLine($"    reusable        : {Hex.ToHex(again) == Hex.ToHex(derived)}, matches static overload: {Hex.ToHex(viaStatic) == Hex.ToHex(derived)}");

        // Raising the cost changes the output, which is why the parameters have to travel with a stored hash.
        var costlier = new Scrypt(new ScryptParameters { CostN = 2048, BlockSizeR = 8, Parallelization = 1 });
        Console.WriteLine($"    N=2048 (2x cost): {Hex.ToHex(costlier.GetBytes(Password, Salt, 32))} (a different value, as it must be)");
    }
}
