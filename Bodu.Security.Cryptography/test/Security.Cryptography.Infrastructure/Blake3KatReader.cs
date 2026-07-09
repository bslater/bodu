// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3KatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the official BLAKE3 <c>test_vectors.json</c> reference file (github.com/BLAKE3-team/BLAKE3) into known-answer
/// records for the unkeyed hash. Each case specifies an <c>input_len</c>; the reference input is the repeating 251-byte
/// ramp <c>input[i] = i mod 251</c>, and the <c>hash</c> field is the extended (XOF) output. The reader reconstructs
/// the input and truncates the expected output to the requested length.
/// </summary>
public static class Blake3KatReader
{
    /// <summary>
    /// Reads the unkeyed-hash vectors from <paramref name="stream" />, yielding one
    /// <see cref="MessageDigestKnownAnswer" /> per case with the expected digest truncated to
    /// <paramref name="outputBytes" />.
    /// </summary>
    /// <param name="stream">A readable stream over the BLAKE3 test_vectors.json source.</param>
    /// <param name="outputBytes">
    /// The number of leading output bytes to compare (BLAKE3's first 32 bytes are the standard 256-bit hash).
    /// </param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The reconstructed vectors, in source order.</returns>
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream stream, int outputBytes, string? source = null)
    {
        using JsonDocument document = JsonDocument.Parse(stream);

        foreach (JsonElement testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            int inputLen = testCase.GetProperty("input_len").GetInt32();
            string hashHex = testCase.GetProperty("hash").GetString() ?? string.Empty;

            byte[] message = new byte[inputLen];
            for (int i = 0; i < inputLen; i++) message[i] = (byte)(i % 251);

            yield return new MessageDigestKnownAnswer
            {
                Name = source is null ? $"input_len {inputLen}" : $"{source} input_len {inputLen}",
                Message = message,
                Digest = Hex(hashHex[..(outputBytes * 2)]),
            };
        }
    }
}
