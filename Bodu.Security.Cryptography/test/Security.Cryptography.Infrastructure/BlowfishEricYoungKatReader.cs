// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishEricYoungKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses Eric Young's published Blowfish reference vectors into <see cref="BlockCipherKnownAnswer" /> rows. The file
/// carries two ECB sections: a variable-key table of <c>key / clear / cipher</c> hex triples, and a set-key sequence (<c>c=&lt;cipher&gt; k[N]=&lt;key&gt;</c>
/// lines over the fixed plaintext declared by <c>data[8]=</c>) that grows the key one byte at a time. The trailing
/// chaining-mode (CBC/CFB/OFB) section is ignored — those are mode-level vectors, not raw single-block ECB.
/// </summary>
/// <remarks>
/// Set-key rows whose key is shorter than <paramref name="minKeyBytes" /> are skipped: Blowfish's specification fixes
/// the minimum key length at 32 bits (4 bytes), so Eric Young's <c>k[1]</c>–<c>k[3]</c> rows exercise an out-of-spec
/// key length that a conforming engine rejects.
/// </remarks>
public static class BlowfishEricYoungKatReader
{
    /// <summary>
    /// Reads every ECB vector from <paramref name="stream" />, yielding one <see cref="BlockCipherKnownAnswer" /> per
    /// variable-key triple and per in-range set-key row.
    /// </summary>
    /// <param name="stream">A readable stream over Eric Young's Blowfish vector file.</param>
    /// <param name="provenance">The provenance stamped onto every emitted vector.</param>
    /// <param name="minKeyBytes">
    /// The minimum key length, in bytes, an emitted vector may carry (spec floor is 4).
    /// </param>
    /// <returns>The vectors, in source order.</returns>
    public static IEnumerable<BlockCipherKnownAnswer> Read(Stream stream, KatProvenance provenance, int minKeyBytes = 4)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        var section = Section.None;
        byte[]? setKeyPlaintext = null;
        int ecbIndex = 0;
        int setKeyIndex = 0;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            if (trimmed.StartsWith("ecb test data", StringComparison.OrdinalIgnoreCase))
            {
                section = Section.Ecb;
                continue;
            }

            if (trimmed.StartsWith("set_key test data", StringComparison.OrdinalIgnoreCase))
            {
                section = Section.SetKey;
                continue;
            }

            if (trimmed.StartsWith("chaining mode", StringComparison.OrdinalIgnoreCase))
            {
                section = Section.Chaining;
                continue;
            }

            switch (section)
            {
                case Section.Ecb:
                {
                    string[] columns = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                    if (columns.Length != 3 || !IsHexBlock(columns[0]) || !IsHexBlock(columns[1]) || !IsHexBlock(columns[2]))
                        continue;

                    ecbIndex++;
                    yield return new BlockCipherKnownAnswer
                    {
                        Name = $"Blowfish ECB #{ecbIndex} (key {columns[0]})",
                        Provenance = provenance,
                        Key = Hex(columns[0]),
                        Plaintext = Hex(columns[1]),
                        Ciphertext = Hex(columns[2]),
                    };
                    break;
                }

                case Section.SetKey:
                {
                    if (trimmed.StartsWith("data[8]", StringComparison.OrdinalIgnoreCase))
                    {
                        setKeyPlaintext = Hex(trimmed[(trimmed.IndexOf('=') + 1)..].Trim());
                        continue;
                    }

                    if (!trimmed.StartsWith("c=", StringComparison.Ordinal) || setKeyPlaintext is null)
                        continue;

                    string cipher = trimmed[2..].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
                    int keyEquals = trimmed.LastIndexOf("]=", StringComparison.Ordinal);
                    if (keyEquals < 0)
                        continue;

                    string keyHex = trimmed[(keyEquals + 2)..].Trim();
                    byte[] key = Hex(keyHex);
                    setKeyIndex++;

                    if (key.Length < minKeyBytes)
                        continue;

                    yield return new BlockCipherKnownAnswer
                    {
                        Name = $"Blowfish set_key k[{setKeyIndex}] ({key.Length}-byte key)",
                        Provenance = provenance,
                        Key = key,
                        Plaintext = (byte[])setKeyPlaintext.Clone(),
                        Ciphertext = Hex(cipher),
                    };
                    break;
                }

                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Indicates whether <paramref name="token" /> is a non-empty run of hex digits of even length.
    /// </summary>
    /// <param name="token">The candidate token.</param>
    /// <returns><see langword="true" /> when the token is a valid byte-aligned hex string.</returns>
    private static bool IsHexBlock(string token)
    {
        if (token.Length == 0 || (token.Length & 1) != 0)
            return false;

        foreach (char c in token)
        {
            if (!Uri.IsHexDigit(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Identifies which section of the vector file the reader is currently traversing.
    /// </summary>
    private enum Section
    {
        /// <summary>
        /// Preamble before any recognized section.
        /// </summary>
        None,

        /// <summary>
        /// The variable-key ECB triple table.
        /// </summary>
        Ecb,

        /// <summary>
        /// The set-key growing-key sequence.
        /// </summary>
        SetKey,

        /// <summary>
        /// The trailing chaining-mode section (ignored).
        /// </summary>
        Chaining,
    }
}
