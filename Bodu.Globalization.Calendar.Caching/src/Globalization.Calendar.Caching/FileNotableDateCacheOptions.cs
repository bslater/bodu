// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileNotableDateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// The options for a file-backed <see cref="INotableDateCache" />, adding the storage directory to the storage-agnostic
/// base.
/// </summary>
public sealed class FileNotableDateCacheOptions
    : NotableDateCacheOptions
{
    /// <summary>
    /// Gets or sets the directory used by the on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-notable-dates</c> folder under the system
    /// temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }
}
