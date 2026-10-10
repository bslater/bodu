// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.SharedScalarCodec.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv;

/// <summary>Regression coverage for the shared invariant conversion used by DotEnv.</summary>
public partial class DotEnvSerializerTests
{
    [TestMethod]
    public void ScalarCodec_WhenConfigurationNullableHasOnlyWhitespace_ShouldNotTreatItAsNull()
    {
        Assert.IsNull(InvariantScalarCodec.Parse(string.Empty, typeof(int?), InvariantScalarCodec.Policy.Configuration));
        Assert.ThrowsExactly<FormatException>(() =>
            InvariantScalarCodec.Parse("  ", typeof(int?), InvariantScalarCodec.Policy.Configuration));
    }

    [TestMethod]
    public void ScalarCodec_WhenConfigurationFormatsScalar_ShouldUseInvariantCulture()
    {
        Assert.AreEqual("1.25", InvariantScalarCodec.Format(1.25m, InvariantScalarCodec.Policy.Configuration));
        Assert.AreEqual("false", InvariantScalarCodec.Format(false, InvariantScalarCodec.Policy.Configuration));
        Assert.AreEqual(1.25m, InvariantScalarCodec.Parse("1.25", typeof(decimal), InvariantScalarCodec.Policy.Configuration));
    }

    private sealed class SharedMemberSample
    {
        [Required]
        [PropertyName("CUSTOM_KEY")]
        public int Count { get; set; }

        [Bodu.Text.Serialization.Ignore]
        public string? NotIncluded { get; set; }

        public int PublicField;
    }

    [TestMethod]
    public void MemberDiscovery_WhenIncludeFieldsAndRequiredAttributes_ShouldPreserveMetadata()
    {
        var declarations = FlatMemberDiscovery.Enumerate(typeof(SharedMemberSample), includeFields: true).ToArray();
        Assert.AreEqual(2, declarations.Length);
        FlatMemberDescriptor? property = null;
        foreach (var declaration in declarations)
        {
            if (declaration.Name == nameof(SharedMemberSample.Count))
                property = new SharedMemberProbe(declaration);
        }
        Assert.IsNotNull(property);
        Assert.AreEqual("CUSTOM_KEY", property.Name);
        Assert.IsTrue(property.Required);
        Assert.IsTrue(property.CanRead);
        Assert.IsTrue(property.CanWrite);
    }

    private sealed class SharedMemberProbe : FlatMemberDescriptor
    {
        public SharedMemberProbe(System.Reflection.MemberInfo member) : base(member, static name => name) { }
    }
}
