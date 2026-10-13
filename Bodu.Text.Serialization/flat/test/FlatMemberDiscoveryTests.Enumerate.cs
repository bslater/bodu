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

    /// <summary>
    /// Verifies that a property and a field hidden with <see langword="new" /> are not returned, and that the members
    /// hiding them are, once each.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenMembersAreHiddenWithNew_ShouldReturnOnlyTheMostDerivedDeclarations()
    {
        MemberInfo[] members = FlatMemberDiscovery.Enumerate(typeof(HidingSample), includeFields: true).ToArray();

        CollectionAssert.AreEquivalent(
            new[] { nameof(HidingSample.Port), nameof(HidingSample.Code) },
            members.Select(static member => member.Name).ToArray());
        Assert.IsTrue(members.All(static member => member.DeclaringType == typeof(HidingSample)));
    }

    /// <summary>
    /// Verifies that, with fields excluded, discovery returns only the plain property, the renamed one: the field, the ignored property
    /// and the indexer are skipped.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenFieldsExcluded_ShouldSkipFieldsIgnoredPropertiesAndIndexers()
    {
        MemberInfo[] members = FlatMemberDiscovery.Enumerate(typeof(MemberSample), includeFields: false).ToArray();

        CollectionAssert.AreEqual(
            new[] { nameof(MemberSample.Count) },
            members.Select(static member => member.Name).ToArray());
    }

    /// <summary>
    /// Verifies that the descriptors are ordered by the property-order attribute, members of equal order keeping the
    /// order discovery returned them in.
    /// </summary>
    [TestMethod]
    public void Describe_WhenMembersHaveOrders_ShouldOrderThemByOrderAndThenByDiscovery()
    {
        FlatMemberDescriptor[] members = FlatMemberDiscovery.Describe(typeof(OrderedSample), includeFields: false, namingPolicy: null);

        CollectionAssert.AreEqual(
            new[] { nameof(OrderedSample.First), nameof(OrderedSample.Second), nameof(OrderedSample.Last) },
            members.Select(static member => member.Name).ToArray());
    }

    /// <summary>
    /// Verifies that two members mapped to one name are found, by the name, and that distinct names, even ones that
    /// differ only in case, are not.
    /// </summary>
    [TestMethod]
    public void FindDuplicateName_WhenTwoMembersShareAName_ShouldReturnIt()
    {
        FlatMemberDescriptor[] duplicated = FlatMemberDiscovery.Describe(typeof(DuplicateSample), includeFields: false, namingPolicy: null);
        FlatMemberDescriptor[] distinct = FlatMemberDiscovery.Describe(typeof(OrderedSample), includeFields: false, namingPolicy: NamingPolicy.CamelCase);

        Assert.AreEqual("X", FlatMemberDiscovery.FindDuplicateName(duplicated));
        Assert.IsNull(FlatMemberDiscovery.FindDuplicateName(distinct));
    }
}
