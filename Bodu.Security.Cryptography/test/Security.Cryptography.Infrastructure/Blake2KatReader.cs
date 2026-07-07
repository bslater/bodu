// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2KatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the official BLAKE2 <c>blake2-kat.json</c> reference file (github.com/BLAKE2/BLAKE2) into known-answer
/// records. Each JSON entry carries <c>hash</c>, <c>in</c> (message hex), <c>key</c> (key hex, empty for the unkeyed
/// case), and <c>out</c> (digest hex). The reader emits only the entries whose <c>hash</c> matches the requested
/// algorithm name.
/// </summary>
public static class Blake2KatReader
{
    /// <summary>
    /// Reads the vectors for <paramref name="hashName" /> from <paramref name="stream" />, yielding one
    /// <see cref="MessageDigestKnownAnswer" /> each with the per-row key applied (or unset for the unkeyed rows).
    /// </summary>
    /// <param name="stream">A readable stream over the blake2-kat.json source.</param>
    /// <param name="hashName">The <c>hash</c> value to select, for example <c>blake2b</c> or <c>blake2s</c>.</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>The matching vectors, in source order.</returns>
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream stream, string hashName, string? source = null)
    {
        using JsonDocument document = JsonDocument.Parse(stream);

        int index = 0;
        foreach (JsonElement entry in document.RootElement.EnumerateArray())
        {
            if (!entry.GetProperty("hash").GetString()!.Equals(hashName, StringComparison.Ordinal))
                continue;

            string inHex = entry.GetProperty("in").GetString() ?? string.Empty;
            string keyHex = entry.GetProperty("key").GetString() ?? string.Empty;
            string outHex = entry.GetProperty("out").GetString() ?? string.Empty;

            yield return new MessageDigestKnownAnswer
            {
                Name = source is null
                    ? $"{hashName} #{index} ({(keyHex.Length == 0 ? "unkeyed" : "keyed")}, in {inHex.Length / 2} bytes)"
                    : $"{source} {hashName} #{index} ({(keyHex.Length == 0 ? "unkeyed" : "keyed")}, in {inHex.Length / 2} bytes)",
                Message = Hex(inHex),
                Key = keyHex.Length == 0 ? null : Hex(keyHex),
                Digest = Hex(outHex),
            };

            index++;
        }
    }
}
