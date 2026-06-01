// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitStreamCipher.Diagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <content> Internal diagnostic hooks that expose the Rabbit inner state at the checkpoints described by RFC 4503
/// Appendix B. These exist solely so the Appendix B debugging vectors can be asserted from tests; they are not part of
/// the public surface and play no role in normal keystream generation. </content>
internal sealed partial class RabbitStreamCipher
{
    /// <summary>
    /// Captures the Rabbit inner state (carry, the eight <c>X</c> words, and the eight <c>C</c> counters) at a named
    /// point during setup, matching an RFC 4503 Appendix B checkpoint.
    /// </summary>
    /// <param name="Name">The checkpoint name.</param>
    /// <param name="Carry">The counter carry bit (0 or 1).</param>
    /// <param name="X">A snapshot of the eight state words.</param>
    /// <param name="C">A snapshot of the eight counter words.</param>
    internal sealed record StateCheckpoint(string Name, uint Carry, uint[] X, uint[] C);

    /// <summary>
    /// Re-runs key setup for <paramref name="key" />, recording the RFC 4503 Appendix B.1 checkpoints, then generates
    /// 48 keystream bytes and records the final post-output state.
    /// </summary>
    /// <param name="key">The 16-byte key.</param>
    /// <returns>The ordered list of Appendix B.1 checkpoints.</returns>
    internal static IReadOnlyList<StateCheckpoint> TraceKeySetup(ReadOnlySpan<byte> key)
    {
        var cipher = new RabbitStreamCipher();
        var checkpoints = new List<StateCheckpoint>();

        cipher.ExpandKey(key);
        checkpoints.Add(cipher.Snapshot("Inner state after key expansion"));

        cipher.NextState();
        checkpoints.Add(cipher.Snapshot("Inner state after first key setup iteration"));

        for (var i = 1; i < 4; i++)
            cipher.NextState();
        checkpoints.Add(cipher.Snapshot("Inner state after fourth key setup iteration"));

        for (var i = 0; i < 8; i++)
            cipher._c[i] ^= cipher._x[(i + 4) & 7];
        checkpoints.Add(cipher.Snapshot("Inner state after final key setup xor"));

        Span<byte> scratch = stackalloc byte[BlockSizeBytes];
        for (var i = 0; i < 3; i++)
            cipher.NextKeystreamBlock(scratch);
        checkpoints.Add(cipher.Snapshot("Inner state after generation of 48 bytes of output"));

        return checkpoints;
    }

    /// <summary>
    /// Re-runs key setup followed by IV setup for <paramref name="key" /> and <paramref name="nonce" />, recording the
    /// RFC 4503 Appendix B.2 checkpoints.
    /// </summary>
    /// <param name="key">The 16-byte key.</param>
    /// <param name="nonce">The 8-byte IV.</param>
    /// <returns>The ordered list of Appendix B.2 checkpoints.</returns>
    internal static IReadOnlyList<StateCheckpoint> TraceIvSetup(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce)
    {
        var cipher = new RabbitStreamCipher();
        var checkpoints = new List<StateCheckpoint>();

        cipher.KeySetup(key);

        cipher.MixIv(nonce);
        checkpoints.Add(cipher.Snapshot("Inner state after IV expansion"));

        cipher.NextState();
        checkpoints.Add(cipher.Snapshot("Inner state after first IV setup iteration"));

        for (var i = 1; i < 4; i++)
            cipher.NextState();
        checkpoints.Add(cipher.Snapshot("Inner state after fourth IV setup iteration"));

        return checkpoints;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitStreamCipher" /> class as an empty engine for diagnostic
    /// tracing; no key or IV is bound.
    /// </summary>
    private RabbitStreamCipher()
    {
    }

    /// <summary>
    /// Expands only the key into the state and counters, without running any setup iterations or the final counter XOR.
    /// </summary>
    /// <param name="key">The 16-byte key.</param>
    /// <remarks>
    /// Used by <see cref="TraceKeySetup(ReadOnlySpan{byte})" /> to capture the post-expansion checkpoint.
    /// </remarks>
    private void ExpandKey(ReadOnlySpan<byte> key)
    {
        uint k0 = SubKey(key, 0), k1 = SubKey(key, 1), k2 = SubKey(key, 2), k3 = SubKey(key, 3);
        uint k4 = SubKey(key, 4), k5 = SubKey(key, 5), k6 = SubKey(key, 6), k7 = SubKey(key, 7);

        _x[0] = (k1 << 16) | k0;
        _x[1] = (k6 << 16) | k5;
        _x[2] = (k3 << 16) | k2;
        _x[3] = (k0 << 16) | k7;
        _x[4] = (k5 << 16) | k4;
        _x[5] = (k2 << 16) | k1;
        _x[6] = (k7 << 16) | k6;
        _x[7] = (k4 << 16) | k3;

        _c[0] = (k4 << 16) | k5;
        _c[1] = (k1 << 16) | k2;
        _c[2] = (k6 << 16) | k7;
        _c[3] = (k3 << 16) | k4;
        _c[4] = (k0 << 16) | k1;
        _c[5] = (k5 << 16) | k6;
        _c[6] = (k2 << 16) | k3;
        _c[7] = (k7 << 16) | k0;

        _carry = 0;
    }

    /// <summary>
    /// Captures the current inner state as a named checkpoint.
    /// </summary>
    /// <param name="name">The checkpoint name.</param>
    /// <returns>A snapshot of the carry, <c>X</c>, and <c>C</c> state.</returns>
    private StateCheckpoint Snapshot(string name) =>
        new(name, _carry, (uint[])_x.Clone(), (uint[])_c.Clone());
}
