// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetAnchorKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents a recurrence set, written as an iCalendar property block, with the horizon up to which a test compares
/// the set's queries against the union of its sources.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Block">The iCalendar property block: a <c>DTSTART</c> line and the set's rules and dates.</param>
/// <param name="Horizon">The latest instant the test queries; the occurrences are materialized up to it.</param>
public sealed record RecurrenceSetAnchorKat(string Name, string Block, DateTime Horizon) : IKat;
