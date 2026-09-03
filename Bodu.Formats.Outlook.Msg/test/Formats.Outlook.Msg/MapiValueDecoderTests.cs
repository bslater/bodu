// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiValueDecoderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Verifies the behavior of <see cref="MapiValueDecoder" />, the container-neutral wire-value decoder shared by the
/// Outlook format readers. The member partials pin the "never throws" contract and the UTC time-stamp semantics.
/// </summary>
[TestClass]
public partial class MapiValueDecoderTests
{
    /// <summary>
    /// Runs an action with the process-local time zone temporarily switched, restoring the original afterwards.
    /// </summary>
    /// <param name="timeZoneId">The IANA time-zone identifier to switch to.</param>
    /// <param name="action">The action to run under the switched zone.</param>
    internal static void WithLocalTimeZone(string timeZoneId, Action action)
    {
        string? original = Environment.GetEnvironmentVariable("TZ");
        try
        {
            Environment.SetEnvironmentVariable("TZ", timeZoneId);
            TimeZoneInfo.ClearCachedData();
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable("TZ", original);
            TimeZoneInfo.ClearCachedData();
        }
    }
}
