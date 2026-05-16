// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ClassifiedMemberTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.CodeStyle.Ordering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.Ordering.Test.Classification;

[TestClass]
public sealed class ClassifiedMemberTests
{
    /// <summary>
    /// Verifies that constructor-supplied properties round-trip through the public getters.
    /// </summary>
    [TestMethod]
    public void Ctor_ShouldRoundTripProperties()
    {
        ClassifiedMember member = new ClassifiedMember(MemberKind.Method, AccessibilityRank.Public, groupRank: 7, accessibilityRank: 0);

        Assert.AreEqual(MemberKind.Method, member.Kind);
        Assert.AreEqual(AccessibilityRank.Public, member.Accessibility);
        Assert.AreEqual(7, member.GroupRank);
        Assert.AreEqual(0, member.AccessibilityRank);
    }
}
