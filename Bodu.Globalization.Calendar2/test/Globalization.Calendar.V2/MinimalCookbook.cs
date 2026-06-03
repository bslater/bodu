// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MinimalCookbook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Loads the embedded minimal cookbook fixtures used by the first-version test catalogue.
/// </summary>
internal static class MinimalCookbook
{
    /// <summary>
    /// Loads the baseline minimal cookbook with no override operations.
    /// </summary>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    public static NotableDateResource Load() =>
        LoadResource("minimal.xml");

    /// <summary>
    /// Loads the minimal cookbook with a <c>RemoveRule</c> override targeting the Puerto Rico Constitution Day rule.
    /// </summary>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    public static NotableDateResource LoadWithRemoveOverride() =>
        LoadResource("minimal-remove-pr-rule.xml");

    /// <summary>
    /// Loads the minimal cookbook with a <c>PatchRule</c> override targeting the Puerto Rico Constitution Day rule.
    /// </summary>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    public static NotableDateResource LoadWithPatchOverride() =>
        LoadResource("minimal-patch-pr-rule.xml");

    /// <summary>
    /// Loads and parses an embedded cookbook fixture by file name.
    /// </summary>
    /// <param name="fileName">The fixture file name.</param>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    private static NotableDateResource LoadResource(string fileName)
    {
        string resourceName = "Bodu.Globalization.Calendar.V2.Fixtures." + fileName;
        using Stream stream = typeof(MinimalCookbook).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{resourceName}'.");

        return NotableDateCookbook.Load(stream);
    }
}
