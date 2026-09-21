// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockAccumulatorTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MerkleBlockAccumulatorTests
{
    /// <summary>
    /// Verifies that every member throws <see cref="ObjectDisposedException" /> after disposal.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldMakeEveryMemberThrowObjectDisposedException()
    {
        MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock, retainLeafHashes: true);
        accumulator.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => { accumulator.Append(new byte[] { 0x01 }); });
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = accumulator.Finish(); });
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = accumulator.FinishBound(); });
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = accumulator.FinishComputation(); });
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => { accumulator.Reset(); });
    }

    /// <summary>
    /// Verifies that disposing twice is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        MerkleBlockAccumulator accumulator = CreateTree().CreateBlockAccumulator(SmallBlock);
        accumulator.Append(SeededInput(3));

        accumulator.Dispose();
        accumulator.Dispose();
    }

    /// <summary>
    /// Verifies that a finished root obtained before disposal remains valid afterwards — disposal releases the
    /// algorithm, not results already returned.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledAfterAFinish_ShouldNotDisturbTheReturnedRoot()
    {
        Rfc6962MerkleTree tree = CreateTree();
        byte[] data = SeededInput(SmallBlock + 4);
        MerkleBlockAccumulator accumulator = tree.CreateBlockAccumulator(SmallBlock);
        accumulator.Append(data);
        byte[] root = accumulator.Finish();

        accumulator.Dispose();

        Assert.AreEqual(Hex(ExpectedRoot(tree, data, SmallBlock)), Hex(root));
    }
}
