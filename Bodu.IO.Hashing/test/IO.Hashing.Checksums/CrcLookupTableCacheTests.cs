// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableCacheTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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

    private CrcLookupTableCache _cache = null!;

    [TestInitialize]
    public void SetUp() =>
        _cache = new CrcLookupTableCache();

}
