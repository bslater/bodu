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

    /// <summary>
    /// Verifies that a naming policy converts a member's name, and that a member's own
    /// <see cref="PropertyNameAttribute" /> name is kept as it is instead.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenANamingPolicyIsGiven_ShouldConvertTheNameUnlessTheMemberNamesItself()
    {
        var converted = new FlatMemberDescriptor(typeof(MemberSample).GetProperty(nameof(MemberSample.MaybeDay))!, NamingPolicy.SnakeCaseUpper);
        var named = new FlatMemberDescriptor(typeof(MemberSample).GetProperty(nameof(MemberSample.Count))!, NamingPolicy.SnakeCaseUpper);

        Assert.AreEqual("MAYBE_DAY", converted.Name);
        Assert.AreEqual("CUSTOM_KEY", named.Name);
    }

    /// <summary>
    /// Verifies that a member of a nullable scalar type is described by the scalar it makes nullable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMemberTypeIsANullableScalar_ShouldDescribeTheScalarItMakesNullable()
    {
        var descriptor = new FlatMemberDescriptor(typeof(MemberSample).GetProperty(nameof(MemberSample.MaybeDay))!, namingPolicy: null);

        Assert.AreEqual(typeof(DateOnly?), descriptor.MemberType);
        Assert.AreEqual(typeof(DateOnly), descriptor.ScalarType);
        Assert.IsTrue(descriptor.IsNullable);
        Assert.AreEqual(FlatScalarKind.DateOnly, descriptor.ScalarKind);
        Assert.IsTrue(descriptor.IsScalar);
    }

    /// <summary>
    /// Verifies that the member's declared name is kept apart from the serialized name that a
    /// <see cref="PropertyNameAttribute" /> or a naming policy gives it.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTheMemberIsRenamed_ShouldKeepItsDeclaredName()
    {
        var renamed = new FlatMemberDescriptor(typeof(MemberSample).GetProperty(nameof(MemberSample.Count))!, namingPolicy: null);
        var converted = new FlatMemberDescriptor(typeof(KeyValuePair<string, int>).GetProperty(nameof(KeyValuePair<string, int>.Key))!, NamingPolicy.CamelCase);

        Assert.AreEqual("CUSTOM_KEY", renamed.Name);
        Assert.AreEqual(nameof(MemberSample.Count), renamed.MemberName);
        Assert.AreEqual("key", converted.Name);
        Assert.AreEqual("Key", converted.MemberName);
    }
}
