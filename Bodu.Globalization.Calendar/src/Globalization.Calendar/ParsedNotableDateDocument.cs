// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParsedNotableDateDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Holds the parsed components of a notable-date document before override application and final validation.
/// </summary>
internal sealed class ParsedNotableDateDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParsedNotableDateDocument" /> class.
    /// </summary>
    /// <param name="resourceId">The parsed resource identifier.</param>
    /// <param name="schemaVersion">The parsed schema version.</param>
    /// <param name="resolutionPolicy">The parsed resolution policy.</param>
    /// <param name="adjustmentPolicies">The parsed adjustment policies.</param>
    /// <param name="notableDates">The parsed notable-date concepts.</param>
    /// <param name="overrides">The parsed override operations.</param>
    /// <param name="imports">The parsed import directives.</param>
    public ParsedNotableDateDocument(
        string resourceId,
        string schemaVersion,
        ResolutionPolicy resolutionPolicy,
        IReadOnlyList<AdjustmentPolicy> adjustmentPolicies,
        IReadOnlyList<NotableDateDefinition> notableDates,
        IReadOnlyList<NotableDateRuleOverride> overrides,
        IReadOnlyList<NotableDateImport> imports)
    {
        ResourceId = resourceId;
        SchemaVersion = schemaVersion;
        ResolutionPolicy = resolutionPolicy;
        AdjustmentPolicies = adjustmentPolicies;
        NotableDates = notableDates;
        Overrides = overrides;
        Imports = imports;
    }

    /// <summary>
    /// Gets the parsed resource identifier.
    /// </summary>
    /// <returns>The resource id.</returns>
    public string ResourceId { get; }

    /// <summary>
    /// Gets the parsed schema version.
    /// </summary>
    /// <returns>The schema version string.</returns>
    public string SchemaVersion { get; }

    /// <summary>
    /// Gets the parsed resolution policy.
    /// </summary>
    /// <returns>The resolution policy.</returns>
    public ResolutionPolicy ResolutionPolicy { get; }

    /// <summary>
    /// Gets the parsed adjustment policies.
    /// </summary>
    /// <returns>The adjustment policies.</returns>
    public IReadOnlyList<AdjustmentPolicy> AdjustmentPolicies { get; }

    /// <summary>
    /// Gets the parsed notable-date concepts.
    /// </summary>
    /// <returns>The notable-date concepts.</returns>
    public IReadOnlyList<NotableDateDefinition> NotableDates { get; }

    /// <summary>
    /// Gets the parsed override operations.
    /// </summary>
    /// <returns>The override operations.</returns>
    public IReadOnlyList<NotableDateRuleOverride> Overrides { get; }

    /// <summary>
    /// Gets the parsed import directives.
    /// </summary>
    /// <returns>The import directives.</returns>
    public IReadOnlyList<NotableDateImport> Imports { get; }
}
