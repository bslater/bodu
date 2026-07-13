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
/// is treated as an empty result, and all file-level resilience is provided by
/// <see cref="FileNotableDateCacheBase" />.
/// </remarks>
public sealed class JsonNotableDateCache
    : FileNotableDateCacheBase
{
    /// <summary>The serializer options shared by every read and write; indented for human-readable on-disk files.</summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web)
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options" /> fails validation.</exception>
    public JsonNotableDateCache(FileNotableDateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
        : base(options, timeProvider, logger)
    {
    }

    /// <inheritdoc />
    protected override string FileExtension => ".json";

    /// <inheritdoc />
    private protected override string Serialize(TerritoryCacheState state) =>
        JsonSerializer.Serialize(NotableDateCacheFileConverter.ToFile(state), s_jsonOptions);

    /// <inheritdoc />
    private protected override TerritoryCacheState Deserialize(string text, string path)
    {
        try
        {
            NotableDateCacheFile? file = JsonSerializer.Deserialize<NotableDateCacheFile>(text, s_jsonOptions);
            return file is null ? TerritoryCacheState.Empty : NotableDateCacheFileConverter.ToState(file);
        }
        catch (JsonException ex)
        {
            OnCacheFileCorrupt(path, ex);
            return TerritoryCacheState.Empty;
        }
    }
}
