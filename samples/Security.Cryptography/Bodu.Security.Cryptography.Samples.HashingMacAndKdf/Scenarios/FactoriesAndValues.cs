// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FactoriesAndValues.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Demonstrates the two supporting surfaces a consumer composing hashes reaches for: the
/// <see cref="IHashAlgorithmFactory{T}" /> seam, which supplies a fresh algorithm on demand, and
/// <see cref="HashValue" />, a comparable value type for a digest.
/// </summary>
public static class FactoriesAndValues
{
    /// <summary>The fixed message every digest is taken over.</summary>
    private static readonly byte[] Message = Encoding.ASCII.GetBytes("factories and values");

    /// <summary>
    /// Builds algorithms through a factory, then parses and compares digests as values.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Hash factories and hash values ---");

        RunFactories();
        RunHashValues();

        Console.WriteLine();
    }

    /// <summary>
    /// Shows why a factory rather than an instance is the right parameter shape for a reusable component.
    /// </summary>
    private static void RunFactories()
    {
        Console.WriteLine("  IHashAlgorithmFactory<T>:");

        // A HashAlgorithm is stateful and not thread-safe, so a component that hashes on behalf of its caller cannot
        // take one as a parameter and hold it - two concurrent calls would corrupt each other. Taking a *factory*
        // instead lets the component create and dispose one per operation, which is exactly why MerkleTree's
        // constructor takes Func<HashAlgorithm> rather than a HashAlgorithm.
        IHashAlgorithmFactory<Blake2b> factory = HashAlgorithmFactory.From(() => new Blake2b());

        using (var first = factory.Create())
        using (var second = factory.Create())
        {
            Console.WriteLine($"    two independent instances: {!ReferenceEquals(first, second)}");
            Console.WriteLine($"    same digest from each     : {Hex.ToHex(first.ComputeHash(Message)) == Hex.ToHex(second.ComputeHash(Message))}");
        }

        // From returns the concrete DelegateHashAlgorithmFactory<T>, so a caller that wants the concrete type has it,
        // while a component can still accept the interface.
        var delegating = HashAlgorithmFactory.From(() => new Blake2s(256));
        Console.WriteLine($"    factory type              : {delegating.GetType().Name.Split('`')[0]}<{nameof(Blake2s)}>");

        using (var blake2s = delegating.Create())
            Console.WriteLine($"    Blake2s-256 via factory   : {Hex.ToHex(blake2s.ComputeHash(Message))}");

        // The delegate can carry configuration, which keeps a parameterised algorithm behind the same seam.
        var configured = HashAlgorithmFactory.From(() => new Tiger { Variant = TigerHashingVariant.Tiger2 });
        using (var tiger2 = configured.Create())
            Console.WriteLine($"    configured Tiger2         : {Hex.ToHex(tiger2.ComputeHash(Message))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Shows a digest treated as a comparable value rather than a loose byte array.
    /// </summary>
    private static void RunHashValues()
    {
        Console.WriteLine("  HashValue:");

        using var algorithm = new Blake2b();
        var digest = algorithm.ComputeHash(Message);

        // A byte[] compares by reference, so == on two digests is almost always wrong. HashValue is a value type with
        // structural equality, which makes a digest safe to compare, use as a dictionary key, and pass around.
        var value = HashValue.FromBytes(digest);
        var sameAgain = HashValue.FromBytes(algorithm.ComputeHash(Message));

        Console.WriteLine($"    byte[] == byte[]  : {ReferenceEquals(digest, algorithm.ComputeHash(Message))} (reference comparison - the trap)");
        Console.WriteLine($"    HashValue equality: {value == sameAgain}");
        Console.WriteLine($"    length            : {value.Length} bytes, IsEmpty={value.IsEmpty}");

        // ParseHex / TryParseHex read the wire form back, so an expected digest from a manifest or a config file
        // becomes a comparable value without hand-rolled hex parsing.
        var parsed = HashValue.ParseHex(Hex.ToHex(digest));
        Console.WriteLine($"    round-trips hex   : {parsed == value}");

        Console.WriteLine($"    TryParseHex valid : {HashValue.TryParseHex(Hex.ToHex(digest), out var ok)} -> matches: {ok == value}");
        Console.WriteLine($"    TryParseHex odd   : {HashValue.TryParseHex("abc", out _)} (odd length rejected without throwing)");
        Console.WriteLine($"    TryParseHex junk  : {HashValue.TryParseHex("zz", out _)} (non-hex rejected without throwing)");

        // A different message gives a different value, and the inequality operator agrees.
        var other = HashValue.FromBytes(algorithm.ComputeHash(Encoding.ASCII.GetBytes("something else")));
        Console.WriteLine($"    different message : {value != other}");
    }
}
