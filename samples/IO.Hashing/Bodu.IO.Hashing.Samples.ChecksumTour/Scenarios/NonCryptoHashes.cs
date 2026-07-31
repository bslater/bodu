// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptoHashes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Hashing;

namespace Bodu.IO.Hashing.Samples.ChecksumTour.Scenarios;

/// <summary>
/// Demonstrates the classic non-cryptographic hash functions — FNV-1a, MurmurHash3, CityHash —
/// in their natural role: fast, well-distributed bucket assignment for sharding and hash tables.
/// These are NOT cryptographic: an adversary can craft collisions, so never use them for
/// signatures, passwords, or integrity against tampering (that is <c>Bodu.Security.Cryptography</c>'s job).
/// </summary>
public static class NonCryptoHashes
{
    /// <summary>
    /// Assigns fixed keys to shards with three hash functions and compares distributions.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Non-cryptographic hashes: bucket assignment (NOT security) ---");

        string[] keys = ["alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel"];
        const int shards = 4;

        foreach (var (name, factory) in new (string, Func<System.IO.Hashing.NonCryptographicHashAlgorithm>)[]
        {
            ("FNV-1a/32   ", () => new Fnv1a32()),
            ("Murmur3/32  ", () => new MurmurHash3_32()),
            ("CityHash/32 ", () => new CityHash32()),
        })
        {
            var assignment = keys.Select(key =>
            {
                var algorithm = factory();
                algorithm.Append(System.Text.Encoding.UTF8.GetBytes(key));

                // Digest bytes follow the System.IO.Hashing little-endian convention, so read them
                // back as a little-endian integer before taking the shard modulus.
                var hash = BinaryPrimitives.ReadUInt32LittleEndian(algorithm.GetHashAndReset());
                return $"{key}->{hash % shards}";
            });

            Console.WriteLine($"  {name}: {string.Join(' ', assignment)}");
        }

        Console.WriteLine("same keys, same shard, every run - deterministic routing without coordination.");

        Console.WriteLine();
    }
}
