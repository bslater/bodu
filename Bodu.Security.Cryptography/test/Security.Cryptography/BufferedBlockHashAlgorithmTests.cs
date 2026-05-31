// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferedBlockHashAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies the protocol contract exposed by <see cref="BufferedBlockHashAlgorithm{T}" />: constructor argument
/// validation, residual-buffer / counter reset on <c>Initialize</c>, dispose-once semantics, secret-zeroing on
/// disposal, and forwarding of the byte-array <c>HashCore</c> overload to the span overload.
/// </summary>
[TestClass]
public sealed class BufferedBlockHashAlgorithmTests
{
    /// <summary>
    /// Verifies that supplying a block size of zero to the constructor throws an
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlockSizeIsZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MonitoringBufferedBlockHashAlgorithm(0);
        });
    }

    /// <summary>
    /// Verifies that supplying a negative block size to the constructor throws an
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlockSizeIsNegative_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MonitoringBufferedBlockHashAlgorithm(-1);
        });
    }

    /// <summary>
    /// Verifies that a valid block size is stored verbatim and a residual buffer of the same length is allocated.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBlockSizeIsValid_ShouldStoreBlockSizeAndAllocateResidualBuffer()
    {
        using var sut = new MonitoringBufferedBlockHashAlgorithm(16);

        Assert.AreEqual(16, sut.ConfiguredBlockSize);
        Assert.AreEqual(16, sut.ResidualBufferSnapshot.Length);
        Assert.AreEqual(0, sut.ResidualBytesSnapshot);
        Assert.AreEqual(0UL, sut.TotalBytesSnapshot);
    }

    /// <summary>
    /// Verifies that calling <see cref="BufferedBlockHashAlgorithm{T}.Initialize" /> on a derived class invokes the
    /// derived override.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalled_ShouldRunDerivedOverride()
    {
        using var sut = new MonitoringBufferedBlockHashAlgorithm();
        var baseline = sut.InitializeCallCount;

        sut.Initialize();

        Assert.AreEqual(baseline + 1, sut.InitializeCallCount);
    }

    /// <summary>
    /// Verifies that <see cref="BufferedBlockHashAlgorithm{T}.Initialize" /> clears the residual buffer and resets
    /// both the residual byte count and the running byte total to zero, regardless of any prior seeded state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterSeededState_ShouldClearResidualAndCounters()
    {
        using var sut = new MonitoringBufferedBlockHashAlgorithm(8);
        sut.SeedResidualState([1, 2, 3, 4, 5, 6, 7, 8], residualBytes: 5, totalBytes: 256UL);

        sut.Initialize();

        Assert.AreEqual(0, sut.ResidualBytesSnapshot);
        Assert.AreEqual(0UL, sut.TotalBytesSnapshot);
        CollectionAssert.AreEqual(new byte[8], sut.ResidualBufferSnapshot);
    }

    /// <summary>
    /// Verifies that repeated calls to <see cref="BufferedBlockHashAlgorithm{T}.Initialize" /> run the derived
    /// override on every call, ensuring derived state is rebuilt each time.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledMultipleTimes_ShouldRunDerivedOverrideEachTime()
    {
        using var sut = new MonitoringBufferedBlockHashAlgorithm();
        var baseline = sut.InitializeCallCount;

        sut.Initialize();
        sut.Initialize();
        sut.Initialize();

        Assert.AreEqual(baseline + 3, sut.InitializeCallCount);
    }

    /// <summary>
    /// Verifies that <see cref="BufferedBlockHashAlgorithm{T}.Dispose(bool)" /> invokes
    /// <see cref="BufferedBlockHashAlgorithm{T}.OnDispose(bool)" /> exactly once with <c>disposing == true</c>.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledOnce_ShouldInvokeOnDisposeWithDisposingTrue()
    {
        var sut = new MonitoringBufferedBlockHashAlgorithm();

        sut.Dispose();

        Assert.AreEqual(1, sut.OnDisposeCallCount);
        Assert.IsTrue(sut.LastDisposeFlag == true);
    }

    /// <summary>
    /// Verifies that calling <see cref="BufferedBlockHashAlgorithm{T}.Dispose(bool)" /> twice invokes
    /// <see cref="BufferedBlockHashAlgorithm{T}.OnDispose(bool)" /> only once, in line with the standard
    /// <see cref="IDisposable" /> idempotency contract.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldInvokeOnDisposeOnce()
    {
        var sut = new MonitoringBufferedBlockHashAlgorithm();

        sut.Dispose();
        sut.Dispose();

        Assert.AreEqual(1, sut.OnDisposeCallCount);
    }

    /// <summary>
    /// Verifies that disposal clears the residual buffer and resets both counters, ensuring that no secret material
    /// survives in the buffer after the algorithm has been released.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldClearResidualAndCounters()
    {
        var sut = new MonitoringBufferedBlockHashAlgorithm(8);
        sut.SeedResidualState([9, 9, 9, 9, 9, 9, 9, 9], residualBytes: 8, totalBytes: 128UL);

        sut.Dispose();

        Assert.AreEqual(0, sut.ResidualBytesSnapshot);
        Assert.AreEqual(0UL, sut.TotalBytesSnapshot);
        CollectionAssert.AreEqual(new byte[8], sut.ResidualBufferSnapshot);
    }

    /// <summary>
    /// Verifies that <c>HashCore(byte[], int, int)</c> throws <see cref="ArgumentNullException" /> when the input
    /// array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void HashCore_WhenArrayIsNull_ShouldThrowExactly()
    {
        using var sut = new MonitoringBufferedBlockHashAlgorithm();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.InvokeHashCore(null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that <c>HashCore(byte[], int, int)</c> forwards to the span overload with the requested slice
    /// length, so derived classes that override only the span overload still see input from the byte-array overload.
    /// </summary>
    [TestMethod]
    public void HashCore_WhenCalledWithSlice_ShouldForwardSliceLengthToSpanOverload()
    {
        using var sut = new MonitoringBufferedBlockHashAlgorithm();
        var input = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        sut.InvokeHashCore(input, ibStart: 2, cbSize: 5);
        sut.InvokeHashCore(input, ibStart: 0, cbSize: 0);
        sut.InvokeHashCore(input, ibStart: 0, cbSize: input.Length);

        CollectionAssert.AreEqual(new[] { 5, 0, input.Length }, sut.CapturedSpanLengths);
    }

    /// <summary>
    /// Verifies that <c>HashCore(byte[], int, int)</c> throws <see cref="ObjectDisposedException" /> after the
    /// algorithm has been disposed.
    /// </summary>
    [TestMethod]
    public void HashCore_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var sut = new MonitoringBufferedBlockHashAlgorithm();
        sut.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.InvokeHashCore(new byte[1], 0, 1);
        });
    }
}
