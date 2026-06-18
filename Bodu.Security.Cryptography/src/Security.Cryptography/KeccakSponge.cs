// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeccakSponge.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides an incremental Keccak sponge (NIST FIPS 202) with absorb-then-streaming-squeeze semantics, covering the
/// SHA3-256 / SHA3-512 fixed-output hashes and the SHAKE128 / SHAKE256 extendable-output functions consumed by the
/// ML-KEM and ML-DSA engines.
/// </summary>
/// <remarks>
/// <para>
/// The sponge is a mutable struct: create one via the <c>Create*</c> factories, call <see cref="Absorb" /> any number
/// of times, then call <see cref="Squeeze" /> repeatedly to stream output. The first squeeze applies the multi-rate
/// padding and closes the absorb phase; absorbing after squeezing throws. Byte access into the lane state uses explicit
/// shift arithmetic, so behavior is independent of host endianness.
/// </para>
/// <para>
/// The lattice-cryptography samplers require an unbounded output stream (rejection sampling squeezes until enough
/// candidates are accepted), which the platform-gated BCL SHAKE types cannot provide portably; this sponge keeps the
/// library self-contained.
/// </para>
/// </remarks>
internal struct KeccakSponge
{
    /// <summary>The sponge rate, in bytes.</summary>
    private readonly int _rateBytes;

    /// <summary>The FIPS 202 domain-separation suffix byte applied during multi-rate padding.</summary>
    private readonly byte _domainSuffix;

    /// <summary>The 25-lane Keccak state buffer permuted in place across the sponge operation.</summary>
    private LaneBuffer _state;

    /// <summary>The current byte position within the rate portion of the state.</summary>
    private int _position;

    /// <summary>Indicates whether the sponge has entered the squeeze phase.</summary>
    private bool _squeezing;

    /// <summary>Domain-separation suffix for the fixed-output SHA-3 hashes.</summary>
    private const byte Sha3DomainSuffix = 0x06;

    /// <summary>Domain-separation suffix for the SHAKE extendable-output functions.</summary>
    private const byte ShakeDomainSuffix = 0x1F;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeccakSponge" /> struct with the given rate and domain suffix.
    /// </summary>
    /// <param name="rateBytes">The sponge rate in bytes.</param>
    /// <param name="domainSuffix">The FIPS 202 domain-separation suffix byte.</param>
    private KeccakSponge(int rateBytes, byte domainSuffix)
    {
        _state = default;
        _rateBytes = rateBytes;
        _domainSuffix = domainSuffix;
        _position = 0;
        _squeezing = false;
    }

    /// <summary>
    /// Creates a SHA3-256 sponge (rate 136 bytes). Squeeze exactly 32 bytes for the standard digest.
    /// </summary>
    /// <returns>A fresh sponge ready to absorb.</returns>
    internal static KeccakSponge CreateSha3_256() =>
        new(136, Sha3DomainSuffix);

    /// <summary>
    /// Creates a SHA3-512 sponge (rate 72 bytes). Squeeze exactly 64 bytes for the standard digest.
    /// </summary>
    /// <returns>A fresh sponge ready to absorb.</returns>
    internal static KeccakSponge CreateSha3_512() =>
        new(72, Sha3DomainSuffix);

    /// <summary>
    /// Creates a SHAKE128 sponge (rate 168 bytes, 128-bit security).
    /// </summary>
    /// <returns>A fresh sponge ready to absorb.</returns>
    internal static KeccakSponge CreateShake128() =>
        new(168, ShakeDomainSuffix);

    /// <summary>
    /// Creates a SHAKE256 sponge (rate 136 bytes, 256-bit security).
    /// </summary>
    /// <returns>A fresh sponge ready to absorb.</returns>
    internal static KeccakSponge CreateShake256() =>
        new(136, ShakeDomainSuffix);

    /// <summary>
    /// Computes the 32-byte SHA3-256 digest of <paramref name="input" />.
    /// </summary>
    /// <param name="input">The input bytes.</param>
    /// <param name="output">The 32-byte span that receives the digest.</param>
    /// <exception cref="ArgumentException"><paramref name="output" /> is not exactly 32 bytes.</exception>
    internal static void Sha3_256(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, 32);

