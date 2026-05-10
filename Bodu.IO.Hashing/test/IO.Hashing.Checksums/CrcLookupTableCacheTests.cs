// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableCacheTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for <see cref="CrcLookupTableCache" /> covering correctness, identity, parameter
/// validation, and concurrent access of the cached precomputed CRC tables.
/// </summary>
[TestClass]
public partial class CrcLookupTableCacheTests
{
    private CrcLookupTableCache cache = null!;

    [TestInitialize]
    public void SetUp()
    {
        cache = new CrcLookupTableCache();
    }
}
