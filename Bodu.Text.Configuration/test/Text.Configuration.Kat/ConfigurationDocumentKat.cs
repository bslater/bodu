// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationDocumentKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Configuration.Kat;

/// <summary>
/// Represents a known-answer test row for <see cref="ConfigurationDocument" />: a configuration text payload, the
/// section / key the test probes, and the expected resolved value once
/// <see cref="ConfigurationDocument.Parse(string)" /> and <see cref="IniDocument" />'s sectioned lookup chain produce a
/// view.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Text">The raw configuration text supplied to the parser.</param>
/// <param name="SectionGlob">The section glob (or <c>"*"</c>) under which <see cref="Key" /> is resolved.</param>
/// <param name="Key">The key whose value is asserted.</param>
/// <param name="ExpectedValue">
/// The expected string value after parse + resolve; <see langword="null" /> when the key is absent.
/// </param>
public sealed record ConfigurationDocumentKat(
    string Name,
    string Text,
    string SectionGlob,
    string Key,
    string? ExpectedValue) : IKat;
