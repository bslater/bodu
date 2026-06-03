// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CookbookDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Holds the parsed components of a cookbook before override application and final validation.
/// </summary>
internal sealed class CookbookDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CookbookDocument" /> class.
    /// </summary>
    /// <param name="resourceId">The parsed resource identifier.</param>
    /// <param name="schemaVersion">The parsed schema version.</param>
    /// <param name="resolutionPolicy">The parsed resolution policy.</param>
    /// <param name="adjustmentPolicies">The parsed adjustment policies.</param>
    /// <param name="notableDates">The parsed notable-date concepts.</param>
    /// <param name="overrides">The parsed override operations.</param>
    public CookbookDocument(
        string resourceId,
        string schemaVersion,
        ResolutionPolicy resolutionPolicy,
        IReadOnlyList<AdjustmentPolicy> adjustmentPolicies,
        IReadOnlyList<NotableDateDefinition> notableDates,
        IReadOnlyList<CookbookOverride> overrides)
    {
        this.ResourceId = resourceId;
        this.SchemaVersion = schemaVersion;
        this.ResolutionPolicy = resolutionPolicy;
        this.AdjustmentPolicies = adjustmentPolicies;
        this.NotableDates = notableDates;
        this.Overrides = overrides;
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
    public IReadOnlyList<CookbookOverride> Overrides { get; }
}
