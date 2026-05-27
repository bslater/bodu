// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarBuilderOutputKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Represents a positive-vector known-answer test row for the <c>Bodu.Globalization.Calendar.Builder</c> source
/// generator: a source-document input and the resource name + rule count the builder must produce from it.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="SourceInput">The raw rule document supplied to the builder.</param>
/// <param name="ExpectedResourceName">
/// The resource name the builder must emit (typically the deterministic logical name).
/// </param>
/// <param name="ExpectedRuleCount">The number of rules the generated resource must contain after flattening.</param>
public sealed record CalendarBuilderOutputKat(
    string Name,
    string SourceInput,
    string ExpectedResourceName,
    int ExpectedRuleCount) : IKat;
