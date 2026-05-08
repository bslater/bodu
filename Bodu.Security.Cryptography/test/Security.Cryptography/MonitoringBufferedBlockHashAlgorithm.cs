// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringBufferedBlockHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Test-oriented derivation of <see cref="BufferedBlockHashAlgorithm{T}" /> that records calls into selected base-class
/// extension points and exposes the protected residual-buffer state for assertion. Used to verify the buffered-block
/// contract in isolation without depending on any concrete cryptographic algorithm.
/// </summary>
public sealed class MonitoringBufferedBlockHashAlgorithm
    : BufferedBlockHashAlgorithm<MonitoringBufferedBlockHashAlgorithm>
{
    private bool _disposeObserved;

    /// <summary>
    /// Lengths of every span passed to <see cref="HashCore(ReadOnlySpan{byte})" />, in invocation order. Lets tests
    /// verify that the byte-array overload forwarded to the span overload with the expected slice length.
    /// </summary>
    public List<int> CapturedSpanLengths { get; } = new();

    /// <summary>
    /// Number of times <see cref="Initialize" /> has been invoked across the lifetime of the instance.
    /// </summary>
    public int InitializeCallCount { get; private set; }

    /// <summary>
    /// Number of times this test instance has observed the first call to <see cref="Dispose(bool)" />.
    /// </summary>
    /// <remarks>
    /// The value is latched so it preserves the old template-hook behaviour where disposal observation occurred at most
    /// once per instance.
    /// </remarks>
    public int OnDisposeCallCount { get; private set; }

    /// <summary>
    /// Captures the <c>disposing</c> argument from the first observed invocation of <see cref="Dispose(bool)" />, or
    /// <see langword="null" /> if disposal has not yet been observed.
    /// </summary>
    public bool? LastDisposeFlag { get; private set; }

    /// <inheritdoc />
    public override string AlgorithmName => nameof(MonitoringBufferedBlockHashAlgorithm);

    /// <summary>
    /// Initialises a new instance with the default 8-byte block size.
    /// </summary>
    public MonitoringBufferedBlockHashAlgorithm()
        : this(8)
    {
    }

    /// <summary>
    /// Initialises a new instance with the specified block size.
    /// </summary>
    /// <param name="blockSize">The block size, in bytes, to forward to the base constructor.</param>
    public MonitoringBufferedBlockHashAlgorithm(int blockSize)
        : base(blockSize)
    {
    }

    /// <summary>
    /// Gets a snapshot of the current residual byte count owned by the base class.
    /// </summary>
    /// <returns>The number of bytes currently held in the residual buffer.</returns>
    public int ResidualBytesSnapshot => this._residualBytes;

    /// <summary>
    /// Gets a snapshot of the running byte total owned by the base class.
    /// </summary>
    /// <returns>The total number of bytes consumed so far, as tracked by the base class.</returns>
    public ulong TotalBytesSnapshot => this._totalBytes;

    /// <summary>
    /// Gets the block size that was supplied to the base constructor.
    /// </summary>
    /// <returns>The configured block size, in bytes.</returns>
    public int ConfiguredBlockSize => this.BlockSizeBytes;

    /// <summary>
    /// Gets a defensive copy of the residual buffer for test assertion purposes.
    /// </summary>
    /// <returns>A new byte array of length <see cref="ConfiguredBlockSize" /> containing the current residual bytes.</returns>
    public byte[] ResidualBufferSnapshot => this._residualBlock.ToArray();

    /// <summary>
    /// Public test seam exposing the protected
    /// <see cref="System.Security.Cryptography.HashAlgorithm.HashCore(byte[], int, int)" /> overload so tests may
    /// drive it directly without going through <c>ComputeHash</c> (which performs its own validation before reaching
    /// the override).
    /// </summary>
    /// <param name="array">The input byte array.</param>
    /// <param name="ibStart">The start index in <paramref name="array" />.</param>
    /// <param name="cbSize">The number of bytes to read from <paramref name="array" />.</param>
    public void InvokeHashCore(byte[] array, int ibStart, int cbSize) =>
        this.HashCore(array, ibStart, cbSize);

    /// <summary>
    /// Seeds the residual buffer to a known state so tests may verify that <see cref="BufferedBlockHashAlgorithm{T}.Initialize" />
    /// and <see cref="BufferedBlockHashAlgorithm{T}.Dispose(bool)" /> clear it.
    /// </summary>
    /// <param name="data">The bytes to copy into the residual buffer. Must not exceed the configured block size.</param>
    /// <param name="residualBytes">The residual byte count to record.</param>
    /// <param name="totalBytes">The total byte count to record.</param>
    public void SeedResidualState(byte[] data, int residualBytes, ulong totalBytes)
    {
        data.AsSpan().CopyTo(this._residualBlock.Span);
        this._residualBytes = residualBytes;
        this._totalBytes = totalBytes;
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        this.InitializeCallCount++;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!this._disposeObserved)
        {
            this.OnDisposeCallCount++;
            this.LastDisposeFlag = disposing;
            this._disposeObserved = true;
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.CapturedSpanLengths.Add(source.Length);
    }

    /// <inheritdoc />
    protected override byte[] HashFinal() =>
        Array.Empty<byte>();
}