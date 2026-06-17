// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerRegistry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// A mutable registry of custom <see cref="IAdjustmentHandler" /> implementations keyed by handler key.
/// </summary>
/// <remarks>
/// <para>
/// Keys are matched ordinally and are case-sensitive. Registering a key that is already present replaces the previous
/// handler.
/// </para>
/// </remarks>
public sealed class AdjustmentHandlerRegistry
    : IAdjustmentHandlerRegistry
{
    /// <summary>The registered handlers keyed by handler key.</summary>
    private readonly Dictionary<string, IAdjustmentHandler> _handlers = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers a handler under a key, replacing any existing registration.
    /// </summary>
    /// <param name="key">The handler key the policy references.</param>
    /// <param name="handler">The handler implementation.</param>
    /// <returns>The same registry, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="handler" /> is <see langword="null" />.
    /// </exception>
    public AdjustmentHandlerRegistry Register(string key, IAdjustmentHandler handler)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(handler);

        _handlers[key] = handler;
        return this;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Contains(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _handlers.ContainsKey(key);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGet(string key, out IAdjustmentHandler? handler)
    {
        ThrowHelper.ThrowIfNull(key);

        bool found = _handlers.TryGetValue(key, out IAdjustmentHandler? value);
        handler = value;
        return found;
    }
}
