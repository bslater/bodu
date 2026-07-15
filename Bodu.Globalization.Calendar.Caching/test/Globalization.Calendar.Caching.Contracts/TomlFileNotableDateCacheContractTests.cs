// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileNotableDateCacheContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Caching;

namespace Bodu.Globalization.Calendar.Caching.Contracts;

/// <summary>
/// Runs the <see cref="NotableDateCacheContractTests" /> against <see cref="TomlNotableDateCache" />.
/// </summary>
[TestClass]
public sealed class TomlFileNotableDateCacheContractTests : FileNotableDateCacheContractTestsBase
{
    /// <inheritdoc />
    protected override INotableDateCache CreateCache(FileNotableDateCacheOptions options) => new TomlNotableDateCache(options);
}
