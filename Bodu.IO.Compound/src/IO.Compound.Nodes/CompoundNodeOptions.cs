// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundNodeOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Nodes;

/// <summary>
/// Specifies options that control the behavior of the mutable compound-file object model.
/// </summary>
/// <remarks>
/// The OLE2 / Compound File Binary format treats entry names case-insensitively, so the default name comparison is
/// case-insensitive ordinal. Set <see cref="NameComparisonCaseSensitive" /> to <see langword="true" /> only when a
/// case-sensitive in-memory model is explicitly required.
/// </remarks>
public struct CompoundNodeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether child names are compared case-sensitively.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> to compare names with ordinal case sensitivity; <see langword="false" /> (the default)
    /// to compare them case-insensitively, matching the compound-file format.
    /// </returns>
    public bool NameComparisonCaseSensitive { get; set; }

    /// <summary>
    /// Gets the string comparer that implements the configured name comparison.
    /// </summary>
    /// <returns>
    /// An ordinal comparer that is case-sensitive or case-insensitive per <see cref="NameComparisonCaseSensitive" />.
    /// </returns>
    internal readonly StringComparer NameComparer =>
        NameComparisonCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
}
