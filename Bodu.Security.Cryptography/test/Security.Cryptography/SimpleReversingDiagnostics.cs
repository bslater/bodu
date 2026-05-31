// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingDiagnostics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Records all encrypt and decrypt calls made through a <see cref="SimpleReversingBlockCipher" /> or
/// <see cref="SimpleReversingCryptoTransform" /> for post-hoc inspection in test harnesses.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="RecordEncrypt" /> or <see cref="RecordDecrypt" /> appends a <see cref="BlockOperation" />
/// entry to the appropriate log. Tests can then assert on the number of blocks processed, the exact byte content of
/// each input and output block, and the sequencing of encrypt vs. decrypt calls.
/// </para>
/// <para>
/// This class is not thread-safe. Use a separate instance per test case.
/// </para>
/// </remarks>
public sealed class SimpleReversingDiagnostics
{
    private readonly List<BlockOperation> encryptLog = new();
    private readonly List<BlockOperation> decryptLog = new();
    private readonly List<BlockOperation> transformBlockLog = new();
    private readonly List<BlockOperation> transformFinalBlockLog = new();

    /// <summary>
    /// Gets the sequence of block-level encrypt operations recorded by the cipher engine.
    /// </summary>
    public IReadOnlyList<BlockOperation> EncryptLog => encryptLog;

    /// <summary>
    /// Gets the sequence of block-level decrypt operations recorded by the cipher engine.
    /// </summary>
    public IReadOnlyList<BlockOperation> DecryptLog => decryptLog;

    /// <summary>
    /// Gets the sequence of <see cref="SimpleReversingCryptoTransform.TransformBlock" /> calls.
    /// </summary>
    public IReadOnlyList<BlockOperation> TransformBlockLog => transformBlockLog;

    /// <summary>
    /// Gets the sequence of <see cref="SimpleReversingCryptoTransform.TransformFinalBlock" /> calls.
    /// </summary>
    public IReadOnlyList<BlockOperation> TransformFinalBlockLog => transformFinalBlockLog;

    /// <summary>
    /// Gets the total number of bytes passed to <see cref="RecordEncrypt" />.
    /// </summary>
    public long TotalEncryptedBytes { get; private set; }

    /// <summary>
    /// Gets the total number of bytes passed to <see cref="RecordDecrypt" />.
    /// </summary>
    public long TotalDecryptedBytes { get; private set; }

    /// <summary>
    /// Gets the total number of bytes processed by <see cref="RecordTransformBlock" />.
    /// </summary>
    public long TotalTransformBlockBytes { get; private set; }

    /// <summary>
    /// Gets the total number of bytes processed by <see cref="RecordTransformFinalBlock" />.
    /// </summary>
    public long TotalTransformFinalBlockBytes { get; private set; }

    /// <summary>
    /// Records a single block-level encrypt operation.
    /// </summary>
    /// <param name="input">The plaintext bytes presented to the cipher.</param>
    /// <param name="output">The ciphertext bytes produced by the cipher.</param>
    internal void RecordEncrypt(ReadOnlySpan<byte> input, ReadOnlySpan<byte> output)
    {
        encryptLog.Add(new BlockOperation(input, output, encryptLog.Count));
        TotalEncryptedBytes += input.Length;
    }

    /// <summary>
    /// Records a single block-level decrypt operation.
    /// </summary>
    /// <param name="input">The ciphertext bytes presented to the cipher.</param>
    /// <param name="output">The plaintext bytes produced by the cipher.</param>
    internal void RecordDecrypt(ReadOnlySpan<byte> input, ReadOnlySpan<byte> output)
    {
        decryptLog.Add(new BlockOperation(input, output, decryptLog.Count));
        TotalDecryptedBytes += input.Length;
    }

    /// <summary>
    /// Records a call to <see cref="SimpleReversingCryptoTransform.TransformBlock" />.
    /// </summary>
    /// <param name="input">The raw input bytes supplied by the caller.</param>
    /// <param name="output">The bytes written to the output buffer.</param>
    internal void RecordTransformBlock(ReadOnlySpan<byte> input, ReadOnlySpan<byte> output)
    {
        transformBlockLog.Add(new BlockOperation(input, output, transformBlockLog.Count));
        TotalTransformBlockBytes += input.Length;
    }

    /// <summary>
    /// Records a call to <see cref="SimpleReversingCryptoTransform.TransformFinalBlock" />.
    /// </summary>
    /// <param name="input">The raw input bytes supplied by the caller.</param>
    /// <param name="output">The bytes returned to the caller.</param>
    internal void RecordTransformFinalBlock(ReadOnlySpan<byte> input, ReadOnlySpan<byte> output)
    {
        transformFinalBlockLog.Add(new BlockOperation(input, output, transformFinalBlockLog.Count));
        TotalTransformFinalBlockBytes += input.Length;
    }

    /// <summary>
    /// Clears all recorded operations and resets all byte counters.
    /// </summary>
    public void Reset()
    {
        encryptLog.Clear();
        decryptLog.Clear();
        transformBlockLog.Clear();
        transformFinalBlockLog.Clear();
        TotalEncryptedBytes = 0;
        TotalDecryptedBytes = 0;
        TotalTransformBlockBytes = 0;
        TotalTransformFinalBlockBytes = 0;
    }

    /// <summary>
    /// Represents a single block-level cipher or transform operation captured by
    /// <see cref="SimpleReversingDiagnostics" />.
    /// </summary>
    public sealed class BlockOperation
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="BlockOperation" /> class.
        /// </summary>
        /// <param name="input">The input bytes for this operation.</param>
        /// <param name="output">The output bytes produced by this operation.</param>
        /// <param name="sequenceNumber">The zero-based position of this operation within its log.</param>
        internal BlockOperation(ReadOnlySpan<byte> input, ReadOnlySpan<byte> output, int sequenceNumber)
        {
            Input = input.ToArray();
            Output = output.ToArray();
            SequenceNumber = sequenceNumber;
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets a snapshot of the input bytes presented to the operation.
        /// </summary>
        public byte[] Input { get; }

        /// <summary>
        /// Gets a snapshot of the output bytes produced by the operation.
        /// </summary>
        public byte[] Output { get; }

        /// <summary>
        /// Gets the zero-based ordinal of this operation within its log.
        /// </summary>
        public int SequenceNumber { get; }

        /// <summary>
        /// Gets the UTC timestamp at which this operation was recorded.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the number of input bytes in this operation.
        /// </summary>
        public int InputLength => Input.Length;

        /// <summary>
        /// Gets the number of output bytes in this operation.
        /// </summary>
        public int OutputLength => Output.Length;

        /// <inheritdoc />
        public override string ToString() =>
            $"[{SequenceNumber}] in={Convert.ToHexString(Input)} → out={Convert.ToHexString(Output)}";
    }
}
