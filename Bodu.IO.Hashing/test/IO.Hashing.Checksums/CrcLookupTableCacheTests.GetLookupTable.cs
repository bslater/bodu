// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableCacheTests.GetLookupTable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcLookupTableCacheTests
{

    /// <summary>
    /// Verifies that varying any single keying parameter (<c>size</c>, <c>polynomial</c>, or <c>reflectIn</c>)
    /// produces a distinct cached table, so cache hits are scoped strictly to the full key tuple.
    /// </summary>
    [TestMethod]
    public void GetLookupTable_WhenAnyParameterChanges_ShouldReturnDistinctTable()
    {
        var baseline = _cache.GetLookupTable(32, 0x04C11DB7UL, reflectIn: true).ToArray();

        CollectionAssert.AreNotEqual(baseline, _cache.GetLookupTable(16, 0x04C11DB7UL, reflectIn: true).ToArray());
        CollectionAssert.AreNotEqual(baseline, _cache.GetLookupTable(32, 0x1EDC6F41UL, reflectIn: true).ToArray());
        CollectionAssert.AreNotEqual(baseline, _cache.GetLookupTable(32, 0x04C11DB7UL, reflectIn: false).ToArray());
    }

    /// <summary>
    /// Verifies that concurrent lookups for the same parameters all succeed and return non-empty tables,
    /// exercising the cache's thread-safety contract under contention.
    /// </summary>
    [TestMethod]
    public void GetLookupTable_WhenCalledConcurrently_ShouldHandleConcurrentAccess()
    {
        var threads = new Task[100];
        var lengths = new int[100];

        for (var i = 0; i < 100; i++)
        {
            var threadIndex = i;
            threads[i] = Task.Run(() =>
            {
                ReadOnlyMemory<ulong> result = _cache.GetLookupTable(32, 0x04C11DB7UL, reflectIn: true);
                lengths[threadIndex] = result.Length;
            });
        }

        Task.WhenAll(threads).Wait();

        foreach (var length in lengths)
            Assert.AreEqual(256, length);
    }

    /// <summary>
    /// Verifies that repeated lookups with identical parameters return content-equivalent read-only views and
    /// observe the same underlying cached array, so callers see the precomputed table without paying for repeated
    /// allocation or initialisation.
    /// </summary>
    [TestMethod]
    public void GetLookupTable_WhenCalledWithSameParameters_ShouldReturnCachedInstance()
    {
        ReadOnlyMemory<ulong> first = _cache.GetLookupTable(32, 0x04C11DB7UL, reflectIn: true);
        ReadOnlyMemory<ulong> second = _cache.GetLookupTable(32, 0x04C11DB7UL, reflectIn: true);

        Assert.IsTrue(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(first, out ArraySegment<ulong> firstSegment));
        Assert.IsTrue(System.Runtime.InteropServices.MemoryMarshal.TryGetArray(second, out ArraySegment<ulong> secondSegment));
        Assert.AreSame(firstSegment.Array, secondSegment.Array);
    }

    /// <summary>
    /// Verifies that mutating a copy of a cached lookup table does not affect subsequent lookups returned by
    /// the cache for the same parameters.
    /// </summary>
    [TestMethod]
    public void GetLookupTable_WhenModified_ShouldNotModifyCachedArrays()
    {
        var copy = _cache.GetLookupTable(32, 0x04C11DB7UL, reflectIn: true).ToArray();
        copy[0] = 123456;

        ReadOnlyMemory<ulong> cached = _cache.GetLookupTable(32, 0x04C11DB7UL, reflectIn: true);
        Assert.AreNotEqual(123456UL, cached.Span[0]);
    }
    /// <summary>
    /// Verifies that <see cref="CrcLookupTableCache.GetLookupTable" /> returns a table whose length is
    /// <c>2^min(size, 8)</c> for every valid combination of <paramref name="size" />, <paramref name="polynomial" />,
    /// and <paramref name="reflectIn" /> — covering the inclusive boundaries (1-bit and 64-bit), the common
    /// 16/32-bit widths, and the polynomial edge values (zero and all-ones).
    /// </summary>
    /// <param name="size">The CRC width in bits.</param>
    /// <param name="polynomial">The CRC polynomial.</param>
    /// <param name="reflectIn">Whether input bytes are bit-reflected.</param>
    /// <param name="expectedLength">The expected lookup-table length.</param>
    [TestMethod]
    [DataRow(1, 0x1UL, false, 2)]
    [DataRow(1, 0x04C11DB7UL, true, 2)]
    [DataRow(16, 0x1EDC6F41UL, false, 256)]
    [DataRow(32, 0x04C11DB7UL, true, 256)]
    [DataRow(32, 0x0UL, true, 256)]
    [DataRow(32, 0xFFFFFFFFUL, true, 256)]
    [DataRow(64, 0x04C11DB7UL, false, 256)]
    [DataRow(64, 0x1UL, false, 256)]
    public void GetLookupTable_WhenParametersAreValid_ShouldReturnTableOfExpectedLength(
        int size, ulong polynomial, bool reflectIn, int expectedLength)
    {
        ReadOnlyMemory<ulong> table = _cache.GetLookupTable(size, polynomial, reflectIn);

        Assert.AreEqual(expectedLength, table.Length);
    }

    /// <summary>
    /// Verifies that invalid <paramref name="size" /> values surface an <see cref="ArgumentOutOfRangeException" />
    /// whose <c>ParamName</c> is <c>size</c>, confirming the <see cref="ThrowHelper" /> contract propagates
    /// through the cache lookup path.
    /// </summary>
    /// <param name="size">The invalid CRC width under test.</param>
    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(65)]
    [DataRow(128)]
    [DataRow(int.MaxValue)]
    public void GetLookupTable_WhenSizeIsInvalid_ShouldReportSizeParamName(int size)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => _cache.GetLookupTable(size, 0x04C11DB7UL, reflectIn: false));
        Assert.AreEqual("size", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the public <see cref="CrcLookupTableCache.GetLookupTable" /> return type is
    /// <see cref="ReadOnlyMemory{T}" /> of <see cref="ulong" />, so callers receive an immutable view of the
    /// shared cached table rather than a mutable array reference.
    /// </summary>
    [TestMethod]
    public void GetLookupTable_ReturnType_ShouldBeReadOnlyMemoryOfUlong()
    {
        System.Reflection.MethodInfo? method = typeof(CrcLookupTableCache)
            .GetMethod(nameof(CrcLookupTableCache.GetLookupTable), [typeof(int), typeof(ulong), typeof(bool)]);

        Assert.IsNotNull(method);
        Assert.AreEqual(typeof(ReadOnlyMemory<ulong>), method!.ReturnType);
    }

}
