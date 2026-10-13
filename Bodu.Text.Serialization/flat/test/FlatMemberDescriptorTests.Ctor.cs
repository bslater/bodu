// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FlatMemberDescriptorTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

public sealed partial class FlatMemberDescriptorTests
{
    /// <summary>
    /// Verifies that a property's name and required attributes are read, and that a public property with public
    /// accessors is readable and writable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPropertyHasNameAndRequiredAttributes_ShouldReadThem()
    {
        var descriptor = new FlatMemberDescriptor(typeof(MemberSample).GetProperty(nameof(MemberSample.Count))!, namingPolicy: null);

        Assert.AreEqual("CUSTOM_KEY", descriptor.Name);
        Assert.IsTrue(descriptor.Required);
        Assert.IsTrue(descriptor.CanRead);
        Assert.IsTrue(descriptor.CanWrite);
    }

    /// <summary>
    /// Verifies that a property declared with the C# <see langword="required" /> keyword is described as required, as
    /// one marked <see cref="RequiredAttribute" /> is.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPropertyIsDeclaredWithTheRequiredKeyword_ShouldDescribeItAsRequired()
    {
        var descriptor = new FlatMemberDescriptor(typeof(RequiredKeywordSample).GetProperty(nameof(RequiredKeywordSample.Count))!, namingPolicy: null);

        Assert.IsTrue(descriptor.Required);
    }

    /// <summary>
    /// Verifies that a read-only field is described as readable but not writable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFieldIsReadOnly_ShouldDescribeItAsReadableButNotWritable()
    {
        var descriptor = new FlatMemberDescriptor(typeof(MemberSample).GetField(nameof(MemberSample.ImmutableField))!, namingPolicy: null);

        Assert.IsTrue(descriptor.CanRead);
        Assert.IsFalse(descriptor.CanWrite);
    }

    /// <summary>
    /// Verifies that a property's order attribute is read.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPropertyHasOrderAttribute_ShouldReadTheOrder()
    {
        var descriptor = new FlatMemberDescriptor(typeof(MemberSample).GetProperty(nameof(MemberSample.Number))!, namingPolicy: null);

        Assert.AreEqual(2, descriptor.Order);
    }
}
