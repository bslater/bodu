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
    /// Verifies that, with fields excluded, discovery returns only the plain property: the field, the ignored property
    /// and the indexer are skipped.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenFieldsExcluded_ShouldSkipFieldsIgnoredPropertiesAndIndexers()
    {
        MemberInfo[] members = FlatMemberDiscovery.Enumerate(typeof(MemberSample), includeFields: false).ToArray();

        CollectionAssert.AreEqual(
            new[] { nameof(MemberSample.Name) },
            members.Select(static member => member.Name).ToArray());
    }
}
