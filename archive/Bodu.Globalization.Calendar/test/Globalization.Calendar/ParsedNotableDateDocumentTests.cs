// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParsedNotableDateDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="ParsedNotableDateDocument" /> coerces a default-uninitialised
/// <see cref="ImmutableArray{T}" /> to <see cref="ImmutableArray{T}.Empty" /> on either property, ensuring downstream
/// consumers never have to guard against <see cref="ImmutableArray{T}.IsDefault" />.
/// </summary>
[TestClass]
public sealed class ParsedNotableDateDocumentTests
{
    /// <summary>
    /// Verifies that constructing a document with default-uninitialised <c>useGroups</c> coerces the property to
    /// <see cref="ImmutableArray{T}.Empty" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenUseGroupsIsDefault_ShouldCoerceToEmpty()
    {
        ImmutableArray<NotableDateRuleUseGroup> defaultUseGroups = default;
        ImmutableArray<NotableDateRule> localRules = [];

        ParsedNotableDateDocument document = new(defaultUseGroups, localRules);

        Assert.IsFalse(document.UseGroups.IsDefault);
        Assert.AreEqual(0, document.UseGroups.Length);
    }

    /// <summary>
    /// Verifies that constructing a document with default-uninitialised <c>localRules</c> coerces the property to
    /// <see cref="ImmutableArray{T}.Empty" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLocalRulesIsDefault_ShouldCoerceToEmpty()
    {
        ImmutableArray<NotableDateRuleUseGroup> useGroups = [];
        ImmutableArray<NotableDateRule> defaultLocalRules = default;

        ParsedNotableDateDocument document = new(useGroups, defaultLocalRules);

        Assert.IsFalse(document.LocalRules.IsDefault);
        Assert.AreEqual(0, document.LocalRules.Length);
    }

    /// <summary>
    /// Verifies that constructing a document with both default-uninitialised arrays coerces both properties to
    /// <see cref="ImmutableArray{T}.Empty" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBothArraysAreDefault_ShouldCoerceBothToEmpty()
    {
        ParsedNotableDateDocument document = new(default, default);

        Assert.IsFalse(document.UseGroups.IsDefault);
        Assert.IsFalse(document.LocalRules.IsDefault);
        Assert.AreEqual(0, document.UseGroups.Length);
        Assert.AreEqual(0, document.LocalRules.Length);
    }

    /// <summary>
    /// Verifies that constructing a document with populated arrays preserves them verbatim — the coercion branch
    /// only fires for default-uninitialised arrays.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenArraysAreNonDefault_ShouldPreserveContents()
    {
        NotableDateRuleUseGroup useGroup = new(
            SourceResource: "shared.xml",
            UseAll: true,
            Uses: []);

        NotableDateRule rule = new()
        {
            Name = "Local",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
        };

        ParsedNotableDateDocument document = new(
            [useGroup],
            [rule]);

        Assert.AreEqual(1, document.UseGroups.Length);
        Assert.AreSame(useGroup, document.UseGroups[0]);
        Assert.AreEqual(1, document.LocalRules.Length);
        Assert.AreSame(rule, document.LocalRules[0]);
    }
}
