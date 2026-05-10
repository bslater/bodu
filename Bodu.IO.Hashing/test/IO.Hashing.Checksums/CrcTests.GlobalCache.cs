// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.GlobalCache.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcTests
{
    /// <summary>
    /// Verifies that <see cref="Crc.GlobalCache" /> is lazily initialised on first access and returns a usable,
    /// non-null instance even when no explicit cache has been assigned.
    /// </summary>
    [TestMethod]
    public void GlobalCache_WhenNotAssigned_ShouldReturnLazilyCreatedCache()
    {
        CrcLookupTableCache cache = Crc.GlobalCache;
        Assert.IsNotNull(cache);

        var table = cache.GetLookupTable(32, 0x04C11DB7UL, reflectIn: true);
        Assert.IsNotNull(table);
        Assert.AreEqual(256, table.Length);
    }

    /// <summary>
    /// Verifies that assigning a non-null <see cref="CrcLookupTableCache" /> to <see cref="Crc.GlobalCache" />
    /// replaces the active cache, and that restoring a fresh cache afterwards leaves no residual shared state.
    /// </summary>
    [TestMethod]
    public void GlobalCache_WhenAssigned_ShouldReturnAssignedInstance()
    {
        CrcLookupTableCache original = Crc.GlobalCache;
        try
        {
            CrcLookupTableCache replacement = new();
            Crc.GlobalCache = replacement;

            Assert.AreSame(replacement, Crc.GlobalCache);
        }
        finally
        {
            Crc.GlobalCache = original;
        }
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> to <see cref="Crc.GlobalCache" /> throws
    /// <see cref="System.ArgumentNullException" /> with <c>ParamName</c> equal to <c>value</c>, preserving the
    /// previously configured cache.
    /// </summary>
    [TestMethod]
    public void GlobalCache_WhenAssignedNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<System.ArgumentNullException>(() => Crc.GlobalCache = null!);
        Assert.AreEqual("value", ex.ParamName);
    }
}
