// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Contracts;

/// <summary>
/// Reusable behavioural contract test base for an additive (symmetric, shared-key) stream cipher derived from
/// <see cref="SymmetricStreamAlgorithm" />. Concrete cipher test classes inherit this base to gain common coverage —
/// self-inverse round-trips, one-shot versus segmented equivalence, zero-plaintext-equals-keystream, transform
/// lifecycle, buffer-overlap handling, key / nonce generation, and key / nonce argument validation — mirroring the way
/// block-cipher tests inherit from <see cref="BlockCipherContractTests{TCipher}" />.
/// </summary>
/// <typeparam name="TCipher">The stream-cipher type under test.</typeparam>
/// <remarks>
/// The base is generic over the cipher type and is not itself a <see cref="TestClassAttribute" />; a concrete subclass
/// supplies the <see cref="TestClassAttribute" /> and the expected key / nonce sizes, after which MSTest discovers the
/// inherited <see cref="TestMethodAttribute" /> members against the subclass's cipher.
/// </remarks>
public abstract partial class SymmetricStreamAlgorithmContractTests<TCipher>
    where TCipher : SymmetricStreamAlgorithm, new()
{
    /// <summary>
    /// Gets the key size, in bits, the default-constructed cipher is expected to expose.
    /// </summary>
    /// <returns>The expected key size, in bits.</returns>
    protected abstract int ExpectedKeySizeBits { get; }

    /// <summary>
    /// Gets the nonce size, in bits, the default-constructed cipher is expected to expose.
    /// </summary>
    /// <returns>The expected nonce size, in bits.</returns>
    protected abstract int ExpectedNonceSizeBits { get; }

    /// <summary>
    /// Creates a new instance of the stream cipher under test.
    /// </summary>
    /// <returns>A new <typeparamref name="TCipher" /> instance.</returns>
    protected virtual TCipher CreateAlgorithm() => new();

    /// <summary>
    /// Gets the required key length, in bytes, for the cipher under test.
    /// </summary>
    /// <returns>The key length in bytes.</returns>
    protected int KeyLengthBytes
    {
        get
        {
            using TCipher alg = CreateAlgorithm();
            return alg.KeySize / 8;
        }
    }

    /// <summary>
    /// Gets the required nonce length, in bytes, for the cipher under test.
    /// </summary>
    /// <returns>The nonce length in bytes.</returns>
    protected int NonceLengthBytes
    {
        get
        {
            using TCipher alg = CreateAlgorithm();
            return alg.NonceSize / 8;
        }
    }

    /// <summary>
    /// Creates a deterministic key of the algorithm's required length.
    /// </summary>
    /// <returns>A key byte array.</returns>
    private byte[] CreateKey() => FillSequential(new byte[KeyLengthBytes], 0x10);

    /// <summary>
    /// Creates a deterministic nonce of the algorithm's required length.
    /// </summary>
    /// <returns>A nonce byte array.</returns>
    private byte[] CreateNonce() => FillSequential(new byte[NonceLengthBytes], 0x40);

    /// <summary>
    /// Creates a deterministic, non-trivial payload of the requested length.
    /// </summary>
    /// <param name="length">The payload length, in bytes.</param>
    /// <returns>A payload byte array.</returns>
    private static byte[] CreatePayload(int length) => FillSequential(new byte[length], 1);

    /// <summary>
    /// Fills a buffer with a simple incrementing byte pattern.
    /// </summary>
    /// <param name="buffer">The buffer to fill.</param>
    /// <param name="seed">The starting byte value.</param>
    /// <returns>The same <paramref name="buffer" />, filled.</returns>
    private static byte[] FillSequential(byte[] buffer, int seed)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(seed + i);

        return buffer;
    }
}
