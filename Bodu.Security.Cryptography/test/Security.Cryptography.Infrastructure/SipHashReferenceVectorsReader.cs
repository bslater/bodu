// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashReferenceVectorsReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.RegularExpressions;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the SipHash reference implementation's <c>vectors.h</c> file (github.com/veorq/SipHash) into known-answer
/// records. The file declares C arrays such as <c>vectors_sip128[64][16]</c> whose row <c>i</c> is the tag over the
/// <c>i</c>-byte message <c>0x00..0x(i-1)</c> under the fixed reference key <c>0x00..0x0F</c>.
/// </summary>
public static partial class SipHashReferenceVectorsReader
{
    /// <summary>The fixed reference key used by every vector in <c>vectors.h</c>: the bytes 0x00..0x0F.</summary>
    private static readonly byte[] ReferenceKey =
        [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];

    /// <summary>
    /// Reads the named vector table from <paramref name="stream" /> and yields one
    /// <see cref="MessageDigestKnownAnswer" /> per row, with the reference key applied and the incremental message
    /// reconstructed from the row index.
    /// </summary>
    /// <param name="stream">A readable stream over the <c>vectors.h</c> source text.</param>
    /// <param name="tableName">The C array identifier to extract, for example <c>vectors_sip128</c>.</param>
    /// <param name="outputBytes">
    /// The per-row output width in bytes (8 for the 64-bit variant, 16 for the 128-bit variant).
    /// </param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>One vector per table row, in source order.</returns>
    /// <exception cref="FormatException">
    /// The named table cannot be located, or its byte count is not a multiple of <paramref name="outputBytes" />.
    /// </exception>
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream stream, string tableName, int outputBytes, string? source = null)
    {
        using var reader = new StreamReader(stream);
        string text = reader.ReadToEnd();

        // Anchor on the array subscript ("name[") so a mention of the table name in a comment or prose header
        // does not get mistaken for the declaration.
        int declIndex = text.IndexOf(tableName + "[", StringComparison.Ordinal);
        if (declIndex < 0)
            throw new FormatException($"Table '{tableName}' was not found in the SipHash vectors source.");

        int open = text.IndexOf('{', declIndex);
        int close = text.IndexOf("};", open, StringComparison.Ordinal);
        if (open < 0 || close < 0)
            throw new FormatException($"Table '{tableName}' is not terminated by a '}};' block.");

        string body = text[(open + 1)..close];
        byte[] bytes = ByteToken().Matches(body)
            .Select(m => (byte)Convert.ToInt32(m.Groups[1].Value, 16))
            .ToArray();

        if (bytes.Length % outputBytes != 0)
            throw new FormatException($"Table '{tableName}' yielded {bytes.Length} bytes, not a multiple of {outputBytes}.");

        int rows = bytes.Length / outputBytes;
        for (int i = 0; i < rows; i++)
        {
            byte[] message = new byte[i];
            for (int j = 0; j < i; j++) message[j] = (byte)j;

            yield return new MessageDigestKnownAnswer
            {
                Name = source is null ? $"{tableName} #{i}" : $"{source} {tableName} #{i}",
                Key = ReferenceKey,
                Message = message,
                Digest = bytes.AsSpan(i * outputBytes, outputBytes).ToArray(),
            };
        }
    }

    /// <summary>
    /// Matches a single C hex byte literal such as <c>0x3f</c>.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex(@"0x([0-9a-fA-F]{1,2})")]
    private static partial Regex ByteToken();
}
