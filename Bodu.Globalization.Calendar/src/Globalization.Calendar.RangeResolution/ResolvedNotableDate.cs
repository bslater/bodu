// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolvedNotableDate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Pairs a resolved <see cref="NotableDate" /> with its <see cref="NotableDateProvenance" /> as it flows from the range
/// pipeline to the collision-resolution stage, so provenance can participate in same-day arbitration without being
/// exposed on the public <see cref="NotableDate" /> surface.
/// </summary>
/// <param name="Notable">The resolved notable date.</param>
/// <param name="Provenance">The layer the occurrence originated from.</param>
internal sealed record ResolvedNotableDate(NotableDate Notable, NotableDateProvenance Provenance);
