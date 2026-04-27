// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmRegistry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides a thread-safe, mutable in-memory implementation of <see cref="INotableDateAlgorithmRegistry" />.
/// </summary>
/// <remarks>
/// <para>
/// Algorithms registered with <see cref="Register(string, INotableDateAlgorithm)" /> are looked up case-insensitively by key. The
/// registry intentionally exposes registration as a constructor-time concern; consumers wiring up dependency injection should populate
/// the registry once during start-up and pass it to <see cref="NotableDateService" />.
/// </para>
/// </remarks>
public sealed class NotableDateAlgorithmRegistry : INotableDateAlgorithmRegistry
{
	private readonly Dictionary<string, INotableDateAlgorithm> _algorithms = new(StringComparer.OrdinalIgnoreCase);
	private readonly object _gate = new();

	/// <summary>
	/// Initialises a new, empty <see cref="NotableDateAlgorithmRegistry" />.
	/// </summary>
	public NotableDateAlgorithmRegistry() { }

	/// <summary>
	/// Initialises a new <see cref="NotableDateAlgorithmRegistry" /> seeded with the supplied algorithms.
	/// </summary>
	/// <param name="algorithms">The key/algorithm pairs to seed into the registry. Must not be <see langword="null" />.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="algorithms" /> is <see langword="null" />.</exception>
	public NotableDateAlgorithmRegistry(IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> algorithms)
	{
		if (algorithms is null) throw new ArgumentNullException(nameof(algorithms));

		foreach (var pair in algorithms)
			Register(pair.Key, pair.Value);
	}

	/// <summary>
	/// Registers a algorithm against the specified key. Existing entries with the same key are replaced.
	/// </summary>
	/// <param name="key">A short stable identifier, for example <c>"easter-sunday"</c>. Must not be <see langword="null" /> or whitespace.</param>
	/// <param name="algorithm">The algorithm instance. Must not be <see langword="null" />.</param>
	/// <returns>The current registry, to allow fluent chaining.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="algorithm" /> is <see langword="null" />.</exception>
	public NotableDateAlgorithmRegistry Register(string key, INotableDateAlgorithm algorithm)
	{
		if (string.IsNullOrWhiteSpace(key))
			throw new ArgumentException(CalendarStrings.KeyNullOrWhiteSpace_ArgumentException, nameof(key));
		if (algorithm is null)
			throw new ArgumentNullException(nameof(algorithm));

		lock (_gate)
		{
			_algorithms[key] = algorithm;
		}

		return this;
	}

	/// <inheritdoc />
	public bool TryGet(string key, out INotableDateAlgorithm algorithm)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			algorithm = null!;
			return false;
		}

		lock (_gate)
		{
			return _algorithms.TryGetValue(key, out algorithm!);
		}
	}

	/// <inheritdoc />
	public bool Contains(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
			return false;

		lock (_gate)
		{
			return _algorithms.ContainsKey(key);
		}
	}
}
