
using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a pass-through padding strategy that adds and removes no bytes, requiring the caller to provide data whose length is
/// already a multiple of the cipher block size.
/// </summary>
/// <remarks>
/// Use this strategy only when the plaintext length is guaranteed to be block-aligned, for example when encrypting fixed-size records
/// or when another framing layer already handles length information.
/// </remarks>
public sealed class NoPadding : IPaddingStrategy
{
    /// <inheritdoc />
    public bool StripsPaddingOnUnpad => false;

    /// <summary>
    /// Returns a copy of <paramref name="input" /> after verifying that its length is a multiple of <paramref name="blockSize" />.
    /// </summary>
    /// <param name="input">The input data to validate and return.</param>
    /// <param name="blockSize">The required block size in bytes.</param>
    /// <returns>A new byte array containing the same bytes as <paramref name="input" />.</returns>
    /// <exception cref="ArgumentException">Thrown if the length of <paramref name="input" /> is not a multiple of <paramref name="blockSize" />.</exception>
    public byte[] Pad(ReadOnlySpan<byte> input, int blockSize)
    {
        if (input.Length % blockSize != 0)
            throw new ArgumentException("Input must be a multiple of block size when using no padding.", nameof(input));
        return input.ToArray();
    }

    /// <summary>
    /// Returns a copy of <paramref name="input" /> unchanged, since no padding is ever added by this strategy.
    /// </summary>
    /// <param name="input">The input data to return.</param>
    /// <param name="blockSize">The block size in bytes. This value is ignored.</param>
    /// <returns>A new byte array containing the same bytes as <paramref name="input" />.</returns>
    public byte[] Unpad(ReadOnlySpan<byte> input, int blockSize) => input.ToArray();
}
