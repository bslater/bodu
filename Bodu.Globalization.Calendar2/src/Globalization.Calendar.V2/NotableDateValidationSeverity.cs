// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateValidationSeverity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Classifies the severity of a <see cref="NotableDateValidationDiagnostic" /> produced while loading a notable-date
/// document.
/// </summary>
public enum NotableDateValidationSeverity
{
    /// <summary>
    /// An informational message that does not affect loading.
    /// </summary>
    Information = 0,

    /// <summary>
    /// A non-fatal issue that does not prevent loading but may indicate an authoring mistake.
    /// </summary>
    Warning,

    /// <summary>
    /// A fatal issue that prevents the notable-date document from loading successfully.
    /// </summary>
    Error,
}
