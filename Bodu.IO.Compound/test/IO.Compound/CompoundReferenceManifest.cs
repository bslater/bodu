// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReferenceManifest.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

/// <summary>
/// Describes a single expected entry within a reference compound-file fixture, as captured by an independent parser.
/// </summary>
/// <param name="Path">The storage-qualified entry path, for example <c>Storage 1/Stream 1</c>.</param>
/// <param name="Type">The entry kind, either <c>storage</c> or <c>stream</c>.</param>
/// <param name="Size">The stream size in bytes; <c>0</c> for storages.</param>
/// <param name="Sha256">
/// The lowercase hexadecimal SHA-256 of the stream bytes, or <see langword="null" /> for storages.
/// </param>
public sealed record CompoundManifestEntry(string Path, string Type, long Size, string? Sha256);

/// <summary>
/// A known-answer row describing the expected container facts of one valid reference fixture.
/// </summary>
/// <param name="RelativePath">The corpus-relative fixture path, for example <c>valid/clean.dat</c>.</param>
/// <param name="SectorSize">The expected regular sector size in bytes.</param>
/// <param name="RootClsid">The expected root-storage class identifier in lowercase, or an empty string.</param>
/// <param name="Entries">The expected directory entries.</param>
public sealed record CompoundReferenceFixtureKat(
    string RelativePath,
    int SectorSize,
    string RootClsid,
    IReadOnlyList<CompoundManifestEntry> Entries)
    : IKat
{
    /// <inheritdoc />
    public string Name => RelativePath;
}

/// <summary>
/// Loads the embedded reference-fixture manifest and exposes its rows for data-driven tests.
/// </summary>
internal static class CompoundReferenceManifest
{
    /// <summary>The deserialization options matching the manifest's camel-cased property names.</summary>
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web);

    /// <summary>The parsed valid-fixture rows, loaded once.</summary>
    private static readonly Lazy<IReadOnlyList<CompoundReferenceFixtureKat>> s_valid = new(LoadValid);

    /// <summary>
    /// Gets the valid-fixture rows as <c>[DynamicData]</c> argument arrays.
    /// </summary>
    /// <returns>
    /// A sequence of single-element argument arrays, each wrapping a <see cref="CompoundReferenceFixtureKat" />.
    /// </returns>
    public static IEnumerable<object[]> ValidFixtures()
    {
        foreach (CompoundReferenceFixtureKat kat in s_valid.Value)
            yield return [kat];
    }

    /// <summary>
    /// Parses the embedded manifest JSON into known-answer rows.
    /// </summary>
    /// <returns>The parsed valid-fixture rows.</returns>
    private static IReadOnlyList<CompoundReferenceFixtureKat> LoadValid()
    {
        ManifestDto manifest = JsonSerializer.Deserialize<ManifestDto>(CompoundFixtures.ReadValidManifestJson(), s_options)
            ?? throw new InvalidOperationException("The reference manifest could not be parsed.");

        return manifest.Fixtures
            .Select(f => new CompoundReferenceFixtureKat(
                f.RelativePath,
                f.SectorSize,
                f.RootClsid ?? string.Empty,
                f.Entries.Select(e => new CompoundManifestEntry(e.Path, e.Type, e.Size, e.Sha256)).ToList()))
            .ToList();
    }

    /// <summary>
    /// The manifest document shape.
    /// </summary>
    private sealed class ManifestDto
    {
        /// <summary>
        /// Gets or sets the fixture rows.
        /// </summary>
        public List<FixtureDto> Fixtures { get; set; } = new();
    }

    /// <summary>
    /// A single fixture record in the manifest.
    /// </summary>
    private sealed class FixtureDto
    {
        /// <summary>
        /// Gets or sets the corpus-relative fixture path.
        /// </summary>
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the regular sector size in bytes.
        /// </summary>
        public int SectorSize { get; set; }

        /// <summary>
        /// Gets or sets the lowercase root-storage class identifier.
        /// </summary>
        public string? RootClsid { get; set; }

        /// <summary>
        /// Gets or sets the expected entries.
        /// </summary>
        public List<EntryDto> Entries { get; set; } = new();
    }

    /// <summary>
    /// A single entry record in the manifest.
    /// </summary>
    private sealed class EntryDto
    {
        /// <summary>
        /// Gets or sets the storage-qualified entry path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entry kind.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the stream size in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the lowercase hexadecimal SHA-256 of the stream bytes.
        /// </summary>
        public string? Sha256 { get; set; }
    }
}
