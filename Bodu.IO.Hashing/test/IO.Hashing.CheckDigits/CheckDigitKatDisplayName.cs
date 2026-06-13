// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitKatDisplayName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Produces the per-row display name for the check-digit known-answer <c>[DynamicData]</c> tests, surfacing the
/// vector's human-readable standard name (the first element of the row) instead of an opaque index.
/// </summary>
internal static class CheckDigitKatDisplayName
{
    /// <summary>
    /// Returns the display name for a single check-digit known-answer <c>[DynamicData]</c> row.
    /// </summary>
    /// <param name="methodInfo">The test method whose row is being named.</param>
    /// <param name="data">The row payload; the first element is the human-readable standard name.</param>
    /// <returns>The standard name carried by the first element of <paramref name="data" />.</returns>
    public static string GetDisplayName(MethodInfo methodInfo, object[] data) =>
        (string)data[0];
}
