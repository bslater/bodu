// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SampleLog.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees;

/// <summary>
/// The fixed inputs every scenario commits to, plus the small conversions the <see cref="MerkleTree" /> surface needs
/// between the shapes its proof and verify members use.
/// </summary>
/// <remarks>
/// Keeping the corpus here rather than per scenario means the roots printed by different scenarios are directly
/// comparable — the consistency scenario's seven-entry root is the same value the commitment scenario publishes.
/// </remarks>
public static class SampleLog
{
    /// <summary>A seven-entry audit log: the running example for the entry-mode scenarios.</summary>
    public static readonly string[] Entries =
    [
        "2026-01-04 deploy  v1.0.0",
        "2026-01-07 rotate  signing-key",
        "2026-01-11 deploy  v1.0.1",
        "2026-01-19 revoke  cert-4417",
        "2026-02-02 deploy  v1.1.0",
        "2026-02-14 rotate  signing-key",
        "2026-03-01 deploy  v1.2.0",
    ];

    /// <summary>
    /// Returns the log as the read-only memory list the entry-mode surface accepts.
    /// </summary>
    /// <returns>The UTF-8 encoded entries, in order.</returns>
    public static IReadOnlyList<ReadOnlyMemory<byte>> AsEntries() =>
        [.. Entries.Select(entry => (ReadOnlyMemory<byte>)Utf8(entry))];

    /// <summary>
    /// Encodes one entry as UTF-8.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>A fresh byte array holding the encoded text.</returns>
    public static byte[] Utf8(string text) =>
        Encoding.UTF8.GetBytes(text);

    /// <summary>
    /// Converts the <c>byte[][]</c> a proof method returns into the list shape a verify method accepts.
    /// </summary>
    /// <param name="steps">The proof steps.</param>
    /// <returns>The same steps as a read-only memory list.</returns>
    public static IReadOnlyList<ReadOnlyMemory<byte>> ToProof(byte[][] steps) =>
        [.. steps.Select(step => (ReadOnlyMemory<byte>)step)];

    /// <summary>
    /// Returns the first <paramref name="length" /> bytes of the sequence <c>0x00, 0x01, 0x02, …</c>, wrapping at 256.
    /// </summary>
    /// <param name="length">The number of bytes to generate.</param>
    /// <returns>The generated payload.</returns>
    /// <remarks>
    /// This is the preimage the RFC 6962 appendix D block-mode vectors are defined over, so the roots the block
    /// scenarios print can be checked against the published values.
    /// </remarks>
    public static byte[] Payload(int length)
    {
        var bytes = new byte[length];
        for (var index = 0; index < length; index++) bytes[index] = (byte)(index & 0xFF);

        return bytes;
    }

    /// <summary>
    /// Cuts a payload into <paramref name="blockSize" />-byte blocks using the shipped block arithmetic, so a
    /// caller-side slice and a library-side slice cannot diverge.
    /// </summary>
    /// <param name="payload">The payload to cut.</param>
    /// <param name="blockSize">The block size, in bytes.</param>
    /// <returns>The blocks, in order; the final block is short rather than padded.</returns>
    public static IReadOnlyList<ReadOnlyMemory<byte>> Cut(byte[] payload, int blockSize)
    {
        var count = MerkleTree.BlockCount(payload.Length, blockSize);
        var blocks = new ReadOnlyMemory<byte>[count];

        for (long index = 0; index < count; index++)
        {
            blocks[index] = payload.AsMemory(
                (int)MerkleTree.BlockOffset(index, blockSize),
                MerkleTree.BlockLength(payload.Length, index, blockSize));
        }

        return blocks;
    }
}