        KeccakSponge sponge = CreateSha3_256();
        sponge.Absorb(input);
        sponge.Squeeze(output);
        sponge.Clear();
    }

    /// <summary>
    /// Computes the 32-byte SHA3-256 digest of the concatenation <paramref name="input1" /> ‖
    /// <paramref name="input2" />, matching the two-part call shapes of the FIPS 203 H and G functions.
    /// </summary>
    /// <param name="input1">The first input segment.</param>
    /// <param name="input2">The second input segment.</param>
    /// <param name="output">The 32-byte span that receives the digest.</param>
    /// <exception cref="ArgumentException"><paramref name="output" /> is not exactly 32 bytes.</exception>
    internal static void Sha3_256(ReadOnlySpan<byte> input1, ReadOnlySpan<byte> input2, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, 32);

        KeccakSponge sponge = CreateSha3_256();
        sponge.Absorb(input1);
        sponge.Absorb(input2);
        sponge.Squeeze(output);
        sponge.Clear();
    }

    /// <summary>
    /// Computes the 64-byte SHA3-512 digest of <paramref name="input" />.
    /// </summary>
    /// <param name="input">The input bytes.</param>
    /// <param name="output">The 64-byte span that receives the digest.</param>
    /// <exception cref="ArgumentException"><paramref name="output" /> is not exactly 64 bytes.</exception>
    internal static void Sha3_512(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, 64);

        KeccakSponge sponge = CreateSha3_512();
        sponge.Absorb(input);
        sponge.Squeeze(output);
        sponge.Clear();
    }

    /// <summary>
    /// Computes the 64-byte SHA3-512 digest of the concatenation <paramref name="input1" /> ‖
    /// <paramref name="input2" />, matching the two-part call shape of the FIPS 203 G function.
    /// </summary>
    /// <param name="input1">The first input segment.</param>
    /// <param name="input2">The second input segment.</param>
    /// <param name="output">The 64-byte span that receives the digest.</param>
    /// <exception cref="ArgumentException"><paramref name="output" /> is not exactly 64 bytes.</exception>
    internal static void Sha3_512(ReadOnlySpan<byte> input1, ReadOnlySpan<byte> input2, Span<byte> output)
    {
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, 64);

        KeccakSponge sponge = CreateSha3_512();
        sponge.Absorb(input1);
        sponge.Absorb(input2);
        sponge.Squeeze(output);
        sponge.Clear();
    }

    /// <summary>
    /// Computes SHAKE128 over <paramref name="input" />, filling <paramref name="output" /> with the requested number
    /// of output bytes.
    /// </summary>
    /// <param name="input">The input bytes.</param>
    /// <param name="output">The span that receives the output stream.</param>
    internal static void Shake128(ReadOnlySpan<byte> input, Span<byte> output)
    {
        KeccakSponge sponge = CreateShake128();
        sponge.Absorb(input);
        sponge.Squeeze(output);
        sponge.Clear();
    }

    /// <summary>
    /// Computes SHAKE256 over <paramref name="input" />, filling <paramref name="output" /> with the requested number
    /// of output bytes.
    /// </summary>
    /// <param name="input">The input bytes.</param>
    /// <param name="output">The span that receives the output stream.</param>
    internal static void Shake256(ReadOnlySpan<byte> input, Span<byte> output)
    {
        KeccakSponge sponge = CreateShake256();
        sponge.Absorb(input);
        sponge.Squeeze(output);
        sponge.Clear();
    }

    /// <summary>
    /// Computes SHAKE256 over the concatenation <paramref name="input1" /> ‖ <paramref name="input2" />, matching the
    /// two-part call shapes of the FIPS 203 PRF and KDF functions without an intermediate buffer.
    /// </summary>
    /// <param name="input1">The first input segment.</param>
    /// <param name="input2">The second input segment.</param>
    /// <param name="output">The span that receives the output stream.</param>
    internal static void Shake256(ReadOnlySpan<byte> input1, ReadOnlySpan<byte> input2, Span<byte> output)
    {
        KeccakSponge sponge = CreateShake256();
        sponge.Absorb(input1);
        sponge.Absorb(input2);
        sponge.Squeeze(output);
        sponge.Clear();
    }

    /// <summary>
    /// Absorbs input bytes into the sponge state.
    /// </summary>
    /// <param name="data">The bytes to absorb. May be empty.</param>
    /// <exception cref="InvalidOperationException">Output has already been squeezed from this sponge.</exception>
    internal void Absorb(ReadOnlySpan<byte> data)
    {
        if (_squeezing) throw new InvalidOperationException(CryptoResourceStrings.Crypt_Invalid_XofSqueezeAfterAbsorb);

        Span<ulong> state = _state;
        for (int i = 0; i < data.Length; i++)
        {
            state[_position >> 3] ^= (ulong)data[i] << (8 * (_position & 7));
            _position++;

            if (_position == _rateBytes)
            {
                KeccakPermutation.Permute(state);
                _position = 0;
            }
        }
    }

    /// <summary>
    /// Zeroes the sponge state. Call when the absorbed material is secret and the sponge is no longer needed.
    /// </summary>
    internal void Clear() =>
        Reset();

    /// <summary>
    /// Returns the sponge to its initial empty state so it can absorb a new message.
    /// </summary>
    internal void Reset()
    {
        Span<ulong> state = _state;
        CryptographyHelper.Clear(state);

        _position = 0;
        _squeezing = false;
    }

    /// <summary>
    /// Squeezes output bytes from the sponge. The first call applies the multi-rate padding and transitions the sponge
    /// into the squeeze phase; subsequent calls continue the same output stream.
    /// </summary>
    /// <param name="destination">The span to fill with output bytes. May be empty.</param>
    internal void Squeeze(Span<byte> destination)
    {
        Span<ulong> state = _state;

        if (!_squeezing)
        {
            // Multi-rate padding (pad10*1): domain suffix at the current position, 0x80 at the final rate byte.
            state[_position >> 3] ^= (ulong)_domainSuffix << (8 * (_position & 7));
            state[(_rateBytes - 1) >> 3] ^= 0x80UL << (8 * ((_rateBytes - 1) & 7));
            KeccakPermutation.Permute(state);

            _position = 0;
            _squeezing = true;
        }

        for (int i = 0; i < destination.Length; i++)
        {
            if (_position == _rateBytes)
            {
                KeccakPermutation.Permute(state);
                _position = 0;
            }

            destination[i] = (byte)(state[_position >> 3] >> (8 * (_position & 7)));
            _position++;
        }
    }

    /// <summary>
    /// Inline storage for the 25-lane Keccak state, kept stack-allocatable so per-operation sponges never touch the
    /// heap.
    /// </summary>
    [InlineArray(KeccakPermutation.StateWords)]
    private struct LaneBuffer
    {
        /// <summary>The first lane of the inline buffer; the <see cref="InlineArrayAttribute" /> expands it to 25 lanes.</summary>
        private ulong _lane0;
    }
}
