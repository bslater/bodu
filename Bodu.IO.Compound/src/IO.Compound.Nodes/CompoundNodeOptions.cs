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
/// The OLE2 / Compound File Binary format treats entry names case-insensitively, so by default the model keys names
/// with the shared compound-file name relationship — the same comparison the reader and writer use, so an in-memory
/// duplicate is detected exactly as the serialized container would collapse it. Set
/// <see cref="NameComparisonCaseSensitive" /> to <see langword="true" /> only when a case-sensitive in-memory model is
/// explicitly required.
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
    /// Gets the comparer that implements the configured name comparison.
    /// </summary>
    /// <returns>
    /// An ordinal case-sensitive comparer when <see cref="NameComparisonCaseSensitive" /> is <see langword="true" />;
    /// otherwise the shared <see cref="CompoundNameComparer" /> that matches the compound-file format.
    /// </returns>
    internal readonly IEqualityComparer<string> NameComparer =>
        NameComparisonCaseSensitive ? StringComparer.Ordinal : CompoundNameComparer.Instance;
}
