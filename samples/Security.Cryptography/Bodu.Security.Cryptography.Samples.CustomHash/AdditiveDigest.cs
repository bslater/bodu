// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdditiveDigest.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.CustomHash;

/// <summary>
/// A small, deterministic 128-bit block hash implemented on the library's
/// <see cref="BlockHashAlgorithm" /> base — the classic Merkle-Damgard shape that Tiger, Whirlpool,
/// and the SHA-2 family also derive from. It consumes input in 16-byte blocks, mixes each block into
/// four 32-bit chaining words with an add / rotate / multiply / cross-diffuse round, and finalizes by
/// appending a <c>0x80</c> pad byte plus the little-endian message bit-length before a last avalanche.
/// </summary>
/// <remarks>
/// <para>
/// The whole contract is four members — <see cref="AlgorithmName" />, <see cref="ProcessBlock" />,
/// <see cref="PadBlock" />, and <see cref="ProcessFinalBlock" /> — plus <see cref="Initialize" /> to
/// reset the chaining state and a public parameterless constructor. The base class drives residual
/// buffering, block alignment, and final-block padding orchestration, so a derived hash never has to
/// re-implement the streaming plumbing that <see cref="System.Security.Cryptography.HashAlgorithm" />
/// consumers rely on.
/// </para>
/// <para>
/// This is a teaching construction, not a cryptographic primitive: it is deterministic and well-defined
/// but makes no collision-resistance claims. Its value is showing that a consumer type slots into the
/// exact same <see cref="System.Security.Cryptography.HashAlgorithm" /> surface — and the same shared
/// test contract — as the shipped algorithms.
/// </para>
/// </remarks>
public sealed class AdditiveDigest : BlockHashAlgorithm
{
    /// <summary>The block size in bits (16 bytes) passed to the base class.</summary>
    private const int BlockSizeBits = 128;

    /// <summary>The digest size in bits (16 bytes).</summary>
    private const int DigestSizeBits = 128;

    /// <summary>Fixed initialization vector for the four chaining words (arbitrary odd constants).</summary>
    private static readonly uint[] InitialState =
    [
        0x6A09E667u,
        0xBB67AE85u,
        0x3C6EF372u,
        0xA54FF53Au,
    ];

    /// <summary>Odd multiplier used for multiplicative diffusion (the 32-bit golden-ratio constant).</summary>
    private const uint Multiplier = 0x9E3779B1u;

    // The four 32-bit chaining words. Reset by Initialize, updated by ProcessBlock, read by ProcessFinalBlock.
    private uint _h0;
    private uint _h1;
    private uint _h2;
    private uint _h3;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdditiveDigest" /> class with a 128-bit block and a
    /// 128-bit digest.
    /// </summary>
    public AdditiveDigest()
        : base(BlockSizeBits)
    {
        // HashSizeValue is the inherited HashAlgorithm.HashSize backing field; consumers read it via HashSize.
        HashSizeValue = DigestSizeBits;
        Initialize();
    }

    /// <summary>
    /// The single configuration variant exposed by <see cref="AdditiveDigest" />. The algorithm has no
    /// tunable parameters, so it declares one canonical variant to satisfy the shared hash test contract.
    /// </summary>
    public enum Variant
    {
        /// <summary>The one and only configuration.</summary>
        Default,
    }

    /// <inheritdoc />
    public override string AlgorithmName
    {
        get
        {
            // Match the shipped algorithms: reading any public member after disposal must throw.
            ThrowIfDisposed();
            return "AdditiveDigest/128";
        }
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Restores the four chaining words to their fixed initial constants so the instance can start a
    /// fresh computation.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        _h0 = InitialState[0];
        _h1 = InitialState[1];
        _h2 = InitialState[2];
        _h3 = InitialState[3];
    }

    /// <inheritdoc />
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        // Read the 16-byte block as four little-endian 32-bit words.
        var w0 = BinaryPrimitives.ReadUInt32LittleEndian(block);
        var w1 = BinaryPrimitives.ReadUInt32LittleEndian(block[4..]);
        var w2 = BinaryPrimitives.ReadUInt32LittleEndian(block[8..]);
        var w3 = BinaryPrimitives.ReadUInt32LittleEndian(block[12..]);

        // Absorb each message word into its lane with add / rotate / multiply for local diffusion.
        _h0 = (uint.RotateLeft(_h0 + w0, 7) ^ _h1) * Multiplier;
        _h1 = (uint.RotateLeft(_h1 + w1, 11) ^ _h2) * Multiplier;
        _h2 = (uint.RotateLeft(_h2 + w2, 17) ^ _h3) * Multiplier;
        _h3 = (uint.RotateLeft(_h3 + w3, 23) ^ _h0) * Multiplier;

        // Cross-mix the lanes so a change in any word eventually reaches every chaining word.
        _h0 += uint.RotateLeft(_h3, 5);
        _h1 += uint.RotateLeft(_h0, 9);
        _h2 += uint.RotateLeft(_h1, 13);
        _h3 += uint.RotateLeft(_h2, 19);
    }

    /// <inheritdoc />
    protected override int PadBlock(ReadOnlySpan<byte> block, ulong messageLength, Span<byte> destination)
    {
        var inputLength = block.Length;
        var blockBytes = BlockSize / 8;

        // The 8-byte length field must fit after the 0x80 pad byte; overflow spills into a second block.
        var needsSecondBlock = inputLength >= blockBytes - 8;
        var totalLength = needsSecondBlock ? blockBytes * 2 : blockBytes;

        var padded = destination[..totalLength];
        block.CopyTo(padded);

        // Merkle-Damgard padding: a single 0x80 marker, zero fill, then the length trailer.
        padded[inputLength] = 0x80;
        padded.Slice(inputLength + 1, totalLength - inputLength - 1 - 8).Clear();

        // Encode the total message length in bits as a little-endian 64-bit trailer.
        BinaryPrimitives.WriteUInt64LittleEndian(padded[(totalLength - 8)..], messageLength * 8);

        return totalLength;
    }

    /// <inheritdoc />
    protected override byte[] ProcessFinalBlock()
    {
        // A short finalization avalanche so every output byte depends on the whole chaining state.
        var a = _h0;
        var b = _h1;
        var c = _h2;
        var d = _h3;

        for (var round = 0; round < 3; round++)
        {
            a = (a ^ uint.RotateLeft(d, 16)) * Multiplier;
            b = (b ^ uint.RotateLeft(a, 12)) * Multiplier;
            c = (c ^ uint.RotateLeft(b, 8)) * Multiplier;
            d = (d ^ uint.RotateLeft(c, 7)) * Multiplier;
        }

        var digest = new byte[DigestSizeBits / 8];
        BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(0), a);
        BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(4), b);
        BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(8), c);
        BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(12), d);
        return digest;
    }

    /// <summary>
    /// Releases the algorithm's resources and clears the chaining state.
    /// </summary>
    /// <param name="disposing"><see langword="true" /> to release managed and unmanaged resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            _h0 = _h1 = _h2 = _h3 = 0;
        }

        base.Dispose(disposing);
    }
}
