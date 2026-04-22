namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Provides a thread-safe, mutable in-memory implementation of <see cref="INotableDateCalculatorRegistry" />.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Calculators registered with <see cref="Register(string, INotableDateCalculator)" /> are looked up case-insensitively by key. The
	/// registry intentionally exposes registration as a constructor-time concern; consumers wiring up dependency injection should populate
	/// the registry once during start-up and pass it to <see cref="NotableDateService" />.
	/// </para>
	/// </remarks>
	public sealed class NotableDateCalculatorRegistry : INotableDateCalculatorRegistry
	{
		private readonly Dictionary<string, INotableDateCalculator> _calculators = new(StringComparer.OrdinalIgnoreCase);
		private readonly object _gate = new();

		/// <summary>
		/// Initializes a new, empty <see cref="NotableDateCalculatorRegistry" />.
		/// </summary>
		public NotableDateCalculatorRegistry() { }

		/// <summary>
		/// Initializes a new <see cref="NotableDateCalculatorRegistry" /> seeded with the supplied calculators.
		/// </summary>
		/// <param name="calculators">The key/calculator pairs to seed into the registry. Must not be <see langword="null" />.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="calculators" /> is <see langword="null" />.</exception>
		public NotableDateCalculatorRegistry(IEnumerable<KeyValuePair<string, INotableDateCalculator>> calculators)
		{
			if (calculators is null) throw new ArgumentNullException(nameof(calculators));

			foreach (var pair in calculators)
				Register(pair.Key, pair.Value);
		}

		/// <summary>
		/// Registers a calculator against the specified key. Existing entries with the same key are replaced.
		/// </summary>
		/// <param name="key">A short stable identifier, for example <c>"easter-sunday"</c>. Must not be <see langword="null" /> or whitespace.</param>
		/// <param name="calculator">The calculator instance. Must not be <see langword="null" />.</param>
		/// <returns>The current registry, to allow fluent chaining.</returns>
		/// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="calculator" /> is <see langword="null" />.</exception>
		public NotableDateCalculatorRegistry Register(string key, INotableDateCalculator calculator)
		{
			if (string.IsNullOrWhiteSpace(key))
				throw new ArgumentException("Key must not be null or whitespace.", nameof(key));
			if (calculator is null)
				throw new ArgumentNullException(nameof(calculator));

			lock (_gate)
			{
				_calculators[key] = calculator;
			}

			return this;
		}

		/// <inheritdoc />
		public bool TryGet(string key, out INotableDateCalculator calculator)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				calculator = null!;
				return false;
			}

			lock (_gate)
			{
				return _calculators.TryGetValue(key, out calculator!);
			}
		}

		/// <inheritdoc />
		public bool Contains(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
				return false;

			lock (_gate)
			{
				return _calculators.ContainsKey(key);
			}
		}
	}
}
