// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc6962MerkleTreeTests.VerifyConsistency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic;

/// <summary>
/// Tests for
/// <see cref="Rfc6962MerkleTree.VerifyConsistency(ReadOnlySpan{byte}, long, ReadOnlySpan{byte}, long, IReadOnlyList{ReadOnlyMemory{byte}})" />.
/// </summary>
/// <remarks>
/// The negative cases follow the same systematic mutation approach as the inclusion tests, plus the three failures
/// specific to consistency: a shrinking log, swapped roots, and a non-empty proof between equal sizes.
/// </remarks>
public partial class Rfc6962MerkleTreeTests
{
    /// <summary>
    /// Verifies that each published consistency proof reconstructs both published roots.
    /// </summary>
    /// <param name="kat">The two sizes and the proof.</param>
    [TestMethod]
    [DynamicData(nameof(ReferenceConsistencyProofs), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void VerifyConsistency_WhenGivenAPublishedProof_ShouldAccept(MerkleConsistencyKat kat)
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] firstRoot = tree.ComputeRoot(TakeEntries(kat.FirstSize));
        byte[] secondRoot = tree.ComputeRoot(TakeEntries(kat.SecondSize));
        IReadOnlyList<ReadOnlyMemory<byte>> proof = kat.Proof
            .Select(step => (ReadOnlyMemory<byte>)Convert.FromHexString(step)).ToArray();

        Assert.IsTrue(tree.VerifyConsistency(firstRoot, kat.FirstSize, secondRoot, kat.SecondSize, proof));
    }

