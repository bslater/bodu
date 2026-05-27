// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RuleCatalogueExpectation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Represents the cherry-pick expectations for a territory's flattened rule catalogue: the rule names that must appear
/// after the territory's XML resource is loaded, and the rule names that must not appear because the resource
/// deliberately did not opt in to them.
/// </summary>
/// <remarks>
/// <para>
/// A row drives the assertion against a single provider: the test invokes <see cref="ProviderFactory" />, calls
/// <c>LoadRules()</c>, materialises a name-set, and verifies that every entry in <see cref="Includes" /> is present and
/// every entry in <see cref="Excludes" /> is absent.
/// </para>
/// <para>
/// Rows live in project-local provider classes (for example
/// <c>UnitedStatesTerritoryAnswers.RuleCatalogueExpectations()</c>) so each Data package can wire its own provider
/// factory; the record itself is authored once in the main calendar test project and linked into each Data test csproj
/// via
/// <c>&lt;Compile Include="..\..\Bodu.Globalization.Calendar\test\Infrastructure\RuleCatalogueExpectation.cs" Link="..." /&gt;</c>
/// .
/// </para>
/// </remarks>
public sealed record RuleCatalogueExpectation : IKat
{
    /// <inheritdoc />
    /// <remarks>Returns <see cref="Territory" /> so the row participates in <see cref="KatDisplayName" />-driven formatting.</remarks>
    string IKat.Name => Territory;

    /// <summary>
    /// Gets the delegate that materialises the rule provider under test. Invoked once per row.
    /// </summary>
    /// <returns>A factory returning a configured <see cref="INotableDateRuleProvider" />.</returns>
    public required Func<INotableDateRuleProvider> ProviderFactory { get; init; }

    /// <summary>
    /// Gets the territory label rendered in the row's display name — for example <c>"US"</c>, <c>"GB"</c>, or
    /// <c>"FR"</c>. Does not affect assertions; used purely for the test explorer entry.
    /// </summary>
    /// <returns>The territory label.</returns>
    public required string Territory { get; init; }

    /// <summary>
    /// Gets the list of rule names that must appear in the flattened catalogue.
    /// </summary>
    /// <returns>An ordered, read-only list of rule names; never <see langword="null" />.</returns>
    public IReadOnlyList<string> Includes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the list of rule names that must not appear in the flattened catalogue — typically rules from other
    /// territories or unselected categories that the resource deliberately did not opt in to.
    /// </summary>
    /// <returns>An ordered, read-only list of rule names; never <see langword="null" />.</returns>
    public IReadOnlyList<string> Excludes { get; init; } = Array.Empty<string>();
}
