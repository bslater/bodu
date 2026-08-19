// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.ValueTypes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the fixed-width integer surface of the serializer across the native-sized and 128-bit types: values inside
/// the writer's signed 64-bit surface emit as YAML integers, values outside it emit as their invariant text and convert
/// back exactly.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="long.MaxValue" /> writes as a plain YAML integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt64MaxValue_ShouldWriteIntegerAndRoundTrip()
    {
        string text = YamlSerializer.Serialize(long.MaxValue);

        Assert.AreEqual("9223372036854775807\n", text);
        Assert.AreEqual(long.MaxValue, YamlSerializer.Deserialize<long>(text));
    }

    /// <summary>
    /// Verifies that an unsigned 64-bit value one above <see cref="long.MaxValue" /> writes as its plain invariant
    /// text — which the reader resolves as a string — and round-trips exactly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUInt64AboveInt64Range_ShouldWriteTextAndRoundTrip()
    {
        const ulong value = (ulong)long.MaxValue + 1;

        string text = YamlSerializer.Serialize(value);

        Assert.AreEqual("9223372036854775808\n", text);
        Assert.AreEqual(value, YamlSerializer.Deserialize<ulong>(text));
    }

    /// <summary>
    /// Verifies that <see cref="ulong.MaxValue" /> writes as its plain invariant text and round-trips exactly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUInt64MaxValue_ShouldWriteTextAndRoundTrip()
    {
        string text = YamlSerializer.Serialize(ulong.MaxValue);

        Assert.AreEqual("18446744073709551615\n", text);
        Assert.AreEqual(ulong.MaxValue, YamlSerializer.Deserialize<ulong>(text));
    }

    /// <summary>
    /// Verifies that an <see cref="Int128" /> value inside the signed 64-bit range writes as a plain YAML integer and
    /// round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt128InsideInt64Range_ShouldWriteIntegerAndRoundTrip()
    {
        Int128 value = long.MaxValue;

        string text = YamlSerializer.Serialize(value);

        Assert.AreEqual("9223372036854775807\n", text);
        Assert.AreEqual(value, YamlSerializer.Deserialize<Int128>(text));
    }

    /// <summary>
    /// Verifies that an <see cref="Int128" /> value one above <see cref="long.MaxValue" /> writes as its plain
    /// invariant text and round-trips exactly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt128AboveInt64Range_ShouldWriteTextAndRoundTrip()
    {
        Int128 value = (Int128)long.MaxValue + 1;

        string text = YamlSerializer.Serialize(value);

        Assert.AreEqual("9223372036854775808\n", text);
        Assert.AreEqual(value, YamlSerializer.Deserialize<Int128>(text));
    }

    /// <summary>
    /// Verifies that <see cref="Int128.MinValue" /> — below the signed 64-bit range — writes as its quoted invariant
    /// text (the leading sign forces quoting) and round-trips exactly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt128MinValue_ShouldWriteTextAndRoundTrip()
    {
        string text = YamlSerializer.Serialize(Int128.MinValue);

        Assert.AreEqual("\"-170141183460469231731687303715884105728\"\n", text);
        Assert.AreEqual(Int128.MinValue, YamlSerializer.Deserialize<Int128>(text));
    }

    /// <summary>
    /// Verifies that <see cref="UInt128.MaxValue" /> writes as its plain invariant text and round-trips exactly.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenUInt128MaxValue_ShouldWriteTextAndRoundTrip()
    {
        string text = YamlSerializer.Serialize(UInt128.MaxValue);

        Assert.AreEqual("340282366920938463463374607431768211455\n", text);
        Assert.AreEqual(UInt128.MaxValue, YamlSerializer.Deserialize<UInt128>(text));
    }

    /// <summary>
    /// Verifies that a native-sized signed integer writes as a plain YAML integer and round-trips.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNativeInt_ShouldWriteIntegerAndRoundTrip()
    {
        nint value = -12345;

        string text = YamlSerializer.Serialize(value);

        Assert.AreEqual("-12345\n", text);
        Assert.AreEqual(value, YamlSerializer.Deserialize<nint>(text));
    }

    /// <summary>
    /// Verifies that a native-sized unsigned integer round-trips, including a 64-bit value above
    /// <see cref="long.MaxValue" /> which writes as its plain invariant text.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenNativeUInt_ShouldRoundTrip()
    {
        nuint small = 42;
        Assert.AreEqual("42\n", YamlSerializer.Serialize(small));
        Assert.AreEqual(small, YamlSerializer.Deserialize<nuint>("42\n"));

        if (UIntPtr.Size == 8)
        {
            nuint large = unchecked((nuint)ulong.MaxValue);
            string text = YamlSerializer.Serialize(large);

            Assert.AreEqual("18446744073709551615\n", text);
            Assert.AreEqual(large, YamlSerializer.Deserialize<nuint>(text));
        }
    }

    /// <summary>
    /// Verifies that a member typed <see cref="Int128" /> participates in object mapping like any other integer width.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenInt128Member_ShouldRoundTripThroughMapping()
    {
        var model = new WideModel { Wide = (Int128)long.MaxValue + 7 };

        string text = YamlSerializer.Serialize(model);

        Assert.AreEqual("Wide: 9223372036854775814\n", text);

        WideModel roundTripped = YamlSerializer.Deserialize<WideModel>(text);
        Assert.AreEqual(model.Wide, roundTripped.Wide);
    }

    /// <summary>
    /// Verifies that an out-of-range scalar read into a narrower width throws
    /// <see cref="YamlSerializationException" /> rather than truncating.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTextExceedsUInt128Range_ShouldThrowYamlSerializationException()
    {
        Assert.ThrowsExactly<YamlSerializationException>(() =>
        {
            _ = YamlSerializer.Deserialize<UInt128>("\"340282366920938463463374607431768211456\"\n");
        });
    }

    /// <summary>
    /// A model carrying an <see cref="Int128" /> member.
    /// </summary>
    private sealed class WideModel
    {
        /// <summary>
        /// Gets or sets the 128-bit value.
        /// </summary>
        /// <value>The value.</value>
        public Int128 Wide { get; set; }
    }
}
