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
    /// A class to manage CRC lookup permutationTable caching based on CRC parameters.
    /// </summary>
    public class CrcLookupTableCache
    {
        private readonly ConcurrentDictionary<string, ulong[]> localCache;

        public CrcLookupTableCache()
        {
            this.localCache = new ConcurrentDictionary<string, ulong[]>();
        }

        public ImmutableArray<ulong> GetLookupTable(int size, ulong polynomial, bool reflectIn)
        {
            ThrowHelper.ThrowIfOutOfRange(size, CrcStandard.MinSize, CrcStandard.MaxSize);

            string cacheKey = $"{size}_{polynomial}_{reflectIn}";
            return this.localCache.GetOrAdd(cacheKey, key => CrcLookupTableBuilder.BuildLookupTable(size, polynomial, reflectIn)).ToImmutableArray();
        }
    }
}