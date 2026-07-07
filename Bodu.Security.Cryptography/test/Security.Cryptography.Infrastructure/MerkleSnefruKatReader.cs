// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleSnefruKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Pairs Ralph Merkle's Snefru 2.5a reference files into known-answer records: the <c>testSnefru</c> shell script (whose
/// here-documents supply each newline-terminated input) and the matching <c>correctSnefruOutput</c> digest listing (one
/// space-separated hex-word digest per line). Row <c>i</c> of the script is paired with digest line <c>i</c>.
/// </summary>
public static class MerkleSnefruKatReader
{
    /// <summary>
    /// Reads the Merkle Snefru reference inputs and digests, yielding one <see cref="MessageDigestKnownAnswer" /> per
    /// paired record.
    /// </summary>
    /// <param name="inputScript">A readable stream over the <c>testSnefru</c> / <c>testSnefru256</c> shell script.</param>
    /// <param name="digestOutput">A readable stream over the matching <c>correctSnefru[256]Output</c> digest listing.</param>
    /// <param name="digestWords">The number of 32-bit hex words per digest line (4 for 128-bit, 8 for 256-bit).</param>
    /// <param name="source">Optional human-readable citation propagated into each emitted vector's name.</param>
    /// <returns>One vector per paired input/digest record, in source order.</returns>
    /// <exception cref="FormatException">The input and digest counts differ, or a digest line is malformed.</exception>
    public static IEnumerable<MessageDigestKnownAnswer> Read(Stream inputScript, Stream digestOutput, int digestWords, string? source = null)
    {
        List<byte[]> inputs = ParseInputs(inputScript);
        List<byte[]> digests = ParseDigests(digestOutput, digestWords);

        if (inputs.Count != digests.Count)
            throw new FormatException(
                $"Snefru reference mismatch: {inputs.Count} inputs but {digests.Count} digests.");

        for (int i = 0; i < inputs.Count; i++)
        {
            yield return new MessageDigestKnownAnswer
            {
                Name = source is null ? $"Snefru #{i} ({inputs[i].Length} bytes)" : $"{source} #{i} ({inputs[i].Length} bytes)",
                Message = inputs[i],
                Digest = digests[i],
            };
        }
    }

    /// <summary>
    /// Extracts each here-document body from the reference shell script. A body begins after a line invoking
    /// <c>./snefru</c> and runs until a line equal to <c>EOF</c>; every body line contributes its bytes plus the
    /// terminating newline the here-document appends.
    /// </summary>
    /// <param name="stream">The shell-script stream.</param>
    /// <returns>The reconstructed input byte sequences, in script order.</returns>
    private static List<byte[]> ParseInputs(Stream stream)
    {
        var inputs = new List<byte[]>();
        using var reader = new StreamReader(stream);

        string? line;
        bool collecting = false;
        StringBuilder body = new();

        while ((line = reader.ReadLine()) is not null)
        {
            if (!collecting)
            {
                if (line.StartsWith("<<EOF", StringComparison.Ordinal) && line.Contains("./snefru", StringComparison.Ordinal))
                {
                    collecting = true;
                    body.Clear();
                }

                continue;
            }

            if (line == "EOF")
            {
                inputs.Add(Encoding.ASCII.GetBytes(body.ToString()));
                collecting = false;
                continue;
            }

            body.Append(line).Append('\n');
        }

        return inputs;
    }

    /// <summary>
    /// Parses the digest listing, concatenating the <paramref name="digestWords" /> hex words on each non-blank line
    /// into a single digest.
    /// </summary>
    /// <param name="stream">The digest-listing stream.</param>
    /// <param name="digestWords">The number of 32-bit hex words expected per line.</param>
    /// <returns>The parsed digests, in listing order.</returns>
    /// <exception cref="FormatException">A non-blank line does not carry exactly <paramref name="digestWords" /> hex words.</exception>
    private static List<byte[]> ParseDigests(Stream stream, int digestWords)
    {
        var digests = new List<byte[]>();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            string[] words = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length != digestWords)
                throw new FormatException($"Expected {digestWords} hex words on a Snefru digest line but found {words.Length}: '{trimmed}'.");

            digests.Add(Convert.FromHexString(string.Concat(words)));
        }

        return digests;
    }
}
