// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> that persists computed years as JSON files, one file per territory.
/// </summary>
/// <remarks>
/// Structurally identical to <see cref="TomlNotableDateCache" /> but serialized with <see cref="System.Text.Json" />:
/// the territory, an <c>Entries</c> array of per-year metadata, and a flat <c>Occurrences</c> array. Malformed content
/// is treated as an empty result, and all file-level resilience is provided by <see cref="FileNotableDateCacheBase" />.
/// </remarks>
public sealed class JsonNotableDateCache
    : FileNotableDateCacheBase
{
    /// <summary>The write-side serializer options: the shared web defaults plus indentation for human-readable on-disk files. Reads use the shared <see cref="NotableDateCacheFileConverter.JsonOptions" /> directly, since indentation does not affect parsing.</summary>
    private static readonly JsonSerializerOptions s_writeOptions = new(NotableDateCacheFileConverter.JsonOptions)
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonNotableDateCache" /> class.
    /// </summary>
    /// <param name="options">The file-cache options that select the storage directory.</param>
    /// <param name="timeProvider">
    /// The time source the swallowed-failure warning rate-limiting is measured against, or <see langword="null" /> to
    /// use <see cref="TimeProvider.System" />.
    /// </param>
    /// <param name="logger">
    /// The logger that receives a rate-limited warning when a best-effort storage failure is swallowed, or
    /// <see langword="null" /> to disable that reporting.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public JsonNotableDateCache(FileNotableDateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
        : base(options, timeProvider, logger)
    {
    }

    /// <inheritdoc />
    protected override string FileExtension => ".json";

    /// <inheritdoc />
    private protected override string Serialize(string territory, IReadOnlyList<NotableDateCacheEntry> entries) =>
        JsonSerializer.Serialize(NotableDateCacheFileConverter.ToFile(territory, entries), s_writeOptions);

    /// <inheritdoc />
    private protected override IReadOnlyList<NotableDateCacheEntry> Deserialize(string text, string path)
    {
        try
        {
            NotableDateCacheFile? file = JsonSerializer.Deserialize<NotableDateCacheFile>(text, NotableDateCacheFileConverter.JsonOptions);
            return file is null ? Array.Empty<NotableDateCacheEntry>() : NotableDateCacheFileConverter.ToEntries(file);
        }
        catch (JsonException ex)
        {
            OnCacheFileCorrupt(path, ex);
            return Array.Empty<NotableDateCacheEntry>();
        }
    }
}
