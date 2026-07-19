// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImplementAndHash.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Samples.CustomHash.Scenarios;

/// <summary>
/// Instantiates the custom <see cref="AdditiveDigest" /> and hashes fixed inputs through the standard
/// <see cref="HashAlgorithm.ComputeHash(byte[])" /> surface, printing each digest as lowercase hex.
/// </summary>
public static class ImplementAndHash
{
    /// <summary>
    /// Computes digests for a handful of fixed messages, including the empty input, to show the derived
    /// type behaving as an ordinary <see cref="HashAlgorithm" />.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AdditiveDigest: a custom BlockHashAlgorithm ---");

        // The custom hash is created and consumed exactly like any shipped HashAlgorithm.
        using var digest = new AdditiveDigest();
        Console.WriteLine($"name       : {digest.AlgorithmName}");
        Console.WriteLine($"hash size  : {digest.HashSize} bits");
        Console.WriteLine();

        // Deterministic fixed inputs - identical bytes on every run yield identical digests.
        HashAndPrint(digest, "empty", Array.Empty<byte>());
        HashAndPrint(digest, "abc", Encoding.ASCII.GetBytes("abc"));
        HashAndPrint(digest, "fox", Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"));
        HashAndPrint(digest, "32 x 0xAB", CreateFilled(32, 0xAB));

        Console.WriteLine();
    }

    /// <summary>
    /// Hashes <paramref name="input" /> with the supplied algorithm and prints the labelled hex digest.
    /// </summary>
    /// <param name="algorithm">The hash algorithm to drive.</param>
    /// <param name="label">A short human-readable label for the input.</param>
    /// <param name="input">The message bytes to hash.</param>
    private static void HashAndPrint(HashAlgorithm algorithm, string label, byte[] input)
    {
        // ComputeHash resets the algorithm each call, so a single instance can be reused deterministically.
        var hash = algorithm.ComputeHash(input);
        Console.WriteLine($"  {label,-10} -> {ToHex(hash)}");
    }

    /// <summary>
    /// Creates a byte array of the given length filled with a single repeated value.
    /// </summary>
    /// <param name="length">The number of bytes.</param>
    /// <param name="value">The fill byte.</param>
    /// <returns>A newly allocated filled array.</returns>
    private static byte[] CreateFilled(int length, byte value)
    {
        var array = new byte[length];
        Array.Fill(array, value);
        return array;
    }

    /// <summary>
    /// Formats a byte array as a lowercase hex string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lowercase hex representation.</returns>
    private static string ToHex(byte[] bytes) =>
        Convert.ToHexString(bytes).ToLowerInvariant();
}
