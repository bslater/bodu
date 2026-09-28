// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3KatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the official BLAKE3 <c>test_vectors.json</c> reference file (github.com/BLAKE3-team/BLAKE3) into known-answer
/// records for its three modes: the unkeyed hash, the keyed hash and key derivation. Each case specifies an
/// <c>input_len</c>; the reference input is the repeating 251-byte ramp <c>input[i] = i mod 251</c>, and each mode's
/// field is the extended (XOF) output. The reader reconstructs the input and truncates the expected output to the
/// requested length.
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
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream stream, int outputBytes, string? source = null) =>
        ReadMode(stream, "hash", keyField: null, outputBytes, source);

    /// <summary>
    /// Reads the keyed-hash vectors from <paramref name="stream" />, yielding one
    /// <see cref="MessageDigestKnownAnswer" /> per case whose <see cref="KeyedKnownAnswer.Key" /> is the file's 32-byte
    /// key and whose digest is truncated to <paramref name="outputBytes" />.
    /// </summary>
    /// <param name="stream">A readable stream over the BLAKE3 test_vectors.json source.</param>
    /// <param name="outputBytes">The number of leading output bytes to compare.</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The reconstructed vectors, in source order.</returns>
    public static IEnumerable<MessageDigestKnownAnswer> ReadKeyedHash(Stream stream, int outputBytes, string? source = null) =>
        ReadMode(stream, "keyed_hash", keyField: "key", outputBytes, source);

    /// <summary>
    /// Reads the key-derivation vectors from <paramref name="stream" />, yielding one
    /// <see cref="MessageDigestKnownAnswer" /> per case whose <see cref="KeyedKnownAnswer.Key" /> is the ASCII context
    /// string, whose message is the key material, and whose digest is the derived key truncated to
    /// <paramref name="outputBytes" />.
    /// </summary>
    /// <param name="stream">A readable stream over the BLAKE3 test_vectors.json source.</param>
    /// <param name="outputBytes">The number of leading output bytes to compare.</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The reconstructed vectors, in source order.</returns>
    public static IEnumerable<MessageDigestKnownAnswer> ReadDeriveKey(Stream stream, int outputBytes, string? source = null) =>
        ReadMode(stream, "derive_key", keyField: "context_string", outputBytes, source);

    /// <summary>
    /// Reads one mode's vectors from <paramref name="stream" />.
    /// </summary>
    /// <param name="stream">A readable stream over the BLAKE3 test_vectors.json source.</param>
    /// <param name="outputField">The name of the mode's output field in each case.</param>
    /// <param name="keyField">
    /// The name of the top-level field whose ASCII bytes become each vector's key, or <see langword="null" /> for none.
    /// </param>
    /// <param name="outputBytes">The number of leading output bytes to compare.</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The reconstructed vectors, in source order.</returns>
    private static IEnumerable<MessageDigestKnownAnswer> ReadMode(Stream stream, string outputField, string? keyField, int outputBytes, string? source)
    {
        using JsonDocument document = JsonDocument.Parse(stream);

        byte[]? key = keyField is null ? null : Encoding.ASCII.GetBytes(document.RootElement.GetProperty(keyField).GetString() ?? string.Empty);
        string mode = outputField == "hash" ? string.Empty : outputField + " ";

        foreach (JsonElement testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            int inputLen = testCase.GetProperty("input_len").GetInt32();
            string outputHex = testCase.GetProperty(outputField).GetString() ?? string.Empty;

            byte[] message = new byte[inputLen];
            for (int i = 0; i < inputLen; i++) message[i] = (byte)(i % 251);

            yield return new MessageDigestKnownAnswer
            {
                Name = source is null ? $"{mode}input_len {inputLen}" : $"{source} {mode}input_len {inputLen}",
                Key = key,
                Message = message,
                Digest = Hex(outputHex[..(outputBytes * 2)]),
            };
        }
    }
}
