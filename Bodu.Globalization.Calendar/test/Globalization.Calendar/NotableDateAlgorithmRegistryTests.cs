// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmRegistryTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies registration, lookup, and containment checks for
/// <see cref="NotableDateAlgorithmRegistry" />.
/// </summary>
[TestClass]
public sealed class NotableDateAlgorithmRegistryTests
{
    /// <summary>
    /// Verifies that the parameterless constructor yields an empty, functional registry.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldReturnEmptyRegistry()
    {
        var registry = new NotableDateAlgorithmRegistry();

        Assert.IsFalse(registry.Contains("any"));
        Assert.IsFalse(registry.TryGet("any", out var algorithm));
        Assert.IsNull(algorithm);
    }

    /// <summary>
    /// Verifies that the seeded constructor registers every supplied pair.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSeeded_ShouldRegisterAllSuppliedAlgorithms()
    {
        var easter = new StaticAlgorithm(new DateTime(2026, 4, 5));
        var lunar = new StaticAlgorithm(new DateTime(2026, 2, 17));

        var registry = new NotableDateAlgorithmRegistry(
        [
            new KeyValuePair<string, INotableDateAlgorithm>("easter", easter),
            new KeyValuePair<string, INotableDateAlgorithm>("lunar", lunar),
        ]);

        Assert.IsTrue(registry.TryGet("easter", out var resolvedEaster));
        Assert.AreSame(easter, resolvedEaster);
        Assert.IsTrue(registry.TryGet("lunar", out var resolvedLunar));
        Assert.AreSame(lunar, resolvedLunar);
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> seed collection throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAlgorithmsIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NotableDateAlgorithmRegistry(null!);
        });

        Assert.AreEqual("algorithms", ex.ParamName);
    }

    /// <summary>
    /// Verifies that registering with a null, empty, or whitespace key throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [DataRow(null!)]
    [DataRow("")]
    [DataRow("   ")]
    [TestMethod]
    public void Register_WhenKeyIsNullOrWhitespace_ShouldThrowExactly(string? key)
    {
        var registry = new NotableDateAlgorithmRegistry();

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = registry.Register(key!, new StaticAlgorithm(DateTime.Today));
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that registering a <see langword="null" /> algorithm throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenAlgorithmIsNull_ShouldThrowExactly()
    {
        var registry = new NotableDateAlgorithmRegistry();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = registry.Register("key", null!);
        });

        Assert.AreEqual("algorithm", ex.ParamName);
    }

    /// <summary>
    /// Verifies that lookups and containment checks are case-insensitive.
    /// </summary>
    [DataRow("Key")]
    [DataRow("KEY")]
    [DataRow("key")]
    [DataRow("kEy")]
    [TestMethod]
    public void LookupsAreCaseInsensitive(string lookupKey)
    {
        var registry = new NotableDateAlgorithmRegistry();
        var algorithm = new StaticAlgorithm(new DateTime(2026, 1, 1));
        registry.Register("Key", algorithm);

        Assert.IsTrue(registry.Contains(lookupKey));
        Assert.IsTrue(registry.TryGet(lookupKey, out var resolved));
        Assert.AreSame(algorithm, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateAlgorithmRegistry.Contains(string)" /> and
    /// <see cref="NotableDateAlgorithmRegistry.TryGet(string, out INotableDateAlgorithm)" />
    /// return <see langword="false" /> for null, empty, or whitespace keys.
    /// </summary>
    [DataRow(null!)]
    [DataRow("")]
    [DataRow("  ")]
    [TestMethod]
    public void Lookup_WhenKeyIsNullOrWhitespace_ShouldReturnFalse(string? key)
    {
        var registry = new NotableDateAlgorithmRegistry();
        registry.Register("actual", new StaticAlgorithm(DateTime.Today));

        Assert.IsFalse(registry.Contains(key!));
        Assert.IsFalse(registry.TryGet(key!, out var algorithm));
        Assert.IsNull(algorithm);
    }

    /// <summary>
    /// Verifies that registering under an existing key replaces the previous algorithm.
    /// </summary>
    [TestMethod]
    public void Register_WhenKeyAlreadyRegistered_ShouldReplaceAlgorithm()
    {
        var registry = new NotableDateAlgorithmRegistry();
        var original = new StaticAlgorithm(new DateTime(2020, 1, 1));
        var replacement = new StaticAlgorithm(new DateTime(2030, 1, 1));

        registry.Register("key", original).Register("key", replacement);

        Assert.IsTrue(registry.TryGet("key", out var resolved));
        Assert.AreSame(replacement, resolved);
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateAlgorithmRegistry.Register(string, INotableDateAlgorithm)" />
    /// returns the registry so callers can chain fluently.
    /// </summary>
    [TestMethod]
    public void Register_WhenCalled_ShouldReturnSameRegistryForChaining()
    {
        var registry = new NotableDateAlgorithmRegistry();

        NotableDateAlgorithmRegistry returned = registry.Register("key", new StaticAlgorithm(DateTime.Today));

        Assert.AreSame(registry, returned);
    }

    /// <summary>
    /// Minimal <see cref="INotableDateAlgorithm" /> test double returning a fixed date.
    /// </summary>
    private sealed class StaticAlgorithm
        : INotableDateAlgorithm
    {
        private readonly DateTime _date;

        public StaticAlgorithm(DateTime date) => _date = date;

        public DateTime? GetDate(int year, SysGlob.Calendar? calendar = null) => _date;
    }
}
