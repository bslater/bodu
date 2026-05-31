// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransform.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a base implementation of <see cref="ICryptoTransform" /> for block cipher algorithms that combine an
/// <see cref="IBlockCipher" /> engine with an <see cref="IBlockCipherModeTransform" /> and an
/// <see cref="IPaddingStrategy" />.
/// </summary>
/// <remarks>
/// <para>
/// Block-aligned streaming data is processed via <see cref="TransformBlock" />, and the final potentially partial block
/// — including padding application or removal — is handled by <see cref="TransformFinalBlock" />.
/// </para>
/// <para>
/// When decrypting with a strippable padding mode, such as <see cref="PaddingMode.PKCS7" />, the last complete block of
/// ciphertext is deferred until <see cref="TransformFinalBlock" /> is called. This allows padding to be validated and
/// removed only at the end of the stream.
/// </para>
/// <para>
/// A <see cref="BlockCipherTransform" /> instance represents a single cryptographic transform operation. Once
/// <see cref="TransformFinalBlock" /> has completed, the instance is finalized and cannot be used for another
/// operation. Consequently, <see cref="CanReuseTransform" /> returns <see langword="false" />. Callers that need to
/// encrypt or decrypt additional data must create a new transform instance.
/// </para>
/// <para>
/// Finalization is distinct from disposal. After <see cref="TransformFinalBlock" /> completes, subsequent transform
/// calls throw <see cref="InvalidOperationException" />. After <see cref="Dispose" /> is called, subsequent transform
/// calls throw <see cref="ObjectDisposedException" />.
/// </para>
/// <para>
/// The transform is intentionally not reusable because the underlying block cipher mode transform may contain mutable
/// chaining, feedback, or counter state. Clearing deferred padding input after finalization is not sufficient to
/// restore the mode transform to its initial IV or counter state.
/// </para>
/// <para>
/// Derived classes need only provide a constructor that calls
/// <see cref="BlockCipherTransform(IBlockCipher, CipherModeKind, PaddingMode, byte[], bool)" /> or
/// <see cref="BlockCipherTransform(IBlockCipher, CipherModeKind, PaddingModeKind, byte[], bool)" /> with the
/// appropriate arguments. All transform logic is handled by this base class.
/// </para>
/// <para>
/// <strong>How this fits with the rest of the library.</strong> <see cref="BlockCipherTransform" /> is the glue layer
/// that turns a low-level <see cref="IBlockCipher" /> into the
/// <see cref="System.Security.Cryptography.ICryptoTransform" /> contract that
/// <see cref="System.Security.Cryptography.CryptoStream" />, <c>SymmetricAlgorithm.CreateEncryptor()</c>, and the rest
/// of the BCL crypto pipeline expect. Every <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> in this
/// library returns an instance of a derived class from its <c>CreateEncryptor</c> / <c>CreateDecryptor</c> overrides —
/// for example, <c>BlowfishTransform</c>, <c>CamelliaTransform</c>, <c>SkipjackTransform</c>, <c>TwofishTransform</c>,
/// <c>SerpentTransform</c>, <c>ThreefishTransform</c>.
/// </para>
/// <para>
/// Derive from this class only when adding a new symmetric algorithm to the library. Most callers never touch this type
/// directly — they use <see cref="System.Security.Cryptography.SymmetricAlgorithm.Mode" /> and
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm.Padding" /> to configure encryption and let the existing
/// transform infrastructure handle the wiring.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Most consumers never touch BlockCipherTransform directly — it is surfaced as the
/// ICryptoTransform returned by every Bodu SymmetricAlgorithm. Typical usage:
/// using SymmetricAlgorithm alg = new Twofish();
/// alg.GenerateKey();
/// alg.GenerateIV();
/// alg.Mode    = CipherMode.CBC;
/// alg.Padding = PaddingMode.PKCS7;
///
/// CreateEncryptor returns a BlockCipherTransform under the hood — a one-shot
/// ICryptoTransform that pairs the block cipher with the configured mode and padding.
/// using ICryptoTransform encryptor = alg.CreateEncryptor();
/// using var output = new MemoryStream();
/// using (var cs = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
///     cs.Write(plaintext, 0, plaintext.Length);
/// byte[] ciphertext = output.ToArray();
///
/// Reuse requires a fresh transform — BlockCipherTransform.CanReuseTransform is false.
///]]>
/// </example>
/// <seealso cref="IBlockCipher"/> <seealso cref="IBlockCipherModeTransform"/> <seealso cref="IPaddingStrategy"/>
public abstract class BlockCipherTransform
    : ICryptoTransform
{
    /// <summary>
    /// The configured block cipher engine used by the mode transform.
    /// </summary>
    private readonly IBlockCipher _cipher;

    /// <summary>
    /// Indicates whether this transform encrypts input data; otherwise, it decrypts input data.
    /// </summary>
    private readonly bool _encrypt;

    /// <summary>
    /// The block cipher mode transform that applies chaining, feedback, counter, or equivalent mode semantics.
    /// </summary>
    /// <remarks>
    /// This object may contain mutable per-operation state and is therefore not reset after finalization.
    /// </remarks>
    private readonly IBlockCipherModeTransform _mode;

    /// <summary>
    /// The padding strategy used to pad plaintext during encryption or remove padding during decryption.
    /// </summary>
    private readonly IPaddingStrategy _padding;

    /// <summary>
    /// Holds the final deferred ciphertext block when decrypting with a padding mode that must inspect the stream
    /// boundary before removing padding.
    /// </summary>
    /// <remarks>
    /// The contents are zeroed before being replaced, cleared, or disposed.
    /// </remarks>
    private byte[]? _deferredInput;

    /// <summary>
    /// Indicates whether this transform has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Indicates whether <see cref="TransformFinalBlock(byte[], int, int)" /> has completed and this one-shot transform
    /// can no longer process additional input.
    /// </summary>
    private bool _finalized;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockCipherTransform" /> class using the specified cipher engine,
    /// mode, padding scheme, initialization vector, and transform direction.
    /// </summary>
    /// <param name="cipher">
    /// The configured <see cref="IBlockCipher" /> engine to use. Must not be <see langword="null" />.
    /// </param>
    /// <param name="cipherMode">The block cipher mode of operation.</param>
    /// <param name="paddingMode">The padding scheme to apply to the final block.</param>
    /// <param name="iv">The initialization vector for the cipher mode. Must match the cipher block size.</param>
    /// <param name="encrypt">
    /// <see langword="true" /> to configure for encryption; <see langword="false" /> for decryption.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    protected BlockCipherTransform(IBlockCipher cipher, CipherModeKind cipherMode, PaddingMode paddingMode, byte[]? iv, bool encrypt)
    {
        _cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        _encrypt = encrypt;
        _mode = BlockCipherModeFactory.Create(cipherMode, cipher, iv);
        _padding = PaddingFactory.Create(paddingMode);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockCipherTransform" /> class using the specified cipher engine,
    /// mode, extended padding scheme, initialization vector, and transform direction.
    /// </summary>
    /// <param name="cipher">
    /// The configured <see cref="IBlockCipher" /> engine to use. Must not be <see langword="null" />.
    /// </param>
    /// <param name="cipherMode">The block cipher mode of operation.</param>
    /// <param name="paddingMode">
    /// The extended padding scheme to apply to the final block. Accepts values beyond the framework
    /// <see cref="PaddingMode" /> enum, including <see cref="PaddingModeKind.ISO7816_4" />.
    /// </param>
    /// <param name="iv">The initialization vector for the cipher mode. Must match the cipher block size.</param>
    /// <param name="encrypt">
    /// <see langword="true" /> to configure for encryption; <see langword="false" /> for decryption.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="cipher" /> is <see langword="null" />.</exception>
    protected BlockCipherTransform(IBlockCipher cipher, CipherModeKind cipherMode, PaddingModeKind paddingMode, byte[]? iv, bool encrypt)
    {
        _cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        _encrypt = encrypt;
        _mode = BlockCipherModeFactory.Create(cipherMode, cipher, iv);
        _padding = PaddingFactory.Create(paddingMode);
    }

    /// <inheritdoc />
    public bool CanReuseTransform => false;

    /// <inheritdoc />
    public bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// The BCL <see cref="ICryptoTransform" /> contract requires this value in <em>bytes</em>; the underlying
    /// <see cref="IBlockCipher.BlockSize" /> is in <em>bits</em>, so we divide by 8.
    /// </remarks>
    public int InputBlockSize => _cipher.BlockSize / 8;

    /// <inheritdoc />
    /// <remarks>
    /// The BCL <see cref="ICryptoTransform" /> contract requires this value in <em>bytes</em>; the underlying
    /// <see cref="IBlockCipher.BlockSize" /> is in <em>bits</em>, so we divide by 8.
    /// </remarks>
    public int OutputBlockSize => _cipher.BlockSize / 8;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        ClearDeferredInput();

        // Cascade disposal to the mode transform so it can zero its chaining vector, counter,
        // tweak, or feedback register before the underlying cipher is released.
        _mode.Dispose();
        _cipher.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Transforms a block-aligned region of the input byte array and writes the result to the output buffer.
    /// </summary>
    /// <param name="inputBuffer">The input data buffer. Must not be <see langword="null" />.</param>
    /// <param name="inputOffset">
    /// The byte offset within <paramref name="inputBuffer" /> at which to begin reading.
    /// </param>
    /// <param name="inputCount">
    /// The number of bytes to process. Must be a multiple of <see cref="InputBlockSize" />.
    /// </param>
    /// <param name="outputBuffer">
    /// The buffer to write the transformed data into. Must not be <see langword="null" />.
    /// </param>
    /// <param name="outputOffset">
    /// The byte offset within <paramref name="outputBuffer" /> at which to begin writing.
    /// </param>
    /// <returns>The number of bytes written to <paramref name="outputBuffer" />.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// This transform has already been finalized and cannot be reused.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="inputBuffer" /> or <paramref name="outputBuffer" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The input or output buffer span is invalid or insufficient in length for the requested operation.
    /// </exception>
    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
        ThrowIfDisposed();
        ThrowIfFinalized();

        ThrowHelper.ThrowIfNull(inputBuffer);
        ThrowHelper.ThrowIfNull(outputBuffer);
        CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(inputBuffer, inputOffset, inputCount);
        CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(outputBuffer, outputOffset, inputCount);

        ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);

        // Allow zero-length input: per the ICryptoTransform contract a zero-byte TransformBlock
        // call must return 0 rather than throw. CryptoStream and similar callers may invoke this
        // path with no buffered data after a flush. Divide BlockSize (bits) by 8 to obtain the
        // byte-length divisor expected by the span helper.
        CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf<byte>(input, _cipher.BlockSize / 8, throwIfZero: false);
        Span<byte> output = outputBuffer.AsSpan(outputOffset, inputCount);
        CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf<byte>(output, _cipher.BlockSize / 8, throwIfZero: false);

        if (_encrypt)
            return _mode.Transform(input, output, true);

        var stripPadding = _padding.StripsPaddingOnUnpad;

        if (!stripPadding)
            return _mode.Transform(input, output, false);

        var combined = Combine(_deferredInput, input);
        ClearDeferredInput();

        if (combined.Length <= _cipher.BlockSize / 8)
        {
            _deferredInput = combined;
            return 0;
        }

        var bytesToProcess = combined.Length - (_cipher.BlockSize / 8);

        try
        {
            var bytesWritten = _mode.Transform(
                combined.AsSpan(0, bytesToProcess),
                output[..bytesToProcess],
                false);

            _deferredInput = combined.AsSpan(bytesToProcess).ToArray();
            return bytesWritten;
        }
        finally
        {
            CryptoHelpers.Clear(combined);
        }
    }

    /// <summary>
    /// Transforms the final block of data, applying or removing padding as appropriate, and returns the result.
    /// </summary>
    /// <param name="inputBuffer">The final input data buffer. Must not be <see langword="null" />.</param>
    /// <param name="inputOffset">
    /// The byte offset within <paramref name="inputBuffer" /> at which to begin reading.
    /// </param>
    /// <param name="inputCount">The number of bytes to process from <paramref name="inputBuffer" />.</param>
    /// <returns>A new byte array containing the transformed and padded, or depadded, final block.</returns>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// This transform has already been finalized and cannot be reused.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="inputBuffer" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The input buffer span is invalid for the requested operation.</exception>
    /// <exception cref="CryptographicException">
    /// The padding is invalid or cannot be removed during decryption.
    /// </exception>
    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
        ThrowIfDisposed();
        ThrowIfFinalized();

        ThrowHelper.ThrowIfNull(inputBuffer);
        CryptoHelpers.ThrowIfArrayOffsetOrCountInvalid(inputBuffer, inputOffset, inputCount);

        ReadOnlySpan<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);

        try
        {
            if (_encrypt)
            {
                // Encrypt path: the padding scheme accepts any input length and emits an
                // aligned buffer, so the alignment check belongs after Pad — not on the raw
                // input. CryptoStream.FlushFinalBlock routinely arrives here with whatever
                // residual sits in its buffer (including the 1..blockSize-1 bytes left over
                // after the last aligned chunk has been forwarded to TransformBlock), and
                // PKCS7 / ANSIX923 / ISO10126 / ISO7816-4 always emit a padding block for
                // empty plaintext too.
                var padded = _padding.Pad(input, _cipher.BlockSize);
                var output = new byte[padded.Length];

                try
                {
                    _mode.Transform(padded, output, true);
                    return output;
                }
                finally
                {
                    CryptoHelpers.Clear(padded);
                }
            }
            else
            {
                // Decrypt path: ciphertext must be block-aligned once any deferred buffer is
                // re-attached. Validate the combined length so a malformed final call surfaces
                // as a CryptographicException with a recognizable message rather than crashing
                // deeper in the mode transform.
                var combined = Combine(_deferredInput, input);
                CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf<byte>(
                    combined,
                    _cipher.BlockSize / 8,
                    throwIfZero: false);

                var decrypted = new byte[combined.Length];

                try
                {
                    _mode.Transform(combined, decrypted, false);
                    return _padding.Unpad(decrypted, _cipher.BlockSize);
                }
                finally
                {
                    CryptoHelpers.Clear(combined);
                    CryptoHelpers.Clear(decrypted);
                }
            }
        }
        finally
        {
            ClearDeferredInput();
            _finalized = true;
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
#endif

    /// <summary>
    /// Throws if this transform has already completed its final block operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This transform has already been finalized and cannot be reused.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfFinalized()
    {
        if (_finalized)
            throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_TransformAlreadyFinalized);
    }

    /// <summary>
    /// Concatenates an optional deferred byte array with an incoming input span into a single contiguous byte array.
    /// </summary>
    /// <param name="first">
    /// The previously cached partial or complete block, or <see langword="null" /> if none was deferred.
    /// </param>
    /// <param name="second">The newly arriving data to append.</param>
    /// <returns>
    /// A new byte array containing <paramref name="first" /> followed by <paramref name="second" />, or a copy of
    /// <paramref name="second" /> alone if <paramref name="first" /> is <see langword="null" /> or empty.
    /// </returns>
    private static byte[] Combine(byte[]? first, ReadOnlySpan<byte> second)
    {
        if (first == null || first.Length == 0)
            return second.ToArray();

        var result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        second.CopyTo(result.AsSpan(first.Length));
        return result;
    }

    /// <summary>
    /// Zeroes and clears any deferred ciphertext block retained for padded decryption.
    /// </summary>
    private void ClearDeferredInput() =>
        CryptoHelpers.ClearAndNullify(ref _deferredInput);
}
