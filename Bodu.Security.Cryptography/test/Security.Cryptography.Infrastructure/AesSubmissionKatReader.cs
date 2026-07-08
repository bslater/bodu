// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesSubmissionKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the AES-candidate submission ECB known-answer test format (the <c>ecb_vk.txt</c> / <c>ecb_vt.txt</c> /
/// <c>ecb_tbl.txt</c> files shipped by submitters such as Twofish and Serpent) into
/// <see cref="BlockCipherKnownAnswer" /> rows. The format groups vectors under <c>KEYSIZE=</c> sections and carries
/// <c>KEY=</c>, <c>PT=</c>, and <c>CT=</c> hex lines; a value persists until it is re-declared, so the variable-key
/// file fixes <c>PT</c> once and the variable-text file fixes <c>KEY</c> once. Each <c>CT=</c> line completes a vector
/// against the running key and plaintext.
/// </summary>
public static class AesSubmissionKatReader
{
    /// <summary>
    /// Reads every ECB encrypt vector from <paramref name="stream" />, yielding one
    /// <see cref="BlockCipherKnownAnswer" /> per <c>CT=</c> line with the running key and plaintext applied.
    /// </summary>
    /// <param name="stream">A readable stream over an AES-submission ECB KAT file.</param>
    /// <param name="provenance">The provenance stamped onto every emitted vector.</param>
    /// <param name="label">
    /// A short label prefix distinguishing this file's rows (for example <c>VK</c> / <c>VT</c> / <c>TBL</c>).
    /// </param>
    /// <returns>The vectors, in source order.</returns>
    public static IEnumerable<BlockCipherKnownAnswer> Read(Stream stream, KatProvenance provenance, string label)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        int keySizeBits = 0;
        byte[]? key = null;
        byte[]? plaintext = null;
        int index = 0;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            int equals = trimmed.IndexOf('=');
            if (equals < 0)
                continue;

            string name = trimmed[..equals].Trim();
            string value = trimmed[(equals + 1)..].Trim();

            switch (name)
            {
                case "KEYSIZE":
                    keySizeBits = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    key = null;
                    plaintext = null;
                    break;
                case "KEY":
                    key = Hex(value);
                    break;
                case "PT":
                    plaintext = Hex(value);
                    break;
                case "CT" when key is not null && plaintext is not null:
                    index++;
                    yield return new BlockCipherKnownAnswer
                    {
                        Name = $"{label}_{keySizeBits}_{index}",
                        Provenance = provenance,
                        Key = (byte[])key.Clone(),
                        Plaintext = (byte[])plaintext.Clone(),
                        Ciphertext = Hex(value),
                    };
                    break;
                default:
                    break;
            }
        }
    }
}
