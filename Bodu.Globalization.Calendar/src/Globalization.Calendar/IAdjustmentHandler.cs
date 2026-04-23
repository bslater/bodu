// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAdjustmentHandler.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Implements custom evaluation and modification logic for an <see cref="ObservanceAdjustment" /> whose
/// <see cref="ObservanceAdjustment.Trigger" /> or <see cref="ObservanceAdjustment.Action" /> is set to
/// <see cref="AdjustmentTrigger.Custom" /> or <see cref="AdjustmentAction.Custom" />.
/// </summary>
/// <remarks>
/// <para>
/// Custom handlers are looked up by <see cref="ObservanceAdjustment.HandlerKey" /> from an <see cref="IAdjustmentHandlerRegistry" />.
/// They receive the resolved date plus the surrounding context and return whether the adjustment activates and, if so, the new date.
/// </para>
/// </remarks>
public interface IAdjustmentHandler
{
	/// <summary>
	/// Evaluates the adjustment and, if active, computes the new date.
	/// </summary>
	/// <param name="context">The current adjustment context, including the date being evaluated and surrounding metadata.</param>
	/// <returns>An <see cref="AdjustmentHandlerResult" /> describing whether the handler activated and what date it produced.</returns>
	AdjustmentHandlerResult Apply(AdjustmentHandlerContext context);
}

/// <summary>
/// Captures the inputs delivered to an <see cref="IAdjustmentHandler" />.
/// </summary>
/// <param name="Date">The currently resolved date being considered for adjustment.</param>
/// <param name="Adjustment">The adjustment specification that triggered the handler.</param>
/// <param name="Rule">The originating <see cref="NotableDateRule" />.</param>
/// <param name="TerritoryCode">The territory currently being resolved, if any.</param>
/// <param name="CalendarType">The calendar currently being resolved, if any.</param>
public sealed record AdjustmentHandlerContext(
	DateTime Date,
	ObservanceAdjustment Adjustment,
	NotableDateRule Rule,
	string? TerritoryCode,
	Type? CalendarType);

/// <summary>
/// Captures the output produced by an <see cref="IAdjustmentHandler" />.
/// </summary>
/// <param name="Activated">Whether the handler considered its trigger satisfied.</param>
/// <param name="AdjustedDate">The new date when <paramref name="Activated" /> is <see langword="true" />, otherwise the unchanged date.</param>
/// <param name="IsNonWorkingOverride">Optional override for the resulting date's non-working flag. <see langword="null" /> preserves the existing value.</param>
public sealed record AdjustmentHandlerResult(bool Activated, DateTime AdjustedDate, bool? IsNonWorkingOverride = null);

/// <summary>
/// Provides lookup of registered <see cref="IAdjustmentHandler" /> instances by stable key.
/// </summary>
public interface IAdjustmentHandlerRegistry
{
	/// <summary>
	/// Attempts to retrieve the <see cref="IAdjustmentHandler" /> registered against the specified key.
	/// </summary>
	/// <param name="key">The handler key.</param>
	/// <param name="handler">When this method returns <see langword="true" />, contains the resolved handler.</param>
	/// <returns><see langword="true" /> if a handler is registered for <paramref name="key" />; otherwise <see langword="false" />.</returns>
	bool TryGet(string key, out IAdjustmentHandler handler);
}

/// <summary>
/// Provides a thread-safe in-memory implementation of <see cref="IAdjustmentHandlerRegistry" />.
/// </summary>
public sealed class AdjustmentHandlerRegistry : IAdjustmentHandlerRegistry
{
	private readonly Dictionary<string, IAdjustmentHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
	private readonly object _gate = new();

	/// <summary>
	/// Initialises a new, empty <see cref="AdjustmentHandlerRegistry" />.
	/// </summary>
	public AdjustmentHandlerRegistry() { }

	/// <summary>
	/// Initialises a new <see cref="AdjustmentHandlerRegistry" /> seeded with the supplied handlers.
	/// </summary>
	/// <param name="handlers">The key/handler pairs to register. Must not be <see langword="null" />.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="handlers" /> is <see langword="null" />.</exception>
	public AdjustmentHandlerRegistry(IEnumerable<KeyValuePair<string, IAdjustmentHandler>> handlers)
	{
		if (handlers is null) throw new ArgumentNullException(nameof(handlers));

		foreach (var pair in handlers)
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
			throw new ArgumentException("Key must not be null or whitespace.", nameof(key));
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
