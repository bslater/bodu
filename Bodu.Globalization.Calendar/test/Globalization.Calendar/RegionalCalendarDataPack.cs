// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RegionalCalendarDataPack.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents one entry in the manifest of regional calendar data packs the solution ships, pairing a region's
/// display name with direct references to its <c>&lt;Region&gt;CalendarData</c> factory members.
/// </summary>
/// <param name="Region">The region's display name, used as the known-answer row label.</param>
/// <param name="GetSupportedCountries">Returns the country codes the region's data factory provides resources for.</param>
/// <param name="CreateService">Creates a notable-date service for a territory using the region's data factory.</param>
/// <remarks>
/// The delegate members capture the concrete <c>&lt;Region&gt;CalendarData</c> factory directly, so the manifest is a
/// compile-time reference to every regional pack: a pack dropped from the build cannot be expressed as a row, and the
/// test assembly fails to compile rather than silently omitting the region from the test and coverage surface.
/// </remarks>
public sealed record RegionalCalendarDataPack(
    string Region,
    Func<IReadOnlyList<string>> GetSupportedCountries,
    Func<string, INotableDateService> CreateService) : IKat
{
    /// <inheritdoc />
    public string Name => Region;
}
