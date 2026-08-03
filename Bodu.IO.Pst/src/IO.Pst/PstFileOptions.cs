// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Controls how a <see cref="PstFile" /> is opened and validated.
/// </summary>
public sealed class PstFileOptions
{
    /// <summary>
    /// Gets the shared default options instance.
    /// </summary>
    /// <value>The default options.</value>
    internal static PstFileOptions Default { get; } = new();

    /// <summary>
    /// Gets how strictly the file's structures and checksums are validated.
    /// </summary>
    /// <value>The validation level; <see cref="PstValidationLevel.Compatible" /> by default.</value>
    public PstValidationLevel ValidationLevel { get; init; } = PstValidationLevel.Compatible;
}
