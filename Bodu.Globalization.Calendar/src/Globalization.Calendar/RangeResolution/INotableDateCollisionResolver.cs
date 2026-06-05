// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateCollisionResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Resolves a set of notable-date occurrences that share the same emitted date, used when the resource's same-day
/// collision policy is <see cref="CollisionPolicy.Custom" />.
/// </summary>
public interface INotableDateCollisionResolver
{
    /// <summary>
    /// Selects which of the colliding occurrences to keep.
    /// </summary>
    /// <param name="date">The emitted date the occurrences share.</param>
    /// <param name="colliding">The two or more occurrences emitted on the date, in resolution order.</param>
    /// <returns>The occurrences to keep; an empty result suppresses the date entirely.</returns>
    IReadOnlyList<NotableDate> Resolve(DateOnly date, IReadOnlyList<NotableDate> colliding);
}
