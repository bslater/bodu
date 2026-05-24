// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationPatternTests.Caching.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public partial class ConfigurationPatternTests
{
    /// <summary>
    /// Verifies that compiling the same pattern under the same comparison twice returns the cached instance,
    /// so callers do not pay regex-compilation cost on repeat lookups.
    /// </summary>
    [TestMethod]
    public void Compile_WhenSamePatternAndComparison_ShouldReturnCachedInstance()
    {
        ConfigurationPattern first = ConfigurationPattern.Compile("**/*.cs", StringComparison.Ordinal);
        ConfigurationPattern second = ConfigurationPattern.Compile("**/*.cs", StringComparison.Ordinal);

        Assert.AreSame(first, second);
    }

    /// <summary>
    /// Verifies that the cache key includes the comparison, so the same source pattern compiled under
    /// different comparisons returns distinct instances rather than colliding.
    /// </summary>
    [TestMethod]
    public void Compile_WhenSamePatternDifferentComparison_ShouldReturnDistinctInstances()
    {
        ConfigurationPattern ordinal = ConfigurationPattern.Compile("**/*.cs", StringComparison.Ordinal);
        ConfigurationPattern caseInsensitive = ConfigurationPattern.Compile("**/*.cs", StringComparison.OrdinalIgnoreCase);

        Assert.AreNotSame(ordinal, caseInsensitive);
    }

    /// <summary>
    /// Verifies that the parameterless overload defaults to <see cref="StringComparison.Ordinal" /> and
    /// shares its cache slot with the explicit <see cref="StringComparison.Ordinal" /> overload.
    /// </summary>
    [TestMethod]
    public void Compile_WhenParameterlessOverload_ShouldShareOrdinalCacheSlot()
    {
        ConfigurationPattern viaDefault = ConfigurationPattern.Compile("logs/run-{1..3}.log");
        ConfigurationPattern viaExplicit = ConfigurationPattern.Compile("logs/run-{1..3}.log", StringComparison.Ordinal);

        Assert.AreSame(viaDefault, viaExplicit);
    }
}
