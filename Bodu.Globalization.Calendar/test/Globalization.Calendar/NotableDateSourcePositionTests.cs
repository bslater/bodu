// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateSourcePositionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies how <see cref="NotableDateSourcePosition" /> reads the positions that XML and JSON readers report.
/// </summary>
[TestClass]
public sealed partial class NotableDateSourcePositionTests
{
    /// <summary>
    /// Returns the exception that parsing <paramref name="json" /> raises, parsed as the calendar's JSON reader parses
    /// it, without a leading byte order mark.
    /// </summary>
    /// <param name="json">JSON text that is not well-formed.</param>
    /// <returns>The reader's failure.</returns>
    private static JsonException ParseFailure(string json) =>
        // The reader throws an internal type derived from JsonException, which no test can name exactly.
        Assert.Throws<JsonException>(() =>
        {
            using JsonDocument document = JsonDocument.Parse(json.TrimStart('\uFEFF'));
        });
}
