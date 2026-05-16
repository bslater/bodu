// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MemberGroupRuleTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using Bodu.CodeStyle.Ordering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.Ordering.Test.Model;

[TestClass]
public sealed class MemberGroupRuleTests
{
    /// <summary>
    /// Verifies that the constructor throws when the name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new MemberGroupRule(null!, ImmutableArray<MemberKind>.Empty, ImmutableArray<string>.Empty);
        });

        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when the kinds array is default.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKindsIsDefault_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new MemberGroupRule("X", default, ImmutableArray<string>.Empty);
        });

        Assert.AreEqual("kinds", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor throws when the required-modifiers array is default.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRequiredModifiersIsDefault_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new MemberGroupRule("X", ImmutableArray<MemberKind>.Empty, default);
        });

        Assert.AreEqual("requiredModifiers", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldRoundTripProperties()
    {
        MemberGroupRule rule = new MemberGroupRule(
            name: "Constants",
            kinds: ImmutableArray.Create(MemberKind.Constant),
            requiredModifiers: ImmutableArray.Create("const"));

        Assert.AreEqual("Constants", rule.Name);
        Assert.AreEqual(1, rule.Kinds.Length);
        Assert.AreEqual(MemberKind.Constant, rule.Kinds[0]);
        Assert.AreEqual(1, rule.RequiredModifiers.Length);
        Assert.AreEqual("const", rule.RequiredModifiers[0]);
    }
}
