// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScheduleDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// An INI document POCO holding one <see cref="GeneratedScheduleSection" />, through which INI's reflection binder
/// reads the section that the generated factory reads directly.
/// </summary>
public sealed class ScheduleDocument
{
    /// <summary>
    /// Gets or sets the schedule section.
    /// </summary>
    /// <value>The section.</value>
    public GeneratedScheduleSection Schedule { get; set; } = new();
}
