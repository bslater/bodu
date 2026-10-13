// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberCacheTests.GetMembers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatMemberCacheTests
{
    /// <summary>
    /// Verifies that a type's descriptors are discovered once and then shared: a second call returns the same array,
    /// and the cache holds one entry.
    /// </summary>
    [TestMethod]
    public void GetMembers_WhenCalledTwiceForAType_ShouldReturnTheSameDescriptors()
    {
        var cache = new FlatMemberCache();

        FlatMemberDescriptor[] first = cache.GetMembers(typeof(PlainSample), includeFields: false, namingPolicy: null, CreateDuplicateNameException);
        FlatMemberDescriptor[] second = cache.GetMembers(typeof(PlainSample), includeFields: false, namingPolicy: null, CreateDuplicateNameException);

        Assert.AreSame(first, second);
        Assert.AreEqual(1, cache.Count);
        CollectionAssert.AreEqual(
            new[] { nameof(PlainSample.Name), nameof(PlainSample.Count) },
            first.Select(static member => member.Name).ToArray());
    }

    /// <summary>
    /// Verifies that a type mapping two members to one name throws the exception the caller creates, naming the type
    /// and the name, and is not cached.
    /// </summary>
    [TestMethod]
    public void GetMembers_WhenTwoMembersMapToOneName_ShouldThrowTheCallersExceptionAndCacheNothing()
    {
        var cache = new FlatMemberCache();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = cache.GetMembers(typeof(DuplicateSample), includeFields: false, namingPolicy: null, CreateDuplicateNameException);
        });

        Assert.AreEqual($"{nameof(DuplicateSample)}:X", ex.Message);
        Assert.AreEqual(0, cache.Count);
    }
}
