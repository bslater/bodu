// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationResolveOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how a <see cref="BoduConfigurationDocument" /> is projected into a
/// <see cref="BoduConfigurationView" /> for a specific target path.
/// </summary>
public sealed partial class BoduConfigurationResolveOptions
{
    /// <summary>
    /// Gets the behaviour profile this option bag represents.
    /// </summary>
    /// <returns>The selected profile.</returns>
    public BoduConfigurationProfile Profile { get; init; } = BoduConfigurationProfile.Bodu;

    /// <summary>
    /// Gets the optional path root used to evaluate anchored glob patterns. When <see langword="null" /> the
    /// document's load path is used; if neither is available, <see cref="MissingPathRootMode" /> controls
    /// behaviour.
    /// </summary>
    /// <returns>The path root, or <see langword="null" />.</returns>
    public string? PathRoot { get; init; }

    /// <summary>
    /// Gets how the resolver reacts to an absent <see cref="PathRoot" /> when the document was parsed from a
    /// string.
    /// </summary>
    /// <returns>The selected mode.</returns>
    public BoduConfigurationMissingPathRootMode MissingPathRootMode { get; init; } =
        BoduConfigurationMissingPathRootMode.UseEmptyRoot;

    /// <summary>
    /// Gets a value indicating whether preamble (global) properties contribute to resolution. Defaults to
    /// <see langword="true" /> for the Bodu profile and <see langword="false" /> for EditorConfig-compatible.
    /// </summary>
    /// <returns><see langword="true" /> when preamble properties are honoured.</returns>
    public bool ApplyPreambleProperties { get; init; } = true;

    /// <summary>
    /// Gets the comparison used when matching target paths against section patterns.
    /// </summary>
    /// <returns>The selected <see cref="StringComparison" />.</returns>
    public StringComparison PathComparison { get; init; } = StringComparison.Ordinal;

    /// <summary>
    /// Gets the unset-value handling mode used by the resolver.
    /// </summary>
    /// <returns>The selected unset-value mode.</returns>
    public BoduConfigurationUnsetValueMode UnsetValueMode { get; init; } =
        BoduConfigurationUnsetValueMode.TreatAsLiteral;

    /// <summary>
    /// Gets the key options used when expanding raw keys into configuration keys for the resolved view.
    /// </summary>
    /// <returns>The selected key options.</returns>
    public BoduConfigurationKeyOptions KeyOptions { get; init; } = BoduConfigurationKeyOptions.Default;
}
