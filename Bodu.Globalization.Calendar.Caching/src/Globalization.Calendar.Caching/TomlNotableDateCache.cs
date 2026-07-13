// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNotableDateCache.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml;
using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// An <see cref="INotableDateCache" /> that persists computed years as TOML files, one file per territory.
/// </summary>
/// <remarks>
/// <para>
/// Each file records the territory as a top-level key, an array of tables under <c>Entries</c> — one table per cached
/// civil year with its version and computed instant — and an array of tables under <c>Occurrences</c>, one flat table
/// per emitted occurrence carrying its year and version so it associates back to its entry. Dates and instants use
/// TOML's native RFC 3339 forms.
/// </para>
/// <para>
/// Malformed content is treated as an empty result, and all file-level resilience — including atomic temp-and-move
/// writes and the parse memo — is provided by <see cref="FileNotableDateCacheBase" />.
/// </para>
/// </remarks>
public sealed class TomlNotableDateCache
    : FileNotableDateCacheBase
{
    /// <summary>The serializer options shared by every read and write.</summary>
    private static readonly TomlSerializerOptions s_tomlOptions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlNotableDateCache" /> class.
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
    public TomlNotableDateCache(FileNotableDateCacheOptions options, TimeProvider? timeProvider = null, ILogger? logger = null)
        : base(options, timeProvider, logger)
    {
    }

    /// <inheritdoc />
    protected override string FileExtension => ".toml";

    /// <inheritdoc />
    private protected override string Serialize(TerritoryCacheState state) =>
        TomlSerializer.Serialize(NotableDateCacheFileConverter.ToFile(state), s_tomlOptions);

    /// <inheritdoc />
    private protected override TerritoryCacheState Deserialize(string text, string path)
    {
        try
        {
            return NotableDateCacheFileConverter.ToState(TomlSerializer.Deserialize<NotableDateCacheFile>(text, s_tomlOptions));
        }
        catch (TomlFormatException ex)
        {
            OnCacheFileCorrupt(path, ex);
            return TerritoryCacheState.Empty;
        }
        catch (TomlSerializationException ex)
        {
            OnCacheFileCorrupt(path, ex);
            return TerritoryCacheState.Empty;
        }
    }
}
