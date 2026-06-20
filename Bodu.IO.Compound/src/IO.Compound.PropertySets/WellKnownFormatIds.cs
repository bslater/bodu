// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WellKnownFormatIds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Provides the well-known format identifiers (FMTIDs) of the standard OLE property sets.
/// </summary>
/// <remarks>
/// These identifiers are defined by the OLE property-set specification and are used to recognize the
/// summary-information, document-summary-information, and user-defined property sections within a compound file.
/// </remarks>
internal static class WellKnownFormatIds
{
    /// <summary>The format identifier of the summary-information property set.</summary>
    internal static readonly Guid SummaryInformation = new("F29F85E0-4FF9-1068-AB91-08002B27B3D9");

    /// <summary>The format identifier of the first (built-in) document-summary-information section.</summary>
    internal static readonly Guid DocumentSummaryInformation = new("D5CDD502-2E9C-101B-9397-08002B2CF9AE");

    /// <summary>The format identifier of the user-defined document-summary-information section.</summary>
    internal static readonly Guid UserDefinedProperties = new("D5CDD505-2E9C-101B-9397-08002B2CF9AE");
}
