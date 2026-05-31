// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingCryptoTransform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Performs a diagnostic cryptographic transformation using the <see cref="SimpleReversingBlockCipher" />,
/// reversing the byte order of each block. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// This transform is intended exclusively for use in test harnesses and diagnostic scenarios. It provides
/// a deterministic, inspectable transform that makes it straightforward to verify block alignment, padding
/// behaviour, mode-of-operation chaining, and streaming correctness without cryptographic complexity.
/// </para>
/// <para>
/// Both <see cref="SimpleReversingSymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> and
/// <see cref="SimpleReversingSymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> return an instance
/// of this class. All calls to <see cref="TransformBlock" /> and <see cref="TransformFinalBlock" /> are
/// recorded in <see cref="Diagnostics" /> for post-hoc inspection.
/// </para>
/// <para>
/// When decrypting with a strippable padding mode (e.g., <see cref="PaddingMode.PKCS7" />), the last
/// complete input block is deferred until <see cref="TransformFinalBlock" /> is called to allow correct
/// padding removal.
/// </para>
/// <note type="warning">This transform provides no cryptographic security and must not be used in production code.</note>
/// </remarks>
public sealed class SimpleReversingCryptoTransform
    : ICryptoTransform, IAsyncDisposable
{
    private readonly SimpleReversingBlockCipher cipher;
    private readonly bool encrypt;
    private readonly IBlockCipherModeTransform mode;
    private readonly IPaddingStrategy padding;

    // Holds the last complete ciphertext block when decrypting with strippable padding.
    private byte[]? deferredInput;
    private bool disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="SimpleReversingCryptoTransform" /> class.
    /// </summary>
    /// <param name="cipher">The configured <see cref="SimpleReversingBlockCipher" /> engine.</param>
    /// <param name="cipherMode">The block cipher mode of operation.</param>
    /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
    /// <param name="iv">The initialisation vector. Required for all modes except ECB.</param>
    /// <param name="encrypt"><see langword="true" /> to configure for encryption; <see langword="false" /> for decryption.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    internal SimpleReversingCryptoTransform(
        SimpleReversingBlockCipher cipher,
        CipherModeKind cipherMode,
        PaddingMode paddingMode,
        byte[]? iv,
        bool encrypt)
    {
        this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        this.encrypt = encrypt;
        mode = BlockCipherModeFactory.Create(cipherMode, cipher, iv);
        padding = PaddingFactory.Create(paddingMode);
    }

    /// <summary>
    /// Gets the diagnostic recorder for this transform instance. Use this in test assertions to inspect
    /// the exact inputs and outputs seen at every layer of the transform pipeline.
    /// </summary>
    public SimpleReversingDiagnostics Diagnostics => cipher.Diagnostics;

    /// <inheritdoc />
    /// <remarks>
    /// Returns <see langword="false" />. Each <see cref="SimpleReversingCryptoTransform" /> instance is
    /// intended for a single-use transform lifetime. Create a new instance for each independent operation.
    /// </remarks>
    public bool CanReuseTransform => false;

    /// <inheritdoc />
    public bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <value>The configured block size in bytes.</value>
    public int InputBlockSize => cipher.BlockSize / 8;

    /// <inheritdoc />
    /// <value>The configured block size in bytes.</value>
    public int OutputBlockSize => cipher.BlockSize / 8;

    /// <summary>
    /// Transforms a block-aligned region of the input byte array and writes the result to the output buffer.
    /// </summary>
    /// <param name="inputBuffer">The input data buffer.</param>
    /// <param name="inputOffset">The byte offset within <paramref name="inputBuffer" /> at which to begin reading.</param>
    /// <param name="inputCount">The number of bytes to process. Must be a multiple of <see cref="InputBlockSize" />.</param>
    /// <param name="outputBuffer">The buffer to write transformed data into.</param>
    /// <param name="outputOffset">The byte offset within <paramref name="outputBuffer" /> at which to begin writing.</param>
    /// <returns>The number of bytes written to <paramref name="outputBuffer" />.</returns>
    /// <remarks>
    /// When decrypting with a strippable padding mode, the last complete input block is deferred until
    /// <see cref="TransformFinalBlock" /> is called to allow correct padding removal.
    /// </remarks>
    public int TransformBlock(
        byte[] inputBuffer,
        int inputOffset,
        int inputCount,
        byte[] outputBuffer,
        int outputOffset)
    {
        ThrowIfDisposed();

        ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
        Span<byte> output = outputBuffer.AsSpan(outputOffset, inputCount);

        int bytesWritten;

        if (encrypt)
        {
            bytesWritten = mode.Transform(input, output, true);
        }
        else
        {
            var stripPadding = padding is Pkcs7Padding;
            var blockBytes = cipher.BlockSize / 8;

            if (stripPadding && input.Length <= blockBytes)
            {
                deferredInput = input.ToArray();
                bytesWritten = 0;
            }
            else
            {
                var bytesToProcess = input.Length;
                if (stripPadding)
                {
                    bytesToProcess -= blockBytes;
                    deferredInput = input.Slice(bytesToProcess).ToArray();
                }

                bytesWritten = mode.Transform(input.Slice(0, bytesToProcess), output.Slice(0, bytesToProcess), false);
            }
        }

        Diagnostics.RecordTransformBlock(input, outputBuffer.AsSpan(outputOffset, bytesWritten));
        return bytesWritten;
    }

    /// <summary>
    /// Transforms the final block of data, applying or removing padding as appropriate, and returns the result.
    /// </summary>
    /// <param name="inputBuffer">The final input data buffer.</param>
    /// <param name="inputOffset">The byte offset within <paramref name="inputBuffer" /> at which to begin reading.</param>
    /// <param name="inputCount">The number of bytes to process from <paramref name="inputBuffer" />.</param>
    /// <returns>A new byte array containing the transformed and padded (or de-padded) final block.</returns>
    /// <exception cref="CryptographicException">The padding is invalid or cannot be removed during decryption.</exception>
    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        ThrowIfDisposed();

        ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
        byte[] result;

        if (encrypt)
        {
            var padded = padding.Pad(input, cipher.BlockSize);

            // CryptoStream calls TransformFinalBlock with zero bytes after consuming all complete
            // blocks via TransformBlock when input is block-aligned. Nothing left to encrypt.
            if (padded.Length == 0)
                return Array.Empty<byte>();

            var output = new byte[padded.Length];
            mode.Transform(padded, output, true);
            result = output;
        }
        else
        {
            var combined = Combine(deferredInput, input);

            if (combined.Length == 0)
            {
                // No deferred block and no new input.
                // For padding modes that require a full block (PKCS7, ANSIX923, ISO10126),
                // this means no padding block was ever received — throw exactly as the BCL does.
                if (padding is not NoPadding and not ZeroPadding)
                    throw new CryptographicException("Padding is invalid and cannot be removed.");

                return Array.Empty<byte>();
            }

            var decrypted = new byte[combined.Length];
            mode.Transform(combined, decrypted, false);
            result = padding.Unpad(decrypted, cipher.BlockSize);
        }

        Diagnostics.RecordTransformFinalBlock(input, result);
        return result;
    }

    /// <summary>
    /// Releases all resources used by this instance, including the underlying cipher engine.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;

        if (deferredInput is not null)
        {
            CryptographicOperations.ZeroMemory(deferredInput);
            deferredInput = null;
        }

        cipher.Dispose();
        disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously releases all resources used by this instance.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private static byte[] Combine(byte[]? first, ReadOnlySpan<byte> second)
    {
        if (first == null || first.Length == 0)
            return second.ToArray();

        var result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        second.CopyTo(result.AsSpan(first.Length));
        return result;
    }

    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (this.disposed) throw new ObjectDisposedException(nameof(SimpleReversingCryptoTransform));
#endif
    }
}
