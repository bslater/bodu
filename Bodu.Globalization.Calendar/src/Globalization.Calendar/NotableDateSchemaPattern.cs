// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSchemaPattern.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies a pattern the notable-date XML schema enforces on a text value, which the JSON reader, validated by no
/// schema, checks itself.
/// </summary>
internal enum NotableDateSchemaPattern
{
    /// <summary>
    /// The schema's <c>identifier</c> type: a concept, rule or adjustment policy identifier, or a reference to one.
    /// </summary>
    Identifier,

    /// <summary>
    /// The schema's <c>resourceIdentifier</c> type: a resource identifier.
    /// </summary>
    ResourceIdentifier,

    /// <summary>
    /// The schema's <c>territoryCode</c> type: a country code, optionally followed by a subdivision code.
    /// </summary>
    TerritoryCode,
}
