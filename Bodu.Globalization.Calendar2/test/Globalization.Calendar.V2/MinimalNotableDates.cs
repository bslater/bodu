// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MinimalNotableDates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Loads the embedded minimal notable-date document fixtures used by the first-version test catalogue.
/// </summary>
internal static class MinimalNotableDates
{
    /// <summary>
    /// Loads the baseline minimal notable-date document with no override operations.
    /// </summary>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    public static NotableDateResource Load() =>
        LoadResource("minimal.xml");

    /// <summary>
    /// Loads the minimal notable-date document with a <c>RemoveRule</c> override targeting the Puerto Rico Constitution Day rule.
    /// </summary>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    public static NotableDateResource LoadWithRemoveOverride() =>
        LoadResource("minimal-remove-pr-rule.xml");

    /// <summary>
    /// Loads the minimal notable-date document with a <c>PatchRule</c> override targeting the Puerto Rico Constitution Day rule.
    /// </summary>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    public static NotableDateResource LoadWithPatchOverride() =>
        LoadResource("minimal-patch-pr-rule.xml");

    /// <summary>
    /// Loads and parses an embedded notable-date document fixture by file name.
    /// </summary>
    /// <param name="fileName">The fixture file name.</param>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    private static NotableDateResource LoadResource(string fileName)
    {
        string resourceName = "Bodu.Globalization.Calendar.V2.Fixtures." + fileName;
        using Stream stream = typeof(MinimalNotableDates).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded fixture '{resourceName}'.");

        return NotableDateResourceLoader.Load(stream);
    }
}
