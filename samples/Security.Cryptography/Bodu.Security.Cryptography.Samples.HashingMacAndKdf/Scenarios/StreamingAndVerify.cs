// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingAndVerify.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography.Samples.HashingMacAndKdf.Scenarios;

/// <summary>
/// Demonstrates incremental hashing with the <c>AppendData</c> streaming extension and the constant-time
/// <c>VerifyHash</c> comparison helper: a message hashed in fragments reproduces the one-shot digest, which
/// then verifies against the correct expected value and rejects a tampered one.
/// </summary>
public static class StreamingAndVerify
{
    private static readonly byte[] Message =
        Encoding.ASCII.GetBytes("stream this message in several fragments");

    /// <summary>
    /// Hashes the message incrementally, then verifies the digest against correct and corrupted expectations.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Incremental hashing and constant-time verify",
            what: "Hashes one message in three uneven chunks through AppendData, compares the result with the one-shot digest of the same bytes, then runs VerifyHash against a correct and a tampered expected digest.",
            why: "Streaming is how data too large to hold - a file, a socket - gets hashed, and it is only usable if the chunk boundaries cannot change the answer. VerifyHash compares in constant time, so a rejected digest does not leak through timing how many leading bytes were right.",
            expect: "The streamed and one-shot digests are identical (True) even though the message was split 10/15/15. VerifyHash prints True for the correct expectation and False for the tampered one.");

        // Feed the message to BLAKE2b in three fragments via the AppendData extension.
        using var streaming = new Blake2b(256);
        streaming.AppendData(Message.AsSpan(0, 10));
        streaming.AppendData(Message.AsSpan(10, 15));
        streaming.AppendData(Message.AsSpan(25));
        // Finalizing with an empty TransformFinalBlock completes the digest, exposed through the Hash property.
        streaming.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var streamedHex = Hex.ToHex(streaming.Hash!);

        // The same input hashed in a single call must produce the identical digest.
        using var oneShot = new Blake2b(256);
        var referenceHex = Hex.ToHex(oneShot.ComputeHash(Message));

        Console.WriteLine($"  streamed (10|15|15) : {streamedHex}");
        Console.WriteLine($"  one-shot            : {referenceHex}");
        Console.WriteLine($"  streaming == one-shot? {streamedHex == referenceHex}");
        Console.WriteLine();

        // VerifyHash re-hashes the input and compares in constant time; the correct value verifies.
        using var verifier = new Blake2b(256);
        var goodMatch = verifier.VerifyHash(Message, referenceHex);

        // Flip the first hex nibble to model a corrupted expected digest; verification must fail.
        var tamperedHex = (referenceHex[0] == '0' ? '1' : '0') + referenceHex[1..];
        using var verifier2 = new Blake2b(256);
        var badMatch = verifier2.VerifyHash(Message, tamperedHex);

        Console.WriteLine($"  VerifyHash(correct expected)  = {goodMatch}");
        Console.WriteLine($"  VerifyHash(tampered expected) = {badMatch}");

        Console.WriteLine();
    }
}
