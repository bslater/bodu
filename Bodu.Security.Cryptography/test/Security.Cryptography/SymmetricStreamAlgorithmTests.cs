// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for additive (symmetric, shared-key) stream ciphers derived from
/// <see cref="SymmetricStreamAlgorithm" />, verifying key and nonce property behaviours, transform lifecycle,
/// buffer-overlap handling, key and nonce generation, and disposal semantics.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TAlgorithm">The type of stream algorithm under test.</typeparam>
[TestClass]
public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
    where TTest : SymmetricStreamAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : SymmetricStreamAlgorithm, new()
{
    /// <summary>
    /// Creates an instance of the stream algorithm under test.
    /// </summary>
    /// <returns>A new <typeparamref name="TAlgorithm" /> instance.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();

    /// <summary>
    /// Returns the <see cref="SymmetricStreamAlgorithmSpecification" /> describing the expected observable properties of
    /// <typeparamref name="TAlgorithm" />.
    /// </summary>
    /// <returns>A populated <see cref="SymmetricStreamAlgorithmSpecification" />.</returns>
    protected abstract SymmetricStreamAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Gets the default key length, in bytes, for the algorithm under test.
    /// </summary>
    /// <returns>The default key length in bytes.</returns>
    protected int KeyLengthBytes =>
        GetSpecification().DefaultKeySizeBits / 8;

    /// <summary>
    /// Gets the nonce length, in bytes, for the algorithm under test.
    /// </summary>
    /// <returns>The nonce length in bytes.</returns>
    protected int NonceLengthBytes =>
        GetSpecification().NonceSizeBits / 8;

    /// <summary>
    /// Creates a deterministic key of the algorithm's default key length.
    /// </summary>
    /// <returns>A key byte array.</returns>
    protected byte[] CreateKey() =>
        FillSequential(new byte[KeyLengthBytes], 0x10);

    /// <summary>
    /// Creates a deterministic key of the requested length, expressed in bits.
    /// </summary>
    /// <param name="keySizeBits">The desired key size in bits.</param>
    /// <returns>A key byte array of <paramref name="keySizeBits" /> / 8 bytes.</returns>
    protected byte[] CreateKey(int keySizeBits) =>
        FillSequential(new byte[keySizeBits / 8], 0x10);

    /// <summary>
    /// Creates a deterministic nonce of the algorithm's required length.
    /// </summary>
    /// <returns>A nonce byte array.</returns>
    protected byte[] CreateNonce() =>
        FillSequential(new byte[NonceLengthBytes], 0x40);

    /// <summary>
    /// Creates a deterministic, non-trivial payload of the requested length.
    /// </summary>
    /// <param name="length">The payload length, in bytes.</param>
    /// <returns>A payload byte array.</returns>
    protected static byte[] CreatePayload(int length) =>
        FillSequential(new byte[length], 1);

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

    /// <summary>
    /// Returns one row per entry in <see cref="SymmetricStreamAlgorithmSpecification.LegalKeySizesBits" /> for use as a
    /// <see cref="DynamicDataAttribute" /> source in parameterised tests.
    /// </summary>
    /// <returns>A sequence of single-element arrays, each containing a key size in bits.</returns>
    public static IEnumerable<object[]> LegalKeySizeBitsData() =>
        new TTest().GetSpecification().LegalKeySizesBits
            .Select(keySize => new object[] { keySize });

    /// <summary>
    /// Returns one row per key-size value, in bits, that <see cref="SymmetricStreamAlgorithm.KeySize" /> must reject.
    /// Values are derived from each legal key size and filtered to exclude any size that happens to be legal for the
    /// algorithm under test.
    /// </summary>
    /// <returns>A sequence of single-element arrays, each containing an invalid key size in bits.</returns>
    public static IEnumerable<object[]> InvalidKeySizeBitsData()
    {
        SymmetricStreamAlgorithmSpecification spec = new TTest().GetSpecification();
        HashSet<int> legal = [.. spec.LegalKeySizesBits];
        IEnumerable<int> candidates =
            new[] { -1, 0 }.Concat(
                legal.SelectMany(size => new[]
                {
                    size - 1,
                    size + 1,
                    size * 2,
                    size / 2,
                }));

        foreach (int candidate in candidates.Distinct().OrderBy(size => size))
        {
            if (!legal.Contains(candidate))
                yield return new object[] { candidate };
        }
    }

    /// <summary>
    /// Returns one row per key-size value, in bytes, that <see cref="SymmetricStreamAlgorithm.Key" /> must reject.
    /// Values are derived from each legal key size and filtered to exclude any size that happens to be legal for the
    /// algorithm under test.
    /// </summary>
    /// <returns>A sequence of single-element arrays, each containing an invalid key size in bytes.</returns>
    public static IEnumerable<object[]> InvalidKeySizeBytesData()
    {
        SymmetricStreamAlgorithmSpecification spec = new TTest().GetSpecification();
        HashSet<int> legal = [.. spec.LegalKeySizesBits.Select(size => size / 8)];
        IEnumerable<int> candidates =
            new[] { -1, 0 }.Concat(
                legal.SelectMany(size => new[]
                {
                    size - 1,
                    size + 1,
                    size * 2,
                    size / 2,
                }));

        foreach (int candidate in candidates.Distinct().OrderBy(size => size))
        {
            if (!legal.Contains(candidate))
                yield return new object[] { candidate };
        }
    }

    /// <summary>
    /// Returns one row per nonce-size value, in bytes, that <see cref="SymmetricStreamAlgorithm.Nonce" /> must reject.
    /// Values are derived from the algorithm's single legal nonce size and filtered to exclude the legal length.
    /// </summary>
    /// <returns>A sequence of single-element arrays, each containing an invalid nonce size in bytes.</returns>
    public static IEnumerable<object[]> InvalidNonceSizeBytesData()
    {
        int nonceSize = new TTest().GetSpecification().NonceSizeBits / 8;
        foreach (int candidate in new[] { 0, -1, nonceSize - 1, nonceSize + 1, nonceSize * 2, nonceSize / 2 })
        {
            if (candidate != nonceSize)
                yield return new object[] { candidate };
        }
    }
}
