// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateResourceProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Supplies the <see cref="NotableDateResource" /> currently in effect, allowing a long-lived service to observe a
/// resource that is swapped at runtime.
/// </summary>
/// <remarks>
/// <para>
/// A provider decouples a service from a single immutable resource: the service reads <see cref="Current" /> on each
/// resolution, so a reload performed through a mutable provider takes effect on the next query without recreating the
/// service or its consumers.
/// </para>
/// </remarks>
public interface INotableDateResourceProvider
{
    /// <summary>
    /// Gets the resource currently in effect.
    /// </summary>
    /// <value>The current <see cref="NotableDateResource" />.</value>
    NotableDateResource Current { get; }
}
