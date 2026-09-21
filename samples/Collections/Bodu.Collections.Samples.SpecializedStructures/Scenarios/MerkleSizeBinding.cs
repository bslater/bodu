// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleSizeBinding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

using Bodu.Collections.Specialized;

namespace Bodu.Collections.Samples.SpecializedStructures.Scenarios;

/// <summary>
/// Demonstrates RFC 6962's tree-size ambiguity and the length-bound root that closes it —
/// <see cref="Rfc6962MerkleTree.BindRoot" /> paired with <see cref="Rfc6962MerkleTree.VerifyInclusionBound" />.
/// </summary>
public static class MerkleSizeBinding
{
    /// <summary>The block size the scenario cuts its input into.</summary>
    private const int BlockSize = 4;

    /// <summary>
    /// Shows the unbound verifier accepting an understated tree size, then shows the bound verifier rejecting the
    /// same claim.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Rfc6962MerkleTree: length-bound roots ---");

        var tree = new Rfc6962MerkleTree(SHA256.Create);

        // A 16-byte payload cut into four 4-byte blocks - a four-leaf tree.
        var input = Payload(16);
        var blocks = MerkleBlockedInputs.Cut(input, BlockSize);

        var root = tree.ComputeRoot(blocks);
        var path = MerkleCommitments.ToPath(tree.AuthenticationPath(blocks, 0));
        var block0 = input.AsSpan(0, BlockSize);

        Console.WriteLine($"  blocks        : {blocks.Count} x {BlockSize} bytes");
        Console.WriteLine($"  root          : {Hex.Full(root)}");

        // Here is the ambiguity. VerifyInclusion's treeSize is *trusted input* - it is not authenticated by the root.
        // A four-leaf tree's path for leaf 0 has exactly the length a three-leaf tree's first path wants and walks to
        // the same head, so the verifier cannot tell the two claims apart. Both of these return true.
        Console.WriteLine($"  VerifyInclusion(size=4): {tree.VerifyInclusion(root, 4, 0, block0, path)} (the true size)");
        Console.WriteLine($"  VerifyInclusion(size=3): {tree.VerifyInclusion(root, 3, 0, block0, path)} (understated - still accepted)");
        Console.WriteLine("  ^ this is RFC 6962's verifier working as specified, not a defect.");

        // Why it matters: a holder of a four-block object that has lost block 3 could declare a three-block object,
        // never be challenged for block 3, and pass every audit for ever.
        //
        // BindRoot closes it by hashing the byte length into the published root: H(0x02 || root || length). A root
        // bound to one length names one object and no other.
        var bound16 = tree.BindRoot(root, 16);
        var bound12 = tree.BindRoot(tree.ComputeRoot(MerkleBlockedInputs.Cut(Payload(12), BlockSize)), 12);

        Console.WriteLine($"  BindRoot(.., 16): {Hex.Full(bound16)}");
        Console.WriteLine($"  BindRoot(.., 12): {Hex.Full(bound12)}");
        Console.WriteLine($"  bound roots differ: {Hex.Full(bound16) != Hex.Full(bound12)}");

        // The bound verifier takes the claimed byte length as well as the claimed leaf count, rebuilds the bound root
        // from both, and compares. The truth verifies; every misstatement fails closed.
        Console.WriteLine($"  bound(len=16, size=4): {tree.VerifyInclusionBound(bound16, 16, 4, 0, block0, path)} (the truth)");
        Console.WriteLine($"  bound(len=12, size=3): {tree.VerifyInclusionBound(bound16, 12, 3, 0, block0, path)} (a whole block understated)");
        Console.WriteLine($"  bound(len=15, size=4): {tree.VerifyInclusionBound(bound16, 15, 4, 0, block0, path)} (off by one byte)");
        Console.WriteLine($"  bound(len=17, size=4): {tree.VerifyInclusionBound(bound16, 17, 4, 0, block0, path)} (overstated)");

        Console.WriteLine();
    }

    /// <summary>
    /// Returns the first <paramref name="length" /> bytes of the sequence <c>0x00, 0x01, 0x02, …</c>.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <returns>The generated payload.</returns>
    internal static byte[] Payload(int length)
    {
        var bytes = new byte[length];
        for (var index = 0; index < length; index++) bytes[index] = (byte)(index & 0xFF);

        return bytes;
    }
}
