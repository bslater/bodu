// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringDeferredFinalBlockHashAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Test-oriented derivation of <see cref="DeferredFinalBlockHashAlgorithm{T}" /> that captures a copy of every
/// <see cref="ProcessBlock(ReadOnlySpan{byte}, ulong, bool)" /> invocation along with its counter and finalisation
/// flag. Used to verify the deferred-final-block buffering contract in isolation.
/// </summary>
public sealed class MonitoringDeferredFinalBlockHashAlgorithm
    : DeferredFinalBlockHashAlgorithm<MonitoringDeferredFinalBlockHashAlgorithm>
{
    /// <summary>
    /// Recorded <see cref="ProcessBlock(ReadOnlySpan{byte}, ulong, bool)" /> invocations in invocation order. Each
    /// entry holds a defensive copy of the block bytes (the underlying span lives in the inherited residual buffer and
    /// is overwritten by subsequent input).
    /// </summary>
    public List<(byte[] Block, ulong Counter, bool IsFinal)> ProcessBlockInvocations { get; } = new();

    /// <summary>
    /// Number of times <see cref="Initialize" /> has been invoked across the lifetime of the instance.
    /// </summary>
    public int InitializeCallCount { get; private set; }

    /// <inheritdoc />
    public override string AlgorithmName => nameof(MonitoringDeferredFinalBlockHashAlgorithm);

    /// <summary>
    /// Initialises a new instance with the default 8-byte block size and an 8-bit digest.
    /// </summary>
    public MonitoringDeferredFinalBlockHashAlgorithm()
        : this(8)
    {
    }

    /// <summary>
    /// Initialises a new instance with the specified block size and an 8-bit digest size (so that the
    /// <see cref="System.Security.Cryptography.HashAlgorithm" /> infrastructure has a non-zero <c>HashSize</c> when
    /// <c>ComputeHash</c> is invoked).
    /// </summary>
    /// <param name="blockSize">
    /// The block size, in bytes, to forward to the base constructor (converted to bits).
    /// </param>
    public MonitoringDeferredFinalBlockHashAlgorithm(int blockSize)
        : base(blockSize * 8)
    {
        this.HashSizeValue = 8;
    }

    /// <summary>
    /// Clears the recorded invocation log so that subsequent calls capture only fresh activity. Does not affect
    /// algorithm state &#8212; use <see cref="System.Security.Cryptography.HashAlgorithm.Initialize" /> for that.
    /// </summary>
    public void ResetCapture() => this.ProcessBlockInvocations.Clear();

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();
        this.InitializeCallCount++;
    }

    /// <inheritdoc />
    protected override void ProcessBlock(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal) => this.ProcessBlockInvocations.Add((block.ToArray(), totalBytesIncludingThisBlock, isFinal));

    /// <inheritdoc />
    protected override byte[] ProcessFinalBlock() =>
        [(byte)this.ProcessBlockInvocations.Count];
}
