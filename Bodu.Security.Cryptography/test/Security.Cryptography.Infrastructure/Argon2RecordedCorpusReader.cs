// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2RecordedCorpusReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Parses a recorded Argon2 corpus - one pipe-separated row per derivation, as produced from a released build - into
/// <see cref="KdfKnownAnswer" /> records.
/// </summary>
/// <remarks>
/// Lines starting with <c>#</c> are comments. Each row holds
/// <c>case|variant|version|memoryKiB|iterations|parallelism|tagLength|password|salt|secret|associatedData|tag</c>,
/// with the byte fields in hex; an empty secret or associated-data field means none was used.
/// </remarks>
public static class Argon2RecordedCorpusReader
{
    /// <summary>The number of pipe-separated fields in a row.</summary>
    private const int FieldCount = 12;

    /// <summary>
    /// Reads every row of a recorded corpus.
    /// </summary>
    /// <param name="stream">A readable stream over the corpus file.</param>
    /// <param name="source">The label prefixed to each vector's name, for example <c>Bodu 1.0.0</c>.</param>
    /// <param name="citation">The citation recorded as each vector's provenance.</param>
    /// <returns>The vectors, in file order.</returns>
    /// <exception cref="InvalidDataException">A row does not have the expected number of fields.</exception>
    public static IEnumerable<KdfKnownAnswer> Read(Stream stream, string source, string citation)
    {
        using var reader = new StreamReader(stream, Encoding.ASCII);

        for (string? line = reader.ReadLine(); line is not null; line = reader.ReadLine())
        {
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            string[] fields = line.Split('|');
            if (fields.Length != FieldCount) throw new InvalidDataException($"Expected {FieldCount} fields but found {fields.Length}: {line}");

            string variant = fields[1];
            int version = Convert.ToInt32(fields[2], 16);
            int memory = int.Parse(fields[3], CultureInfo.InvariantCulture);
            int iterations = int.Parse(fields[4], CultureInfo.InvariantCulture);
            int parallelism = int.Parse(fields[5], CultureInfo.InvariantCulture);
            int tagLength = int.Parse(fields[6], CultureInfo.InvariantCulture);

            yield return new KdfKnownAnswer
            {
                Name = $"{source} {fields[0]} Argon2{variant} v0x{version:x2} m={memory} t={iterations} p={parallelism} T={tagLength}",
                Provenance = KatProvenance.InternalRegression(citation),
                Variant = variant,
                Password = Hex(fields[7]),
                Salt = Hex(fields[8]),
                Secret = fields[9].Length == 0 ? null : Hex(fields[9]),
                AssociatedData = fields[10].Length == 0 ? null : Hex(fields[10]),
                Memory = memory,
                Iterations = iterations,
                Parallelism = parallelism,
                Version = version,
                OutputLength = tagLength,
                ExpectedHex = fields[11],
            };
        }
    }
}
