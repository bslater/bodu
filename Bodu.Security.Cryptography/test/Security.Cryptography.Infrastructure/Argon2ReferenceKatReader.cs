// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2ReferenceKatReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses the Argon2 reference implementation's published vectors (github.com/P-H-C/phc-winner-argon2) into
/// <see cref="KdfKnownAnswer" /> records: the <c>hashtest(...)</c> calls in <c>src/test.c</c>, and the trace files
/// under <c>kats/</c>.
/// </summary>
/// <remarks>
/// Both files are kept in their upstream form. <c>src/test.c</c> switches version with plain assignments (<c>version = ARGON2_VERSION_10;</c>),
/// states memory as a power of two, and publishes each tag with its PHC encoding. A trace file states its variant and
/// version in its banner and lists every input before the final tag.
/// </remarks>
public static partial class Argon2ReferenceKatReader
{
    /// <summary>The Argon2 version 1.0 code.</summary>
    private const int Version10 = 0x10;

    /// <summary>The Argon2 version 1.3 code.</summary>
    private const int Version13 = 0x13;

    /// <summary>
    /// Reads every <c>hashtest(...)</c> call from the reference implementation's <c>src/test.c</c>.
    /// </summary>
    /// <param name="stream">A readable stream over <c>src/test.c</c>.</param>
    /// <param name="citation">The citation recorded as each vector's provenance.</param>
    /// <returns>
    /// The vectors in source order, each carrying the published PHC encoding in <see cref="KdfKnownAnswer.Encoded" />.
    /// </returns>
    /// <exception cref="InvalidDataException">
    /// A call cannot be parsed, or no <c>OUT_LEN</c> is defined before it.
    /// </exception>
    public static IEnumerable<KdfKnownAnswer> ReadHashTests(Stream stream, string citation)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII);

        int version = Version13;
        int outputLength = 0;
        StringBuilder? call = null;

        for (string? line = reader.ReadLine(); line is not null; line = reader.ReadLine())
        {
            string trimmed = line.Trim();

            Match define = OutputLengthPattern().Match(trimmed);
            if (define.Success)
                outputLength = int.Parse(define.Groups["length"].Value, CultureInfo.InvariantCulture);

            if (trimmed.StartsWith("version = ARGON2_VERSION_10;", StringComparison.Ordinal))
                version = Version10;
            else if (trimmed.StartsWith("version = ARGON2_VERSION_NUMBER;", StringComparison.Ordinal))
                version = Version13;

            if (call is null && trimmed.StartsWith("hashtest(version,", StringComparison.Ordinal))
                call = new StringBuilder();

            if (call is null)
                continue;

            call.Append(trimmed).Append(' ');
            if (!trimmed.EndsWith(");", StringComparison.Ordinal))
                continue;

            if (outputLength == 0) throw new InvalidDataException("test.c defines no OUT_LEN before its first hashtest call.");

            yield return ParseHashTest(call.ToString(), version, outputLength, citation);
            call = null;
        }
    }

    /// <summary>
    /// Reads the single vector a reference trace file (for example <c>kats/argon2id_v16</c>) describes.
    /// </summary>
    /// <param name="stream">A readable stream over the trace file.</param>
    /// <param name="citation">The citation recorded as the vector's provenance.</param>
    /// <returns>The vector the trace states, with its final tag as the expected output.</returns>
    /// <exception cref="InvalidDataException">The banner, a cost line, an input line, or the tag is missing.</exception>
    public static KdfKnownAnswer ReadTrace(Stream stream, string citation)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII);

        string? variant = null;
        int version = 0, memory = 0, iterations = 0, parallelism = 0, tagLength = 0;
        byte[]? password = null, salt = null, secret = null, associatedData = null, tag = null;

        for (string? line = reader.ReadLine(); line is not null; line = reader.ReadLine())
        {
            string trimmed = line.Trim();

            Match banner = TraceBannerPattern().Match(trimmed);
            if (banner.Success)
            {
                variant = banner.Groups["variant"].Value;
                version = int.Parse(banner.Groups["version"].Value, CultureInfo.InvariantCulture) == 16 ? Version10 : Version13;
                continue;
            }

            Match cost = TraceCostPattern().Match(trimmed);
            if (cost.Success)
            {
                memory = int.Parse(cost.Groups["memory"].Value, CultureInfo.InvariantCulture);
                iterations = int.Parse(cost.Groups["iterations"].Value, CultureInfo.InvariantCulture);
                parallelism = int.Parse(cost.Groups["lanes"].Value, CultureInfo.InvariantCulture);
                tagLength = int.Parse(cost.Groups["tag"].Value, CultureInfo.InvariantCulture);
                continue;
            }

            password ??= TryReadBytes(trimmed, "Password[");
            salt ??= TryReadBytes(trimmed, "Salt[");
            secret ??= TryReadBytes(trimmed, "Secret[");
            associatedData ??= TryReadBytes(trimmed, "Associated data[");

            if (trimmed.StartsWith("Tag:", StringComparison.Ordinal))
                tag = Hex(trimmed["Tag:".Length..].Replace(" ", string.Empty, StringComparison.Ordinal));
        }

        if (variant is null || memory == 0 || password is null || salt is null || tag is null)
            throw new InvalidDataException("The trace file lacks its banner, cost line, password, salt, or tag.");

        return new KdfKnownAnswer
        {
            Name = $"phc-winner-argon2 kats Argon2{variant} v0x{version:x2}",
            Provenance = KatProvenance.ReferenceImplementation(citation),
            Variant = variant,
            Password = password,
            Salt = salt,
            Secret = secret is { Length: > 0 } ? secret : null,
            AssociatedData = associatedData is { Length: > 0 } ? associatedData : null,
            Memory = memory,
            Iterations = iterations,
            Parallelism = parallelism,
            Version = version,
            OutputLength = tagLength,
            ExpectedHex = Convert.ToHexString(tag).ToLowerInvariant(),
        };
    }

    /// <summary>
    /// Parses one joined <c>hashtest(...)</c> call into a vector.
    /// </summary>
    /// <param name="call">The call's source text, its lines joined by spaces.</param>
    /// <param name="version">The version in force at the call.</param>
    /// <param name="outputLength">The tag length <c>OUT_LEN</c> defines.</param>
    /// <param name="citation">The citation recorded as the vector's provenance.</param>
    /// <returns>The vector the call states.</returns>
    /// <exception cref="InvalidDataException">The call does not match the <c>hashtest</c> signature.</exception>
    private static KdfKnownAnswer ParseHashTest(string call, int version, int outputLength, string citation)
    {
        Match match = HashTestPattern().Match(call);
        if (!match.Success) throw new InvalidDataException($"Unrecognised hashtest call: {call}");

        int iterations = int.Parse(match.Groups["t"].Value, CultureInfo.InvariantCulture);
        int memoryExponent = int.Parse(match.Groups["m"].Value, CultureInfo.InvariantCulture);
        int parallelism = int.Parse(match.Groups["p"].Value, CultureInfo.InvariantCulture);
        string password = match.Groups["password"].Value;
        string salt = match.Groups["salt"].Value;
        string variant = match.Groups["type"].Value;

        // The encoded reference is split across adjacent C string literals; concatenate them.
        string encoded = string.Concat(StringLiteralPattern().Matches(match.Groups["encoded"].Value).Select(literal => literal.Groups["text"].Value));

        return new KdfKnownAnswer
        {
            Name = $"phc-winner-argon2 test.c Argon2{variant} v0x{version:x2} t={iterations} m=2^{memoryExponent} p={parallelism} {password}/{salt}",
            Provenance = KatProvenance.ReferenceImplementation(citation),
            Variant = variant,
            Password = Encoding.ASCII.GetBytes(password),
            Salt = Encoding.ASCII.GetBytes(salt),
            Memory = 1 << memoryExponent,
            Iterations = iterations,
            Parallelism = parallelism,
            Version = version,
            OutputLength = outputLength,
            ExpectedHex = match.Groups["hex"].Value,
            Encoded = encoded,
        };
    }

    /// <summary>
    /// Reads the hex bytes that follow a labelled trace line, such as <c>Salt[16]: 02 02 …</c>.
    /// </summary>
    /// <param name="line">The trimmed trace line.</param>
    /// <param name="label">The label the line must start with.</param>
    /// <returns>The bytes, or <see langword="null" /> when the line carries a different label.</returns>
    private static byte[]? TryReadBytes(string line, string label)
    {
        if (!line.StartsWith(label, StringComparison.Ordinal))
            return null;

        int colon = line.IndexOf(':', StringComparison.Ordinal);
        return Hex(line[(colon + 1)..].Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    /// <summary>
    /// Matches <c>#define OUT_LEN 32</c>.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^#define\s+OUT_LEN\s+(?<length>\d+)$")]
    private static partial Regex OutputLengthPattern();

    /// <summary>
    /// Matches one joined <c>hashtest(...)</c> call.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex("""hashtest\(version,\s*(?<t>\d+),\s*(?<m>\d+),\s*(?<p>\d+),\s*"(?<password>[^"]*)",\s*"(?<salt>[^"]*)",\s*"(?<hex>[0-9a-f]+)",\s*(?<encoded>(?:"[^"]*"\s*)+),\s*Argon2_(?<type>id|i|d)\);""")]
    private static partial Regex HashTestPattern();

    /// <summary>
    /// Matches one C string literal.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex("\"(?<text>[^\"]*)\"")]
    private static partial Regex StringLiteralPattern();

    /// <summary>
    /// Matches a trace banner such as <c>Argon2id version number 16</c>.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^Argon2(?<variant>id|i|d) version number (?<version>\d+)$")]
    private static partial Regex TraceBannerPattern();

    /// <summary>
    /// Matches a trace cost line such as
    /// <c>Memory: 32 KiB, Iterations: 3, Parallelism: 4 lanes, Tag length: 32 bytes</c>.
    /// </summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^Memory: (?<memory>\d+) KiB, Iterations: (?<iterations>\d+), Parallelism: (?<lanes>\d+) lanes, Tag length: (?<tag>\d+) bytes$")]
    private static partial Regex TraceCostPattern();
}
