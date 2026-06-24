// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DescribedStatus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Bodu.Extensions;

/// <summary>
/// A sample enumeration whose members exercise the various attribute combinations recognized by the enum description
/// and display-name helpers.
/// </summary>
public enum DescribedStatus
{
    /// <summary>
    /// Carries a <see cref="System.ComponentModel.DescriptionAttribute" /> only.
    /// </summary>
    [System.ComponentModel.Description("Is Active")]
    Active,

    /// <summary>
    /// Carries a <see cref="DisplayAttribute" /> name only.
    /// </summary>
    [Display(Name = "Pend")]
    Pending,

    /// <summary>
    /// Carries no attributes and falls back to its name.
    /// </summary>
    Plain,

    /// <summary>
    /// Carries a <see cref="DisplayAttribute" /> short name only.
    /// </summary>
    [Display(ShortName = "Cx")]
    Cancelled,
}
