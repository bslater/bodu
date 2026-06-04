// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IAdjustmentHandlerRegistry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Provides lookup of custom <see cref="IAdjustmentHandler" /> implementations by handler key, binding the
/// <see cref="AdjustmentAction.Custom" /> action to caller-supplied logic.
/// </summary>
public interface IAdjustmentHandlerRegistry
{
    /// <summary>
    /// Determines whether a handler is registered under the supplied key.
    /// </summary>
    /// <param name="key">The handler key.</param>
    /// <returns><see langword="true" /> if a handler is registered; otherwise <see langword="false" />.</returns>
    bool Contains(string key);

    /// <summary>
    /// Attempts to retrieve the handler registered under the supplied key.
    /// </summary>
    /// <param name="key">The handler key.</param>
    /// <param name="handler">When this method returns, the registered handler, or <see langword="null" /> when none.</param>
    /// <returns><see langword="true" /> if a handler was found; otherwise <see langword="false" />.</returns>
    bool TryGet(string key, out IAdjustmentHandler? handler);
}
