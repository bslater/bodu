// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithmTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Security.Cryptography;

/// <summary>
/// Provides the shared behavioural contract for additive stream ciphers derived from
/// <see cref="StreamCipherAlgorithm" />. Concrete cipher test classes inherit this base to gain common coverage —
/// self-inverse round-trips, one-shot versus segmented equivalence, zero-plaintext-equals-keystream, mode / padding
/// rejection, transform lifecycle, buffer-overlap handling, and key / nonce argument validation — mirroring the way
/// block-cipher tests inherit from <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" />.
/// </summary>
/// <typeparam name="TTest">The concrete test class (self-referential, so MSTest can construct it).</typeparam>
/// <typeparam name="TAlgorithm">The stream-cipher type under test.</typeparam>
[TestClass]
public abstract partial class StreamCipherAlgorithmTests<TTest, TAlgorithm>
    where TTest : StreamCipherAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : StreamCipherAlgorithm, new()
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
    /// <returns>A new <typeparamref name="TAlgorithm" /> instance.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();

    /// <summary>
    /// Gets the required key length, in bytes, for the cipher under test.
    /// </summary>
    /// <returns>The key length in bytes.</returns>
    protected int KeyLengthBytes
    {
        get
        {
            using TAlgorithm alg = CreateAlgorithm();
            return alg.KeySize / 8;
        }
    }

    /// <summary>
    /// Gets the required nonce (IV) length, in bytes, for the cipher under test.
    /// </summary>
    /// <returns>The nonce length in bytes.</returns>
    protected int NonceLengthBytes
    {
        get
        {
            using TAlgorithm alg = CreateAlgorithm();
            return alg.BlockSize / 8;
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
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(seed + i);

        return buffer;
    }
}
