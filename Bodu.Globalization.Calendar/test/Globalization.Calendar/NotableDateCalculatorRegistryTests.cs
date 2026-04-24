// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCalculatorRegistryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies registration, lookup, and containment checks for
/// <see cref="NotableDateCalculatorRegistry" />.
/// </summary>
[TestClass]
public sealed class NotableDateCalculatorRegistryTests
{
	/// <summary>
	/// Verifies that the parameterless constructor yields an empty, functional registry.
	/// </summary>
	[TestMethod]
	public void Constructor_WhenParameterless_ShouldReturnEmptyRegistry()
	{
		var registry = new NotableDateCalculatorRegistry();

		Assert.IsFalse(registry.Contains("any"));
		Assert.IsFalse(registry.TryGet("any", out var calculator));
		Assert.IsNull(calculator);
	}

	/// <summary>
	/// Verifies that the seeded constructor registers every supplied pair.
	/// </summary>
	[TestMethod]
	public void Constructor_WhenSeeded_ShouldRegisterAllSuppliedCalculators()
	{
		var easter = new StaticCalculator(new DateTime(2026, 4, 5));
		var lunar = new StaticCalculator(new DateTime(2026, 2, 17));

		var registry = new NotableDateCalculatorRegistry(new[]
		{
			new KeyValuePair<string, INotableDateCalculator>("easter", easter),
			new KeyValuePair<string, INotableDateCalculator>("lunar", lunar),
		});

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
	public void Constructor_WhenCalculatorsIsNull_ShouldThrowArgumentNullException()
	{
		var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = new NotableDateCalculatorRegistry(null!);
		});

		Assert.AreEqual("calculators", ex.ParamName);
	}

	/// <summary>
	/// Verifies that registering with a null, empty, or whitespace key throws
	/// <see cref="ArgumentException" />.
	/// </summary>
	[DataRow(null!)]
	[DataRow("")]
	[DataRow("   ")]
	[TestMethod]
	public void Register_WhenKeyIsNullOrWhitespace_ShouldThrowArgumentException(string? key)
	{
		var registry = new NotableDateCalculatorRegistry();

		var ex = Assert.ThrowsExactly<ArgumentException>(() =>
		{
			_ = registry.Register(key!, new StaticCalculator(DateTime.Today));
		});

		Assert.AreEqual("key", ex.ParamName);
	}

	/// <summary>
	/// Verifies that registering a <see langword="null" /> calculator throws
	/// <see cref="ArgumentNullException" />.
	/// </summary>
	[TestMethod]
	public void Register_WhenCalculatorIsNull_ShouldThrowArgumentNullException()
	{
		var registry = new NotableDateCalculatorRegistry();

		var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = registry.Register("key", null!);
		});

		Assert.AreEqual("calculator", ex.ParamName);
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
		var registry = new NotableDateCalculatorRegistry();
		var calculator = new StaticCalculator(new DateTime(2026, 1, 1));
		registry.Register("Key", calculator);

		Assert.IsTrue(registry.Contains(lookupKey));
		Assert.IsTrue(registry.TryGet(lookupKey, out var resolved));
		Assert.AreSame(calculator, resolved);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateCalculatorRegistry.Contains(string)" /> and
	/// <see cref="NotableDateCalculatorRegistry.TryGet(string, out INotableDateCalculator)" />
	/// return <see langword="false" /> for null, empty, or whitespace keys.
	/// </summary>
	[DataRow(null!)]
	[DataRow("")]
	[DataRow("  ")]
	[TestMethod]
	public void Lookup_WhenKeyIsNullOrWhitespace_ShouldReturnFalse(string? key)
	{
		var registry = new NotableDateCalculatorRegistry();
		registry.Register("actual", new StaticCalculator(DateTime.Today));

		Assert.IsFalse(registry.Contains(key!));
		Assert.IsFalse(registry.TryGet(key!, out var calculator));
		Assert.IsNull(calculator);
	}

	/// <summary>
	/// Verifies that registering under an existing key replaces the previous calculator.
	/// </summary>
	[TestMethod]
	public void Register_WhenKeyAlreadyRegistered_ShouldReplaceCalculator()
	{
		var registry = new NotableDateCalculatorRegistry();
		var original = new StaticCalculator(new DateTime(2020, 1, 1));
		var replacement = new StaticCalculator(new DateTime(2030, 1, 1));

		registry.Register("key", original).Register("key", replacement);

		Assert.IsTrue(registry.TryGet("key", out var resolved));
		Assert.AreSame(replacement, resolved);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateCalculatorRegistry.Register(string, INotableDateCalculator)" />
	/// returns the registry so callers can chain fluently.
	/// </summary>
	[TestMethod]
	public void Register_WhenCalled_ShouldReturnSameRegistryForChaining()
	{
		var registry = new NotableDateCalculatorRegistry();

		NotableDateCalculatorRegistry returned = registry.Register("key", new StaticCalculator(DateTime.Today));

		Assert.AreSame(registry, returned);
	}

	/// <summary>
	/// Minimal <see cref="INotableDateCalculator" /> test double returning a fixed date.
	/// </summary>
	private sealed class StaticCalculator : INotableDateCalculator
	{
		private readonly DateTime _date;

		public StaticCalculator(DateTime date) => _date = date;

		public DateTime? GetDate(int year, SysGlob.Calendar? calendar = null) => _date;
	}
}
