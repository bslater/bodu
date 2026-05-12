// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DeferredFinalBlockHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the deferred-final-block buffering contract exposed by
/// <see cref="DeferredFinalBlockHashAlgorithm{T}" />: the rule that the residual block is not compressed at the
/// point it fills (so that <c>isFinal: true</c> can be raised on it later), the per-block counter values, and the
/// behaviour of the finalisation pad-and-compress step across empty, sub-block, exact-block, and multi-block inputs.
/// </summary>
[TestClass]
public sealed class DeferredFinalBlockHashAlgorithmTests
{
    private const int BlockSize = 8;

    /// <summary>
    /// Verifies that hashing an empty input produces exactly one compression call with
    /// <c>isFinal == true</c> and a counter of zero (no bytes have been processed).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsEmpty_ShouldInvokeProcessBlockOnceWithFinalFlagAndZeroCounter()
    {
        using var sut = new MonitoringDeferredFinalBlockHashAlgorithm(BlockSize);

        _ = sut.ComputeHash(Array.Empty<byte>());

        Assert.AreEqual(1, sut.ProcessBlockInvocations.Count);
        Assert.IsTrue(sut.ProcessBlockInvocations[0].IsFinal);
        Assert.AreEqual(0UL, sut.ProcessBlockInvocations[0].Counter);
    }

    /// <summary>
    /// Verifies that an input of exactly one block is deferred until <c>HashFinal</c> and then compressed once with
    /// <c>isFinal == true</c> and a counter equal to <see cref="BlockSize" />.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsExactlyOneBlock_ShouldInvokeProcessBlockOnceWithFinalFlagAndBlockSizeCounter()
    {
        using var sut = new MonitoringDeferredFinalBlockHashAlgorithm(BlockSize);

        _ = sut.ComputeHash(SequentialBytes(BlockSize));

        Assert.AreEqual(1, sut.ProcessBlockInvocations.Count);
        Assert.IsTrue(sut.ProcessBlockInvocations[0].IsFinal);
        Assert.AreEqual((ulong)BlockSize, sut.ProcessBlockInvocations[0].Counter);
    }

    /// <summary>
    /// Verifies that <c>BlockSize + 1</c> bytes produce two compression calls: the held first block fires with
    /// <c>isFinal == false</c> and counter <see cref="BlockSize" />, and the trailing single byte fires from
    /// <c>HashFinal</c> with <c>isFinal == true</c> and counter <c>BlockSize + 1</c>.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsBlockSizePlusOne_ShouldInvokeProcessBlockTwiceWithCorrectCountersAndFlags()
    {
        using var sut = new MonitoringDeferredFinalBlockHashAlgorithm(BlockSize);

        _ = sut.ComputeHash(SequentialBytes(BlockSize + 1));

        Assert.AreEqual(2, sut.ProcessBlockInvocations.Count);

        Assert.IsFalse(sut.ProcessBlockInvocations[0].IsFinal);
        Assert.AreEqual((ulong)BlockSize, sut.ProcessBlockInvocations[0].Counter);

        Assert.IsTrue(sut.ProcessBlockInvocations[1].IsFinal);
        Assert.AreEqual((ulong)(BlockSize + 1), sut.ProcessBlockInvocations[1].Counter);
    }

    /// <summary>
    /// Verifies that exactly two blocks of input fire the first compression with <c>isFinal == false</c> and the
    /// second (deferred) compression with <c>isFinal == true</c> and a counter of <c>2 * BlockSize</c>.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsExactlyTwoBlocks_ShouldInvokeProcessBlockTwiceWithCorrectCountersAndFlags()
    {
        using var sut = new MonitoringDeferredFinalBlockHashAlgorithm(BlockSize);

        _ = sut.ComputeHash(SequentialBytes(2 * BlockSize));

        Assert.AreEqual(2, sut.ProcessBlockInvocations.Count);

        Assert.IsFalse(sut.ProcessBlockInvocations[0].IsFinal);
        Assert.AreEqual((ulong)BlockSize, sut.ProcessBlockInvocations[0].Counter);

        Assert.IsTrue(sut.ProcessBlockInvocations[1].IsFinal);
        Assert.AreEqual((ulong)(2 * BlockSize), sut.ProcessBlockInvocations[1].Counter);
    }

