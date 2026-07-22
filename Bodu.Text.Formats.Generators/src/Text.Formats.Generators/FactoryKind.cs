// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FactoryKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Generators;

/// <summary>
/// Identifies which format factory a model describes.
/// </summary>
internal enum FactoryKind
{
    /// <summary>
    /// A delimited record factory implementing <c>Bodu.Text.Delimited.IDelimitedRecordFactory&lt;TRecord&gt;</c>.
    /// </summary>
    Delimited,

    /// <summary>
    /// An INI section factory implementing <c>Bodu.Text.Ini.IIniSectionFactory&lt;TSection&gt;</c>.
    /// </summary>
    Ini,
}
