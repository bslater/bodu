// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerRegistry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides a thread-safe, mutable in-memory implementation of <see cref="IAdjustmentHandlerRegistry" />.
/// </summary>
/// <remarks>
/// <para>
/// Handlers registered via <see cref="Register(string, IAdjustmentHandler)" /> are looked up case-insensitively by key during
/// observance-adjustment evaluation. The registry is intended as a composition-time concern: hosts populate it once during
/// start-up and pass it to <see cref="NotableDateService" /> via the <c>adjustmentHandlers</c> constructor parameter so that
/// <see cref="ObservanceAdjustment" /> entries declaring <see cref="AdjustmentTrigger.Custom" /> or
/// <see cref="AdjustmentAction.Custom" /> can resolve to a concrete <see cref="IAdjustmentHandler" /> implementation.
/// </para>
/// </remarks>
/// <example>
/// <para>Register a custom handler and wire it into a service:</para>
/// <code>
/// AdjustmentHandlerRegistry handlers = new AdjustmentHandlerRegistry()
///     .Register("conflict-avoidance", new ConflictAvoidanceHandler("Anzac Day"));
///
/// NotableDateService service = new NotableDateService(
///     ruleProviders: new[] { AustraliaCalendarData.CreateProvider() },
///     weekendDefinition: CalendarWeekendDefinition.SaturdaySunday,
///     adjustmentHandlers: handlers);
///
/// // A rule's adjustment can now reference the handler by key:
/// ObservanceAdjustment custom = new ObservanceAdjustment
/// {
///     Key = "avoid-anzac",
///     Trigger = AdjustmentTrigger.Custom,
///     Action = AdjustmentAction.Custom,
///     HandlerKey = "conflict-avoidance",
/// };
/// </code>
/// </example>
public sealed class AdjustmentHandlerRegistry : IAdjustmentHandlerRegistry
{
    /// <summary>The case-insensitive key-to-handler mapping maintained by this registry.</summary>
    private readonly Dictionary<string, IAdjustmentHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Lock protecting read-modify-write access to <see cref="_handlers" />.</summary>
    private readonly object _gate = new();

    /// <summary>
    /// Initializes a new, empty <see cref="AdjustmentHandlerRegistry" />.
    /// </summary>
    public AdjustmentHandlerRegistry() { }

    /// <summary>
    /// Initializes a new <see cref="AdjustmentHandlerRegistry" /> seeded with the supplied handlers.
    /// </summary>
    /// <param name="handlers">The key/handler pairs to register. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handlers" /> is <see langword="null" />.</exception>
    public AdjustmentHandlerRegistry(IEnumerable<KeyValuePair<string, IAdjustmentHandler>> handlers)
    {
        if (handlers is null) throw new ArgumentNullException(nameof(handlers));

        foreach (KeyValuePair<string, IAdjustmentHandler> pair in handlers)
            Register(pair.Key, pair.Value);
    }

    /// <summary>
    /// Registers a handler against the specified key.
    /// </summary>
    /// <param name="key">The handler key. Must not be <see langword="null" /> or whitespace.</param>
    /// <param name="handler">The handler instance. Must not be <see langword="null" />.</param>
    /// <returns>The current registry, for fluent chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="handler" /> is <see langword="null" />.</exception>
    public AdjustmentHandlerRegistry Register(string key, IAdjustmentHandler handler)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(CalendarResourceStrings.ArgumentException_KeyNullOrWhiteSpace, nameof(key));
        if (handler is null)
            throw new ArgumentNullException(nameof(handler));

        lock (_gate)
        {
            _handlers[key] = handler;
        }

        return this;
    }

    /// <inheritdoc />
    public bool TryGet(string key, out IAdjustmentHandler handler)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            handler = null!;
            return false;
        }

        lock (_gate)
        {
            return _handlers.TryGetValue(key, out handler!);
        }
    }
}