    /// <summary>
    /// Verifies that <c>2 * BlockSize + 1</c> bytes produce three compression calls with the expected counter
    /// progression: <c>(false, BlockSize)</c>, <c>(false, 2 * BlockSize)</c>, <c>(true, 2 * BlockSize + 1)</c>.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsTwoBlocksPlusOne_ShouldInvokeProcessBlockThreeTimesWithCorrectCountersAndFlags()
    {
        using var sut = new MonitoringDeferredFinalBlockHashAlgorithm(BlockSize);

        _ = sut.ComputeHash(SequentialBytes((2 * BlockSize) + 1));

        Assert.AreEqual(3, sut.ProcessBlockInvocations.Count);

        Assert.IsFalse(sut.ProcessBlockInvocations[0].IsFinal);
        Assert.AreEqual((ulong)BlockSize, sut.ProcessBlockInvocations[0].Counter);

        Assert.IsFalse(sut.ProcessBlockInvocations[1].IsFinal);
        Assert.AreEqual((ulong)(2 * BlockSize), sut.ProcessBlockInvocations[1].Counter);

        Assert.IsTrue(sut.ProcessBlockInvocations[2].IsFinal);
        Assert.AreEqual((ulong)((2 * BlockSize) + 1), sut.ProcessBlockInvocations[2].Counter);
    }

    /// <summary>
    /// Verifies that the residual buffer is zero-padded for inputs whose length is not a multiple of the block size,
    /// so the final compression observes a well-defined block (data bytes followed by zeros).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsSubBlock_ShouldZeroPadFinalBlockAndReportActualByteCounter()
    {
        using var sut = new MonitoringDeferredFinalBlockHashAlgorithm(BlockSize);
        var input = new byte[] { 0xAA, 0xBB, 0xCC };

        _ = sut.ComputeHash(input);

        Assert.AreEqual(1, sut.ProcessBlockInvocations.Count);

        (var block, var counter, var isFinal) = sut.ProcessBlockInvocations[0];
        Assert.IsTrue(isFinal);
        Assert.AreEqual(3UL, counter);

        var expected = new byte[BlockSize];
        input.CopyTo(expected, 0);
        CollectionAssert.AreEqual(expected, block);
    }

    /// <summary>
    /// Verifies that recomputing the same input after the algorithm has been re-initialised produces an identical
    /// invocation sequence (same block payloads, counters, and finalisation flags), confirming that
    /// <c>Initialize</c> resets the inherited residual buffer and counter via the grandparent.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInvokedTwiceWithIdenticalInput_ShouldProduceIdenticalInvocationSequence()
    {
        using var sut = new MonitoringDeferredFinalBlockHashAlgorithm(BlockSize);
        var input = SequentialBytes((2 * BlockSize) + 3);

        _ = sut.ComputeHash(input);
        (byte[] Block, ulong Counter, bool IsFinal)[] firstRun = sut.ProcessBlockInvocations
            .Select(invocation => (Block: (byte[])invocation.Block.Clone(), invocation.Counter, invocation.IsFinal))
            .ToArray();

        sut.ResetCapture();
        _ = sut.ComputeHash(input);
        (byte[] Block, ulong Counter, bool IsFinal)[] secondRun = sut.ProcessBlockInvocations.ToArray();

        Assert.AreEqual(firstRun.Length, secondRun.Length);
        for (var i = 0; i < firstRun.Length; i++)
        {
            Assert.AreEqual(firstRun[i].Counter, secondRun[i].Counter, $"Counter mismatch at invocation {i}.");
            Assert.AreEqual(firstRun[i].IsFinal, secondRun[i].IsFinal, $"IsFinal mismatch at invocation {i}.");
            CollectionAssert.AreEqual(firstRun[i].Block, secondRun[i].Block, $"Block payload mismatch at invocation {i}.");
        }
    }

    /// <summary>
    /// Verifies that calling <c>Initialize</c> on a derived <see cref="DeferredFinalBlockHashAlgorithm{T}" /> runs the
    /// derived override, ensuring algorithm-specific state is reset on every <c>ComputeHash</c> call.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalled_ShouldRunDerivedOverride()
    {
        using var sut = new MonitoringDeferredFinalBlockHashAlgorithm(BlockSize);
        var baseline = sut.InitializeCallCount;

        sut.Initialize();

        Assert.AreEqual(baseline + 1, sut.InitializeCallCount);
    }

    private static byte[] SequentialBytes(int length)
    {
        var result = new byte[length];
        for (var i = 0; i < length; i++)
            result[i] = unchecked((byte)i);
        return result;
    }
}
