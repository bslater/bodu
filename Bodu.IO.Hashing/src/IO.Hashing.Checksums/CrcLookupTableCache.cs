// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableCache.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Thread-safe cache of precomputed CRC lookup tables keyed by width, polynomial, and input reflection — amortizes the
/// per-tuple build cost of <see cref="CrcLookupTableBuilder.BuildLookupTable(int, ulong, bool)" /> across every
/// <see cref="Crc" /> instance that uses the same <see cref="CrcStandard" />.
/// </summary>
/// <remarks>
/// <para>
/// Building a CRC lookup table is cheap in absolute terms but not free, and the same table is needed every time a
/// <see cref="Crc" /> instance is constructed for a given <see cref="CrcStandard" />.
/// <see cref="CrcLookupTableCache" /> memoizes tables under the unique key <c>(width, polynomial, reflectIn)</c>: the
/// first lookup for a tuple builds and stores the table; subsequent lookups return the same shared array.
/// </para>
/// <para>
/// <strong>Default and custom caches.</strong> A process-wide default lives at <see cref="Crc.GlobalCache" /> and is
/// used implicitly by every <see cref="Crc" /> instance — most callers never need to construct a cache by hand. Reach
/// for a custom <see cref="CrcLookupTableCache" /> only when you want a scoped cache (e.g. per AppDomain, per test
/// fixture) that the global default should not see, or when running diagnostic code that needs deterministic cache
/// state. Custom caches can be installed by assigning to <see cref="Crc.GlobalCache" />.
/// </para>
/// <para>
/// <strong>Thread safety and aliasing.</strong> Internally backed by <see cref="ConcurrentDictionary{TKey, TValue}" />;
/// concurrent <see cref="GetLookupTable(int, ulong, bool)" /> calls are safe and the build delegate runs at most once
/// per key. The returned array is <strong>shared across all callers</strong> with the same parameters and must be
/// treated as read-only — mutating it would corrupt every <see cref="Crc" /> instance that uses the same standard.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
///
/// Most callers do not interact with the cache directly — Crc resolves it through Crc.GlobalCache.
/// var crc = new Crc(CrcStandard.CRC32_ISOHDLC);
///
/// Use a scoped cache for an isolated test, then restore the default.
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
///]]>
/// </code>
/// </example>
/// <seealso cref="Crc"/> <seealso cref="CrcStandard"/> <seealso cref="CrcLookupTableBuilder"/>
public class CrcLookupTableCache
{
    private readonly ConcurrentDictionary<CrcLookupKey, ulong[]> _localCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrcLookupTableCache" /> class.
    /// </summary>
    public CrcLookupTableCache()
    {
        _localCache = new ConcurrentDictionary<CrcLookupKey, ulong[]>();
    }

    /// <summary>
    /// Composite key identifying a CRC lookup-table tuple by width, polynomial, and input-reflection flag. Used as the
    /// dictionary key inside <see cref="CrcLookupTableCache" /> so lookups avoid the string-formatting and
    /// string-hashing cost that an interpolated key would incur on every call.
    /// </summary>
    /// <param name="Size">The CRC width in bits.</param>
    /// <param name="Polynomial">The CRC polynomial.</param>
    /// <param name="ReflectIn"><see langword="true" /> if input bytes are reflected during CRC processing.</param>
    private readonly record struct CrcLookupKey(int Size, ulong Polynomial, bool ReflectIn);

    /// <summary>
    /// Returns the cached lookup table for the specified CRC parameters, building it on first access.
    /// </summary>
    /// <param name="size">
    /// The CRC width in bits (between <see cref="CrcStandard.MinSize" /> and <see cref="CrcStandard.MaxSize" />).
    /// </param>
    /// <param name="polynomial">The CRC polynomial.</param>
    /// <param name="reflectIn"><see langword="true" /> if input bytes are reflected during CRC processing.</param>
    /// <returns>
    /// A read-only view of the shared lookup table for the supplied parameter set. The view wraps the cached array
    /// directly, so successive calls for the same key observe the same backing storage without further allocation.
    /// </returns>
    /// <remarks>
    /// The returned view is shared across all callers with the same parameters. Returning
    /// <see cref="ReadOnlyMemory{T}" /> prevents the caller from mutating the shared backing array at compile time;
    /// internal callers that require a raw <see cref="ulong" /> array can use <see cref="GetLookupTableArray" />.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size" /> is outside the supported range.
    /// </exception>
    public ReadOnlyMemory<ulong> GetLookupTable(int size, ulong polynomial, bool reflectIn) =>
        GetLookupTableArray(size, polynomial, reflectIn);

    /// <summary>
    /// Returns the cached lookup table for the specified CRC parameters as the raw <see cref="ulong" /> array used by
    /// the engine's hot path, building it on first access.
    /// </summary>
    /// <param name="size">
    /// The CRC width in bits (between <see cref="CrcStandard.MinSize" /> and <see cref="CrcStandard.MaxSize" />).
    /// </param>
    /// <param name="polynomial">The CRC polynomial.</param>
    /// <param name="reflectIn"><see langword="true" /> if input bytes are reflected during CRC processing.</param>
    /// <returns>The shared lookup table array for the supplied parameter set.</returns>
    /// <remarks>
    /// Restricted to internal callers so that the assembly's CRC engine can address the table directly without
    /// allocating a <see cref="ReadOnlyMemory{T}" /> wrapper on every byte of input. External callers must use the
    /// public <see cref="GetLookupTable" /> overload, which returns a read-only view of the same array.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="size" /> is outside the supported range.
    /// </exception>
    internal ulong[] GetLookupTableArray(int size, ulong polynomial, bool reflectIn)
    {
        ThrowHelper.ThrowIfOutOfRange(size, CrcStandard.MinSize, CrcStandard.MaxSize);

        var cacheKey = new CrcLookupKey(size, polynomial, reflectIn);
        return _localCache.GetOrAdd(cacheKey, static (key) =>
            CrcLookupTableBuilder.BuildLookupTable(key.Size, key.Polynomial, key.ReflectIn));
    }
}
