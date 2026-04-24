// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableCache.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Thread-safe cache of precomputed CRC lookup tables keyed by width, polynomial, and input reflection.
/// </summary>
public class CrcLookupTableCache
{
    private readonly ConcurrentDictionary<string, ulong[]> localCache;

    /// <summary>
    /// Initialises a new, empty <see cref="CrcLookupTableCache" />.
    /// </summary>
    public CrcLookupTableCache()
    {
        localCache = new ConcurrentDictionary<string, ulong[]>();
    }

    /// <summary>
    /// Returns the cached lookup table for the specified CRC parameters, building it on first access.
    /// </summary>
    /// <param name="size">The CRC width in bits (between <see cref="CrcStandard.MinSize" /> and <see cref="CrcStandard.MaxSize" />).</param>
    /// <param name="polynomial">The CRC polynomial.</param>
    /// <param name="reflectIn"><see langword="true" /> if input bytes are reflected during CRC processing.</param>
    /// <returns>The shared lookup table array for the supplied parameter set.</returns>
    /// <remarks>
    /// The returned array is shared across all callers with the same parameters and <b>must not</b> be mutated. Callers should treat it
    /// as read-only.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size" /> is outside the supported range.</exception>
    public ulong[] GetLookupTable(int size, ulong polynomial, bool reflectIn)
    {
        ThrowHelper.ThrowIfOutOfRange(size, CrcStandard.MinSize, CrcStandard.MaxSize);

        string cacheKey = $"{size}_{polynomial}_{reflectIn}";
        return localCache.GetOrAdd(cacheKey, _ => CrcLookupTableBuilder.BuildLookupTable(size, polynomial, reflectIn));
    }
}
