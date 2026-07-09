// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Rfc4503RabbitKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the RFC 4503 Appendix A Rabbit conformance vectors into <see cref="StreamCipherKnownAnswer" /> keystream
/// rows. The octet-form file declares each vector as a <c>key</c> (Appendix A.1, without-IV setup) or <c>mkey</c> +
/// <c>iv</c> (Appendix A.2, with-IV setup) followed by three 16-byte keystream blocks <c>S[0]</c>, <c>S[1]</c>,
/// <c>S[2]</c>; the reader concatenates the three blocks into the expected 48-byte keystream. A persistent <c>mkey</c>
/// spans the successive IV vectors of the A.2 section.
/// </summary>
/// <remarks>
/// RFC 4503 octet strings follow the I2OSP (big-endian) convention, so the emitted key, nonce, and keystream bytes are
/// used as-is. A <c>key</c> line selects the without-IV setup and clears any running IV; an <c>mkey</c> line selects
/// the with-IV setup and leaves the running IV to be supplied by a following <c>iv</c> line.
/// </remarks>
public static class Rfc4503RabbitKatReader
{
    /// <summary>
    /// Reads every Rabbit keystream vector from <paramref name="stream" />, yielding one
    /// <see cref="StreamCipherKnownAnswer" /> per <c>S[0]/S[1]/S[2]</c> triple with the running key and nonce applied.
    /// </summary>
    /// <param name="stream">A readable stream over the RFC 4503 Appendix A source.</param>
    /// <returns>The vectors, in source order.</returns>
    public static IEnumerable<StreamCipherKnownAnswer> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false);

        byte[]? key = null;
        byte[] nonce = [];
        bool withIv = false;
        var blocks = new List<byte[]>(3);
        int index = 0;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            int equals = line.IndexOf('=');
            if (equals < 0)
                continue;

            string name = line[..equals].Trim();
            string value = ParseBracketedHex(line[(equals + 1)..]);

            switch (name)
            {
                case "key":
                    key = Convert.FromHexString(value);
                    nonce = [];
                    withIv = false;
                    blocks.Clear();
                    break;
                case "mkey":
                    key = Convert.FromHexString(value);
                    nonce = [];
                    withIv = true;
                    blocks.Clear();
                    break;
                case "iv":
                    nonce = Convert.FromHexString(value);
                    blocks.Clear();
                    break;
                case "S[0]":
                case "S[1]":
                    blocks.Add(Convert.FromHexString(value));
                    break;
                case "S[2]" when key is not null:
                    blocks.Add(Convert.FromHexString(value));
                    index++;
                    yield return new StreamCipherKnownAnswer
                    {
                        Name = withIv
                            ? $"RFC 4503 Appendix A.2 #{index} (key {Convert.ToHexString(key)}, IV {Convert.ToHexString(nonce)})"
                            : $"RFC 4503 Appendix A.1 #{index} (key {Convert.ToHexString(key)})",
                        Provenance = KatProvenance.Rfc(withIv ? "RFC 4503 Appendix A.2" : "RFC 4503 Appendix A.1"),
                        Key = key,
                        Nonce = nonce,
                        IsKeystream = true,
                        Plaintext = [],
                        Ciphertext = Concat(blocks),
                    };
                    blocks.Clear();
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Extracts the hex digits from a bracketed, space-separated octet list such as <c>[00 11 22]</c>.
    /// </summary>
    /// <param name="value">The raw field value following the <c>=</c>.</param>
    /// <returns>The concatenated hex string with brackets and whitespace removed.</returns>
    private static string ParseBracketedHex(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (Uri.IsHexDigit(c))
                builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Concatenates the collected keystream blocks into a single array.
    /// </summary>
    /// <param name="blocks">The ordered keystream blocks.</param>
    /// <returns>A new array holding every block in sequence.</returns>
    private static byte[] Concat(List<byte[]> blocks)
    {
        int total = 0;
        foreach (byte[] block in blocks)
            total += block.Length;

        byte[] result = new byte[total];
        int offset = 0;
        foreach (byte[] block in blocks)
        {
            Buffer.BlockCopy(block, 0, result, offset, block.Length);
            offset += block.Length;
        }

        return result;
    }
}
