// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDiscoveryTests.Enumerate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Text.Serialization;

public sealed partial class FlatMemberDiscoveryTests
{
    /// <summary>
    /// Verifies that, with fields included, discovery returns the public properties and fields and skips a member
    /// marked to be ignored always.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenFieldsIncluded_ShouldReturnPropertiesAndFieldsButNotIgnoredMembers()
    {
        MemberInfo[] members = FlatMemberDiscovery.Enumerate(typeof(MemberSample), includeFields: true).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { nameof(MemberSample.Count), nameof(MemberSample.PublicField) },
            members.Select(static member => member.Name).ToArray());
    }
}
