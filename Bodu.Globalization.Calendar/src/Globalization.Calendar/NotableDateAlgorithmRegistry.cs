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
/// <para>
/// At resolution time the service consults this registry whenever a <see cref="NotableDateRule" /> declares
/// <see cref="DateResolutionStrategy.Algorithm" />. The looked-up <see cref="INotableDateAlgorithm" /> supplies the anchor date
/// only — every other field on the emitted <see cref="NotableDate" /> (name, category, tags, territory, calendar, non-working
/// flag, duration, observance adjustments) comes from the rule itself. This means a single registered algorithm can power any
/// number of rules: Easter Sunday, Easter Monday (offset +1 from Easter Sunday), Good Friday (offset −2), and Holy Saturday
/// (offset −1) typically all resolve through one registered <c>"easter-sunday"</c> algorithm.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Register two Easter algorithms and bind a rule to one by key. The resulting <see cref="NotableDate" /> carries the date
/// produced by the algorithm, but its name, category, and territory come from the rule:
/// </para>
/// <code>
/// // Compose the registry once at start-up:
/// NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
///     .Register("easter-sunday",   new GregorianEasterSundayNotableDateProvider())
///     .Register("orthodox-easter", new OrthodoxEasterSundayNotableDateProvider());
///
/// // Author a rule that resolves through the algorithm:
/// NotableDateRule easterSunday = new NotableDateRule
/// {
///     Name = "Easter Sunday",
///     Strategy = DateResolutionStrategy.Algorithm,
///     Category = NotableDateCategory.Religious,
///     AlgorithmKey = "easter-sunday",
///     Tags = ImmutableHashSet.Create("Christian", "Easter"),
/// };
///
/// // Wire registry into the service so DateResolutionStrategy.Algorithm rules can be resolved:
/// NotableDateService service = new NotableDateService(
///     ruleProviders: new[] { new InMemoryRuleProvider(new[] { easterSunday }) },
///     weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
///     options: new NotableDateServiceOptions { AlgorithmRegistry = registry });
/// </code>
/// </example>
public sealed class NotableDateAlgorithmRegistry : INotableDateAlgorithmRegistry
{
    /// <summary>The case-insensitive key-to-algorithm mapping maintained by this registry.</summary>
    private readonly Dictionary<string, INotableDateAlgorithm> _algorithms = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Lock protecting read-modify-write access to <see cref="_algorithms" />.</summary>
    private readonly object _gate = new();

    /// <summary>
    /// Initializes a new, empty <see cref="NotableDateAlgorithmRegistry" />.
    /// </summary>
    public NotableDateAlgorithmRegistry() { }

    /// <summary>
    /// Initializes a new <see cref="NotableDateAlgorithmRegistry" /> seeded with the supplied algorithms.
    /// </summary>
    /// <param name="algorithms">The key/algorithm pairs to seed into the registry. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="algorithms" /> is <see langword="null" />.</exception>
    public NotableDateAlgorithmRegistry(IEnumerable<KeyValuePair<string, INotableDateAlgorithm>> algorithms)
    {
        if (algorithms is null) throw new ArgumentNullException(nameof(algorithms));

        foreach (KeyValuePair<string, INotableDateAlgorithm> pair in algorithms)
            Register(pair.Key, pair.Value);
    }

    /// <summary>
    /// Registers an algorithm against the specified key. Existing entries with the same key are replaced.
    /// </summary>
    /// <param name="key">A short stable identifier, for example <c>"easter-sunday"</c>. Must not be <see langword="null" /> or whitespace.</param>
    /// <param name="algorithm">The algorithm instance. Must not be <see langword="null" />.</param>
    /// <returns>The current registry, to allow fluent chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="algorithm" /> is <see langword="null" />.</exception>
    public NotableDateAlgorithmRegistry Register(string key, INotableDateAlgorithm algorithm)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(CalendarResourceStrings.ArgumentException_KeyNullOrWhiteSpace, nameof(key));
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
