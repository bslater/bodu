// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceAnchorKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Represents a recurrence rule anchored at a series start, with the horizon up to which a test compares the rule's
/// queries against the occurrence stream enumerated from that start.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Rule">The recurrence-rule text.</param>
/// <param name="Start">The series start (<c>DTSTART</c>) the rule is anchored to.</param>
/// <param name="Horizon">The latest instant the test queries; the stream is materialized up to it.</param>
public sealed record RecurrenceAnchorKat(string Name, string Rule, DateTime Start, DateTime Horizon) : IKat;
