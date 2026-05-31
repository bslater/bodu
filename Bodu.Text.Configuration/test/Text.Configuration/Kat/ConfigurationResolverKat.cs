// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationResolverKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Configuration.Kat;

/// <summary>
/// Represents a known-answer test row for resolving a file path against a parsed configuration document, asserting the
/// value the resolver hands back for a probed key.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Text">The configuration text supplied to the parser.</param>
/// <param name="ResolvePath">The file path supplied to <see cref="IniDocument" />.<c>Resolve(path)</c>.</param>
/// <param name="Key">The key whose resolved value is asserted.</param>
/// <param name="ExpectedValue">
/// The expected string value after resolution; <see langword="null" /> when the key is absent.
/// </param>
public sealed record ConfigurationResolverKat(
    string Name,
    string Text,
    string ResolvePath,
    string Key,
    string? ExpectedValue) : IKat;
