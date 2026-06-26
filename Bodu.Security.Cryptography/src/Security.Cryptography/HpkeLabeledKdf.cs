// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HpkeLabeledKdf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the labeled extract-and-expand primitives <c>LabeledExtract</c> and <c>LabeledExpand</c> of RFC 9180
/// §4 and §5.1, which prefix every HKDF input with the protocol version label <c>"HPKE-v1"</c> and a suite identifier
/// so that derivations for different suites and purposes never collide.
/// </summary>
internal static class HpkeLabeledKdf
{
    /// <summary>The HPKE protocol version label prepended to every labeled input.</summary>
    private static ReadOnlySpan<byte> VersionLabel => "HPKE-v1"u8;

    /// <summary>
    /// Computes <c>LabeledExtract(salt, label, ikm) = Extract(salt, "HPKE-v1" ‖ suite_id ‖ label ‖ ikm)</c>.
    /// </summary>
    /// <param name="hashAlgorithm">The HKDF hash algorithm.</param>
    /// <param name="suiteId">The KEM or HPKE suite identifier.</param>
    /// <param name="salt">The HKDF extract salt.</param>
    /// <param name="label">The ASCII label that names the derivation.</param>
    /// <param name="ikm">The input keying material.</param>
    /// <returns>The pseudorandom key, one hash length long.</returns>
    public static byte[] LabeledExtract(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> suiteId, ReadOnlySpan<byte> salt, ReadOnlySpan<byte> label, ReadOnlySpan<byte> ikm)
    {
        int length = VersionLabel.Length + suiteId.Length + label.Length + ikm.Length;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            int offset = 0;
            offset += Append(buffer, offset, VersionLabel);
            offset += Append(buffer, offset, suiteId);
            offset += Append(buffer, offset, label);
            offset += Append(buffer, offset, ikm);

            return Hkdf.Extract(hashAlgorithm, buffer.AsSpan(0, offset), salt);
        }
        finally
        {
            // The labeled input embeds the secret ikm; zero it before returning the buffer to the pool.
            CryptographicOperations.ZeroMemory(buffer.AsSpan(0, length));
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Computes <c>LabeledExpand(prk, label, info, L) = Expand(prk, I2OSP(L,2) ‖ "HPKE-v1" ‖ suite_id ‖ label ‖ info, L)</c>
    /// , writing the <paramref name="output" />.Length octets of output keying material into <paramref name="output" />.
    /// </summary>
    /// <param name="hashAlgorithm">The HKDF hash algorithm.</param>
    /// <param name="suiteId">The KEM or HPKE suite identifier.</param>
    /// <param name="prk">The pseudorandom key.</param>
    /// <param name="label">The ASCII label that names the derivation.</param>
    /// <param name="info">The application-supplied context.</param>
    /// <param name="output">The span that receives the output keying material; its length is the requested <c>L</c>.</param>
    public static void LabeledExpand(HashAlgorithmName hashAlgorithm, ReadOnlySpan<byte> suiteId, ReadOnlySpan<byte> prk, ReadOnlySpan<byte> label, ReadOnlySpan<byte> info, Span<byte> output)
    {
        if (output.Length == 0)
            return;

        // L is encoded into a two-byte field, so a longer request cannot be represented and must be rejected before the
        // truncating cast below rather than silently wrapping.
        ThrowHelper.ThrowIfGreaterThan(output.Length, ushort.MaxValue);

        int length = 2 + VersionLabel.Length + suiteId.Length + label.Length + info.Length;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)output.Length);
            int offset = 2;
            offset += Append(buffer, offset, VersionLabel);
            offset += Append(buffer, offset, suiteId);
            offset += Append(buffer, offset, label);
            offset += Append(buffer, offset, info);

            Hkdf.Expand(hashAlgorithm, prk, output, buffer.AsSpan(0, offset));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer.AsSpan(0, length));
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Copies <paramref name="source" /> into <paramref name="buffer" /> at <paramref name="offset" />.
    /// </summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="offset">The offset at which to begin writing.</param>
    /// <param name="source">The bytes to copy.</param>
    /// <returns>The number of bytes written, equal to <paramref name="source" />.Length.</returns>
    private static int Append(byte[] buffer, int offset, ReadOnlySpan<byte> source)
    {
        source.CopyTo(buffer.AsSpan(offset));
        return source.Length;
    }
}
