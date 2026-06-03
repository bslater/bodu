// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateImport.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Describes an import of concepts from another resource: the entire resource when no cherry-picks are listed, or the
/// specific concepts named by its <see cref="Uses" />.
/// </summary>
/// <param name="Resource">The name of the source resource to resolve and import from.</param>
/// <param name="Uses">The concepts to cherry-pick, or an empty list to import every concept from the source.</param>
internal sealed record NotableDateImport(
    string Resource,
    IReadOnlyList<NotableDateImportUse> Uses);
