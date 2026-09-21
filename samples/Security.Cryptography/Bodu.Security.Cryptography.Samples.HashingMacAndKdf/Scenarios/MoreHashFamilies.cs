// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoreHashFamilies.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Demonstrates the hash families not covered by <see cref="CryptographicHashes" />: the 32-bit BLAKE2 variant, the
/// tunable CubeHash, the two Snefru digests, the Ascon hash and customizable-XOF family, and the algorithms whose
/// behaviour is selected by a variant property rather than a separate type.
/// </summary>
public static class MoreHashFamilies
{
    /// <summary>The fixed message every digest is taken over.</summary>
    private static readonly byte[] Message = System.Text.Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <summary>
    /// Prints a digest from each family, then shows the variant- and parameter-selected behaviours.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Further hash families ---");

        // BLAKE2s is the 32-bit sibling of BLAKE2b: a smaller state and word size, which makes it the better fit on
        // 32-bit and embedded targets. Its output size is chosen at construction, in bits.
        foreach (var size in new[] { 128, 160, 224, 256 })
        {
            using var blake2s = new Blake2s(size);
            Console.WriteLine($"  {$"Blake2s-{size}",-14}: {Hex.ToHex(blake2s.ComputeHash(Message))}");
        }

        // Snefru ships as two fixed-width digests rather than one parameterised type.
        using (var snefru128 = new Snefru128())
            Console.WriteLine($"  {"Snefru-128",-14}: {Hex.ToHex(snefru128.ComputeHash(Message))}");
        using (var snefru256 = new Snefru256())
            Console.WriteLine($"  {"Snefru-256",-14}: {Hex.ToHex(snefru256.ComputeHash(Message))}");

        // The Ascon permutation backs a whole family. AsconHash256 is the fixed-output hash; AsconHashA256 is the
        // reduced-round variant, faster but with a smaller security margin - the two must differ.
        using (var asconHash = new AsconHash256())
            Console.WriteLine($"  {"AsconHash256",-14}: {Hex.ToHex(asconHash.ComputeHash(Message))}");
        using (var asconHashA = new AsconHashA256())
            Console.WriteLine($"  {"AsconHashA256",-14}: {Hex.ToHex(asconHashA.ComputeHash(Message))} (reduced rounds - differs)");

        Console.WriteLine();
        RunCubeHash();
        RunCustomizableXof();
        RunVariantSelected();

        Console.WriteLine();
    }

    /// <summary>
    /// Shows CubeHash at its defaults and with its round and block parameters tuned.
    /// </summary>
    private static void RunCubeHash()
    {
        Console.WriteLine("  CubeHash is parameterised rather than fixed:");

        // CubeHash exposes its whole design space: initialization rounds, rounds per block, block size and
        // finalization rounds. The default is the CubeHash16/32 configuration; changing any parameter is a different
        // function, not a tuning knob that preserves output.
        using (var defaults = new CubeHash())
            Console.WriteLine($"    {$"default ({defaults.HashSize}b)",-13}: {Hex.ToHex(defaults.ComputeHash(Message))}");

        // Only the output size changes here, and a narrower digest is not a truncation of the wider one - the
        // finalization absorbs the requested size, so each is its own function.
        using (var narrower = new CubeHash(256))
            Console.WriteLine($"    {"256-bit out",-13}: {Hex.ToHex(narrower.ComputeHash(Message))}");

        // CubeHash8/1 - eight rounds per block over 1-byte blocks: the parameter set the original submission named as
        // its conservative option.
        using (var tuned = new CubeHash(initializationRounds: 8, rounds: 8, transformBlockSize: 1, finalizationRounds: 8, hashSize: 256))
            Console.WriteLine($"    {"8/1 tuned",-13}: {Hex.ToHex(tuned.ComputeHash(Message))} (a different function, not a faster one)");

        Console.WriteLine();
    }

    /// <summary>
    /// Shows the customizable Ascon XOF separating two domains that share a key and message.
    /// </summary>
    private static void RunCustomizableXof()
    {
        Console.WriteLine("  AsconCxof128 - a customizable XOF:");

        // A plain XOF gives one output stream per message. A *customizable* XOF takes a customization string as well,
        // so two uses that share the same message still produce unrelated output. That is domain separation done by
        // the primitive rather than by the caller prepending a label and hoping it cannot be confused with the data.
        var first = Squeeze("invoice-signing/v1");
        var second = Squeeze("audit-log/v1");
        var repeat = Squeeze("invoice-signing/v1");

        Console.WriteLine($"    \"invoice-signing/v1\": {Hex.ToHex(first)}");
        Console.WriteLine($"    \"audit-log/v1\"      : {Hex.ToHex(second)}");
        Console.WriteLine($"    differ             : {Hex.ToHex(first) != Hex.ToHex(second)} (same message, different domain)");
        Console.WriteLine($"    reproducible       : {Hex.ToHex(first) == Hex.ToHex(repeat)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Shows the algorithms whose behaviour is selected by a property rather than a distinct type.
    /// </summary>
    private static void RunVariantSelected()
    {
        Console.WriteLine("  Variant-selected behaviour:");

        // Tiger and Tiger2 differ only in the padding byte that starts the final block - one byte of specification,
        // an entirely different digest. The variant is a property rather than a separate type.
        foreach (var variant in new[] { TigerHashingVariant.Tiger, TigerHashingVariant.Tiger2 })
        {
            using var tiger = new Tiger { Variant = variant };
            Console.WriteLine($"    {$"Tiger ({variant})",-16}: {Hex.ToHex(tiger.ComputeHash(Message))}");
        }

        // Whirlpool went through three published revisions, and the earlier two remain implemented because data
        // hashed under them still exists. Only WhirlpoolInfo3 should be used for new work.
        foreach (var version in new[] { WhirlpoolVersion.WhirlpoolInfo1, WhirlpoolVersion.WhirlpoolInfo2, WhirlpoolVersion.WhirlpoolInfo3 })
        {
            using var whirlpool = new Whirlpool { Version = version };
            Console.WriteLine($"    {version,-16}: {Hex.ToHex(whirlpool.ComputeHash(Message))}");
        }
    }

    /// <summary>
    /// Squeezes a fixed number of bytes from a customized Ascon XOF over the shared message.
    /// </summary>
    /// <param name="customization">The customization string that separates this use from any other.</param>
    /// <returns>The squeezed output.</returns>
    private static byte[] Squeeze(string customization)
    {
        using var xof = new AsconCxof128();

        // Order matters: the customization string is absorbed into the initial state, so it must be supplied before
        // any message data.
        xof.Customize(System.Text.Encoding.ASCII.GetBytes(customization));
        xof.Absorb(Message);

        var output = new byte[32];
        xof.Squeeze(output);

        return output;
    }
}