    /// <summary>
    /// Verifies that a generated proof round-trips for every pair of sizes with
    /// <c>0 &lt;= m &lt;= n &lt;= 32</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void VerifyConsistency_WhenProofIsGeneratedForAnySizePair_ShouldRoundTrip()
    {
        Rfc6962MerkleTree tree = CreateTree();

        for (int secondSize = 1; secondSize <= 32; secondSize++)
        {
            ReadOnlyMemory<byte>[] entries = new ReadOnlyMemory<byte>[secondSize];
            for (int index = 0; index < secondSize; index++)
                entries[index] = new byte[] { (byte)index, 0xA5 };

            byte[] secondRoot = tree.ComputeRoot(entries);

            for (int firstSize = 0; firstSize <= secondSize; firstSize++)
            {
                byte[] firstRoot = tree.ComputeRoot(entries[..firstSize]);
                IReadOnlyList<ReadOnlyMemory<byte>> proof = ToPath(tree.ConsistencyProof(entries, firstSize));

                Assert.IsTrue(
                    tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, proof),
                    $"{firstSize} -> {secondSize} must verify");
            }
        }
    }

    /// <summary>
    /// Verifies that a log cannot shrink: a second size below the first is rejected even when both roots and the
    /// proof are otherwise genuine.
    /// </summary>
    [TestMethod]
    public void VerifyConsistency_WhenSecondSizeIsBelowTheFirst_ShouldReject()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] smallRoot = tree.ComputeRoot(TakeEntries(3));
        byte[] largeRoot = tree.ComputeRoot(TakeEntries(7));
        IReadOnlyList<ReadOnlyMemory<byte>> proof = ToPath(tree.ConsistencyProof(TakeEntries(7), 3));

        Assert.IsTrue(tree.VerifyConsistency(smallRoot, 3, largeRoot, 7, proof), "the honest direction verifies");
        Assert.IsFalse(tree.VerifyConsistency(largeRoot, 7, smallRoot, 3, proof), "time travel must be rejected");
    }

    /// <summary>
    /// Verifies that swapping the two roots is rejected, since the proof reconstructs each one in its own position.
    /// </summary>
    [TestMethod]
    public void VerifyConsistency_WhenRootsAreSwapped_ShouldReject()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] firstRoot = tree.ComputeRoot(TakeEntries(3));
        byte[] secondRoot = tree.ComputeRoot(TakeEntries(7));
        IReadOnlyList<ReadOnlyMemory<byte>> proof = ToPath(tree.ConsistencyProof(TakeEntries(7), 3));

        Assert.IsFalse(tree.VerifyConsistency(secondRoot, 3, firstRoot, 7, proof));
    }

    /// <summary>
    /// Verifies that equal sizes require identical roots and an <em>empty</em> proof, so a non-empty proof offered
    /// between equal sizes is rejected rather than walked.
    /// </summary>
    [TestMethod]
    public void VerifyConsistency_WhenSizesAreEqual_ShouldRequireIdenticalRootsAndAnEmptyProof()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] root = tree.ComputeRoot(TakeEntries(5));
        byte[] other = tree.ComputeRoot(TakeEntries(4));

        Assert.IsTrue(tree.VerifyConsistency(root, 5, root, 5, []), "a tree is consistent with itself");
        Assert.IsFalse(tree.VerifyConsistency(root, 5, other, 5, []), "equal sizes with different roots");
        Assert.IsFalse(
            tree.VerifyConsistency(root, 5, root, 5, [root]),
            "a non-empty proof between equal sizes must be rejected");
    }

    /// <summary>
    /// Verifies that a first size of zero requires an empty proof, because every tree extends the empty tree and no
    /// evidence can say more.
    /// </summary>
    [TestMethod]
    public void VerifyConsistency_WhenFirstSizeIsZero_ShouldRequireAnEmptyProof()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] emptyRoot = tree.ComputeRoot([]);
        byte[] secondRoot = tree.ComputeRoot(TakeEntries(7));

        Assert.IsTrue(tree.VerifyConsistency(emptyRoot, 0, secondRoot, 7, []));
        Assert.IsFalse(tree.VerifyConsistency(emptyRoot, 0, secondRoot, 7, [secondRoot]));
    }

    /// <summary>
    /// Verifies that a proof between trees that do not share a prefix is rejected.
    /// </summary>
    [TestMethod]
    public void VerifyConsistency_WhenTreesDoNotShareAPrefix_ShouldReject()
    {
        Rfc6962MerkleTree tree = CreateTree();

        // A second tree whose first three entries differ from the reference tree's.
        ReadOnlyMemory<byte>[] divergent = new ReadOnlyMemory<byte>[7];
        for (int index = 0; index < 7; index++)
            divergent[index] = new byte[] { 0xFF, (byte)index };

        byte[] referenceFirstRoot = tree.ComputeRoot(TakeEntries(3));
        byte[] divergentSecondRoot = tree.ComputeRoot(divergent);
        IReadOnlyList<ReadOnlyMemory<byte>> proof = ToPath(tree.ConsistencyProof(divergent, 3));

        Assert.IsFalse(tree.VerifyConsistency(referenceFirstRoot, 3, divergentSecondRoot, 7, proof));
    }

    /// <summary>
    /// Verifies that every systematic corruption of a valid consistency proof is rejected, across many size pairs,
    /// and that none of them throws.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void VerifyConsistency_WhenProofIsCorrupted_ShouldRejectWithoutThrowing()
    {
        Rfc6962MerkleTree tree = CreateTree();

        for (int secondSize = 2; secondSize <= 16; secondSize++)
        {
            ReadOnlyMemory<byte>[] entries = new ReadOnlyMemory<byte>[secondSize];
            for (int index = 0; index < secondSize; index++)
                entries[index] = new byte[] { (byte)index, 0xA5 };

            byte[] secondRoot = tree.ComputeRoot(entries);

            for (int firstSize = 1; firstSize < secondSize; firstSize++)
            {
                byte[] firstRoot = tree.ComputeRoot(entries[..firstSize]);
                byte[][] valid = tree.ConsistencyProof(entries, firstSize);

                foreach ((string label, Func<bool> probe) in CorruptConsistencyProbes(
                    tree, firstRoot, firstSize, secondRoot, secondSize, valid))
                {
                    try
                    {
                        Assert.IsFalse(probe(), $"{firstSize} -> {secondSize} [{label}] must be rejected");
                    }
                    catch (AssertFailedException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"{firstSize} -> {secondSize} [{label}] threw {ex.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Verifies that a root of the wrong width is rejected rather than throwing.
    /// </summary>
    /// <param name="firstRootLength">The first root's length in bytes.</param>
    /// <param name="secondRootLength">The second root's length in bytes.</param>
    [TestMethod]
    [DataRow(16, 32)]
    [DataRow(32, 16)]
    [DataRow(0, 0)]
    public void VerifyConsistency_WhenARootIsNotDigestWidth_ShouldReject(int firstRootLength, int secondRootLength)
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.IsFalse(
            tree.VerifyConsistency(new byte[firstRootLength], 1, new byte[secondRootLength], 2, [new byte[32]]));
    }

    /// <summary>
    /// Verifies that a negative size is rejected rather than throwing, since it can arrive as a wire value.
    /// </summary>
    /// <param name="firstSize">The claimed earlier size.</param>
    /// <param name="secondSize">The claimed later size.</param>
    [TestMethod]
    [DataRow(-1L, 8L)]
    [DataRow(1L, -1L)]
    [DataRow(-2L, -1L)]
    public void VerifyConsistency_WhenASizeIsNegative_ShouldReject(long firstSize, long secondSize)
    {
        Rfc6962MerkleTree tree = CreateTree();

        Assert.IsFalse(tree.VerifyConsistency(new byte[32], firstSize, new byte[32], secondSize, []));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> proof throws rather than being treated as an empty one.
    /// </summary>
    [TestMethod]
    public void VerifyConsistency_WhenProofIsNull_ShouldThrowArgumentNullException()
    {
        Rfc6962MerkleTree tree = CreateTree();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = tree.VerifyConsistency(new byte[32], 1, new byte[32], 2, null!);
        });
    }

    /// <summary>
    /// Verifies that consistency verification returns a boolean and never throws for arbitrary malformed input.
    /// </summary>
    /// <remarks>
    /// Randomized over a fixed seed so a failure is reproducible, mirroring the fuzzing production RFC 6962
    /// implementations run against their consistency verifiers.
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    public void VerifyConsistency_WhenGivenArbitraryMalformedInput_ShouldReturnFalseAndNeverThrow()
    {
        Rfc6962MerkleTree tree = CreateTree();
        var random = new Random(Seed: 20260920);

        long[] sizes = [-1, 0, 1, 2, 3, 4, 7, 8, 16, long.MaxValue];
        int[] widths = [0, 1, 16, 31, 32, 33, 64];

        for (int iteration = 0; iteration < 4000; iteration++)
        {
            byte[] firstRoot = new byte[widths[random.Next(widths.Length)]];
            byte[] secondRoot = new byte[widths[random.Next(widths.Length)]];
            random.NextBytes(firstRoot);
            random.NextBytes(secondRoot);

            ReadOnlyMemory<byte>[] proof = new ReadOnlyMemory<byte>[random.Next(0, 7)];
            for (int step = 0; step < proof.Length; step++)
            {
                byte[] element = new byte[widths[random.Next(widths.Length)]];
                random.NextBytes(element);
                proof[step] = element;
            }

            long firstSize = sizes[random.Next(sizes.Length)];
            long secondSize = sizes[random.Next(sizes.Length)];

            try
            {
                _ = tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, proof);
            }
            catch (Exception ex)
            {
                Assert.Fail(
                    $"iteration {iteration} threw {ex.GetType().Name}: first={firstRoot.Length}B/{firstSize} " +
                    $"second={secondRoot.Length}B/{secondSize} steps={proof.Length}");
            }
        }
    }

    /// <summary>
    /// Enumerates every corruption of a valid consistency proof, each paired with a label for failure reporting.
    /// </summary>
    /// <param name="tree">The tree to verify with.</param>
    /// <param name="firstRoot">The earlier tree's correct root.</param>
    /// <param name="firstSize">The earlier tree's correct size.</param>
    /// <param name="secondRoot">The later tree's correct root.</param>
    /// <param name="secondSize">The later tree's correct size.</param>
    /// <param name="valid">The correct proof.</param>
    /// <returns>The labelled probes, each of which must return <see langword="false" />.</returns>
    private static IEnumerable<(string Label, Func<bool> Probe)> CorruptConsistencyProbes(
        Rfc6962MerkleTree tree,
        byte[] firstRoot,
        int firstSize,
        byte[] secondRoot,
        int secondSize,
        byte[][] valid)
    {
        IReadOnlyList<ReadOnlyMemory<byte>> proof = ToPath(valid);

        // Corrupt the earlier size.
        yield return ("size1 - 1", () => tree.VerifyConsistency(firstRoot, firstSize - 1, secondRoot, secondSize, proof));
        yield return ("size1 + 1", () => tree.VerifyConsistency(firstRoot, firstSize + 1, secondRoot, secondSize, proof));
        yield return ("size1 ^ 2", () => tree.VerifyConsistency(firstRoot, firstSize ^ 2, secondRoot, secondSize, proof));

        // Corrupt the later size.
        yield return ("size2 * 2", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize * 2L, proof));
        yield return ("size2 / 2", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize / 2L, proof));

        // Corrupt the roots.
        yield return ("wrong root1", () => tree.VerifyConsistency(Flip(firstRoot, 0), firstSize, secondRoot, secondSize, proof));
        yield return ("wrong root2", () => tree.VerifyConsistency(firstRoot, firstSize, Flip(secondRoot, 0), secondSize, proof));
        yield return ("empty root1", () => tree.VerifyConsistency(new byte[firstRoot.Length], firstSize, secondRoot, secondSize, proof));
        yield return ("swapped roots", () => tree.VerifyConsistency(secondRoot, firstSize, firstRoot, secondSize, proof));

        // Inject extra steps, including each root, before and after the proof.
        yield return ("empty proof", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, []));
        yield return ("trailing garbage", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, Append(valid, new byte[firstRoot.Length])));
        yield return ("trailing root1", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, Append(valid, firstRoot)));
        yield return ("trailing root2", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, Append(valid, secondRoot)));
        yield return ("preceding garbage", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, Prepend(valid, new byte[firstRoot.Length])));
        yield return ("preceding root1", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, Prepend(valid, firstRoot)));
        yield return ("preceding root2", () => tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, Prepend(valid, secondRoot)));

        for (int step = 0; step < valid.Length; step++)
        {
            int captured = step;

            yield return ($"modified proof[{captured}] bit 4", () =>
                tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, Replace(valid, captured, Flip(valid[captured], 4))));

            yield return ($"removed proof[{captured}]", () =>
                tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, RemoveAt(valid, captured)));

            yield return ($"proof[{captured}] wrong width", () =>
                tree.VerifyConsistency(firstRoot, firstSize, secondRoot, secondSize, Replace(valid, captured, valid[captured][..16])));
        }
    }
}
