// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableCache.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

using System.Collections.Concurrent;

/// <summary>
/// Thread-safe cache of precomputed CRC lookup tables keyed by width, polynomial, and input reflection — amortises
/// the per-tuple build cost of <see cref="CrcLookupTableBuilder.BuildLookupTable(int, ulong, bool)"/> across every
/// <see cref="Crc"/> instance that uses the same <see cref="CrcStandard"/>.
/// </summary>
/// <remarks>
/// <para>
/// Building a CRC lookup table is cheap in absolute terms but not free, and the same table is needed every time a
/// <see cref="Crc"/> instance is constructed for a given <see cref="CrcStandard"/>. <see cref="CrcLookupTableCache"/>
/// memoises tables under the unique key <c>(width, polynomial, reflectIn)</c>: the first lookup for a tuple builds and
/// stores the table; subsequent lookups return the same shared array.
/// </para>
/// <para>
/// <strong>Default and custom caches.</strong> A process-wide default lives at <see cref="Crc.GlobalCache"/> and is
/// used implicitly by every <see cref="Crc"/> instance — most callers never need to construct a cache by hand. Reach
/// for a custom <see cref="CrcLookupTableCache"/> only when you want a scoped cache (e.g. per AppDomain, per test
/// fixture) that the global default should not see, or when running diagnostic code that needs deterministic cache
/// state. Custom caches can be installed by assigning to <see cref="Crc.GlobalCache"/>.
/// </para>
/// <para>
/// <strong>Thread safety and aliasing.</strong> Internally backed by <see cref="ConcurrentDictionary{TKey, TValue}"/>;
/// concurrent <see cref="GetLookupTable(int, ulong, bool)"/> calls are safe and the build delegate runs at most once
/// per key. The returned array is <strong>shared across all callers</strong> with the same parameters and must be
/// treated as read-only — mutating it would corrupt every <see cref="Crc"/> instance that uses the same standard.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using Bodu.IO.Hashing.Checksums;
///
/// // Most callers do not interact with the cache directly — Crc resolves it through Crc.GlobalCache.
/// var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
///
/// // Use a scoped cache for an isolated test, then restore the default.
/// CrcLookupTableCache previous = Crc.GlobalCache;
/// try
/// {
///     Crc.GlobalCache = new CrcLookupTableCache();
///     // ... run isolated tests against a fresh cache ...
/// }
/// finally
/// {
///     Crc.GlobalCache = previous;
/// }
/// </code>
/// </example>
/// <seealso cref="Crc"/>
/// <seealso cref="CrcStandard"/>
/// <seealso cref="CrcLookupTableBuilder"/>
public class CrcLookupTableCache
{
    private readonly ConcurrentDictionary<string, ulong[]> localCache;

    /// <summary>
    /// Initialises a new, empty <see cref="CrcLookupTableCache" />.
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
        return this.localCache.GetOrAdd(cacheKey, _ => CrcLookupTableBuilder.BuildLookupTable(size, polynomial, reflectIn));
    }
}
