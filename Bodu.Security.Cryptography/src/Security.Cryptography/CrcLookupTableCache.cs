// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Thread-safe cache of precomputed CRC lookup tables keyed by width, polynomial, and input reflection.
    /// </summary>
    public class CrcLookupTableCache
    {
        private readonly ConcurrentDictionary<string, ulong[]> localCache;

        /// <summary>
        /// Initializes a new, empty <see cref="CrcLookupTableCache" />.
        /// </summary>
        public CrcLookupTableCache()
        {
            this.localCache = new ConcurrentDictionary<string, ulong[]>();
        }

        /// <summary>
        /// Returns the cached lookup table for the specified CRC parameters, building it on first access.
        /// </summary>
        /// <param name="size">The CRC width in bits (between <see cref="CrcStandard.MinSize" /> and <see cref="CrcStandard.MaxSize" />).</param>
        /// <param name="polynomial">The CRC polynomial.</param>
        /// <param name="reflectIn"><see langword="true" /> if input bytes are reflected during CRC processing.</param>
        /// <returns>An immutable view over the cached lookup table.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size" /> is outside the supported range.</exception>
        public ImmutableArray<ulong> GetLookupTable(int size, ulong polynomial, bool reflectIn)
        {
            ThrowHelper.ThrowIfOutOfRange(size, CrcStandard.MinSize, CrcStandard.MaxSize);

            string cacheKey = $"{size}_{polynomial}_{reflectIn}";
            return this.localCache.GetOrAdd(cacheKey, key => CrcLookupTableBuilder.BuildLookupTable(size, polynomial, reflectIn)).ToImmutableArray();
        }
    }
}