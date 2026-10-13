// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IResolutionPolicySource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Exposes the resolution policy of the resource a service resolves against, so a decorator that serves the service's
/// results from a cache can select a window's occurrences as the service does.
/// </summary>
internal interface IResolutionPolicySource
{
    /// <summary>
    /// Gets the resolution policy of the resource currently in effect.
    /// </summary>
    /// <value>The resource's <see cref="ResolutionPolicy" />.</value>
    ResolutionPolicy ResolutionPolicy { get; }
}
