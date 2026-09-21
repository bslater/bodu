// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SizeBinding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees.Scenarios;

/// <summary>
/// Demonstrates RFC 6962's tree-size ambiguity and the length-bound root that closes it —
/// <see cref="MerkleTree.BindRoot" /> paired with <see cref="MerkleTree.VerifyInclusionBound" />.
/// </summary>
public static class SizeBinding
{
    /// <summary>The block size the scenario cuts its input into.</summary>
    private const int BlockSize = 4;

    /// <summary>
    /// Shows the unbound verifier accepting an understated tree size, then shows the bound verifier rejecting it.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Length-bound roots (closing the tree-size ambiguity) ---");

        var tree = new MerkleTree(SHA256.Create);

        // A 16-byte payload cut into four 4-byte blocks - a four-leaf tree. These are the RFC 6962 appendix D vectors.
        var input = SampleLog.Payload(16);
        var blocks = SampleLog.Cut(input, BlockSize);

        var root = tree.ComputeRoot(blocks);
        var path = SampleLog.ToProof(tree.AuthenticationPath(blocks, 0));
        var block0 = input.AsSpan(0, BlockSize);

        Console.WriteLine($"  blocks        : {blocks.Count} x {BlockSize} bytes");
        Console.WriteLine($"  root          : {Hex.ToHex(root)}");

        // Here is the ambiguity. VerifyInclusion's treeSize is *trusted input* - the root does not authenticate it.
        // A four-leaf tree's path for leaf 0 has exactly the length a three-leaf tree's first path wants and walks to
        // the same head, so the verifier cannot tell the two claims apart. Both of these return true.
        Console.WriteLine($"  VerifyInclusion(size=4): {tree.VerifyInclusion(root, 4, 0, block0, path)} (the true size)");
        Console.WriteLine($"  VerifyInclusion(size=3): {tree.VerifyInclusion(root, 3, 0, block0, path)} (understated - still accepted)");
        Console.WriteLine("  ^ RFC 6962's verifier working as specified, not a defect.");

        // Why it matters: a holder of a four-block object that has lost block 3 could declare a three-block object,
        // never be challenged for block 3, and pass every audit for ever.
        //
        // BindRoot closes it by hashing the length into the commitment as H(0x02 || u64_be(length) || root) - an
        // addition to RFC 6962, not part of it. A root bound to one length names one object and no other.
        var bound16 = tree.BindRoot(root, 16);
        var bound12 = tree.BindRoot(tree.ComputeRoot(SampleLog.Cut(SampleLog.Payload(12), BlockSize)), 12);

        Console.WriteLine($"  BindRoot(.., 16): {Hex.ToHex(bound16)}");
        Console.WriteLine($"  BindRoot(.., 12): {Hex.ToHex(bound12)}");
        Console.WriteLine($"  bound roots differ: {Hex.ToHex(bound16) != Hex.ToHex(bound12)}");

        // The bound verifier takes the claimed byte length as well as the claimed leaf count, rebuilds the bound root
        // from both, and compares. The truth verifies; every misstatement fails closed.
        Console.WriteLine($"  bound(len=16, size=4): {tree.VerifyInclusionBound(bound16, 16, 4, 0, block0, path)} (the truth)");
        Console.WriteLine($"  bound(len=12, size=3): {tree.VerifyInclusionBound(bound16, 12, 3, 0, block0, path)} (a whole block understated)");
        Console.WriteLine($"  bound(len=15, size=4): {tree.VerifyInclusionBound(bound16, 15, 4, 0, block0, path)} (off by one byte)");
        Console.WriteLine($"  bound(len=17, size=4): {tree.VerifyInclusionBound(bound16, 17, 4, 0, block0, path)} (overstated)");

        // Binding the byte length is strictly stronger than binding the block count, because it also pins the final
        // block's length: these two inputs have the same block count but different bound roots.
        var bound13 = tree.BindRoot(tree.ComputeRoot(SampleLog.Cut(SampleLog.Payload(13), BlockSize)), 13);
        var bound14 = tree.BindRoot(tree.ComputeRoot(SampleLog.Cut(SampleLog.Payload(14), BlockSize)), 14);
        Console.WriteLine($"  13 and 14 bytes are both {MerkleTree.BlockCount(13, BlockSize)} blocks, bound roots differ: {Hex.ToHex(bound13) != Hex.ToHex(bound14)}");

        Console.WriteLine();
    }
}
