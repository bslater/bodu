// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BesideTheBuiltIns.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.CustomHash.Scenarios;

/// <summary>
/// Shows the payoff of deriving <see cref="BlockHashAlgorithm" />: the custom <see cref="AdditiveDigest" />
/// is a drop-in <see cref="HashAlgorithm" /> peer. A harness typed against the base class drives it and a
/// shipped hash identically, and the streaming <c>AppendData</c> extension proves the incremental path
/// reproduces the one-shot digest.
/// </summary>
public static class BesideTheBuiltIns
{
    private static readonly byte[] Message =
        Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog");

    /// <summary>
    /// Runs the custom hash and a built-in <see cref="Tiger" /> hash through the same base-class-typed
    /// code path, then verifies streaming equals one-shot for the custom hash.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AdditiveDigest beside a shipped hash ---");

        // A single loop over base-class-typed instances drives the custom hash and a shipped one alike.
        var algorithms = new (string Label, HashAlgorithm Algorithm)[]
        {
            ("AdditiveDigest", new AdditiveDigest()),
            ("Tiger/192", new Tiger()),
        };

        foreach (var (label, algorithm) in algorithms)
        {
            using (algorithm)
            {
                var oneShot = algorithm.ComputeHash(Message);
                Console.WriteLine($"  {label,-14} : {ToHex(oneShot)}");
            }
        }

        Console.WriteLine();

        // Feed the message in two fragments via the AppendData streaming extension, then finalize.
        using var streaming = new AdditiveDigest();
        streaming.AppendData(Message.AsSpan(0, 19)); // "The quick brown fox"
        streaming.AppendData(Message.AsSpan(19));    // " jumps over the lazy dog"
        streaming.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var streamed = streaming.Hash!;

        // The same input hashed in one call must match the fragmented streaming result.
        using var oneShotDigest = new AdditiveDigest();
        var reference = oneShotDigest.ComputeHash(Message);

        Console.WriteLine($"  streamed 19|24 : {ToHex(streamed)}");
        Console.WriteLine($"  one-shot       : {ToHex(reference)}");
        Console.WriteLine($"  streaming == one-shot? {streamed.AsSpan().SequenceEqual(reference)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats a byte array as a lowercase hex string.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lowercase hex representation.</returns>
    private static string ToHex(byte[] bytes) =>
        Convert.ToHexString(bytes).ToLowerInvariant();
}
