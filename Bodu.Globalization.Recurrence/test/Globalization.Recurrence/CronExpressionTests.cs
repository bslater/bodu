// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpressionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Contains unit tests for the <see cref="CronExpression" /> type.
/// </summary>
[TestClass]
public sealed partial class CronExpressionTests
{
    /// <summary>
    /// Reads an instant written as <c>yyyy-MM-ddTHH:mm:ss</c>.
    /// </summary>
    /// <param name="text">The instant text.</param>
    /// <returns>The instant.</returns>
    private static DateTime Instant(string text) =>
        DateTime.ParseExact(text, "s", CultureInfo.InvariantCulture);
}
