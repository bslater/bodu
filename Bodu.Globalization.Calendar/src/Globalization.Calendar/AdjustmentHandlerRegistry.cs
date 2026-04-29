// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerRegistry.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Provides a thread-safe in-memory implementation of <see cref="IAdjustmentHandlerRegistry" />.
/// </summary>
public sealed class AdjustmentHandlerRegistry : IAdjustmentHandlerRegistry
{
	/// <summary>The case-insensitive key-to-handler mapping maintained by this registry.</summary>
	private readonly Dictionary<string, IAdjustmentHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Lock protecting read-modify-write access to <see cref="_handlers" />.</summary>
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
			throw new ArgumentException(CalendarStrings.ArgumentException_KeyNullOrWhiteSpace, nameof(key));
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
