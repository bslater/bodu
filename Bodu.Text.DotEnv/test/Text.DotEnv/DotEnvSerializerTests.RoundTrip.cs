// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Contains the serialize/deserialize round-trip tests for <see cref="DotEnvSerializer" />.
/// </summary>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// Verifies that a POCO survives a serialize/deserialize round-trip with all mapped members preserved.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenPoco_ShouldPreserveMappedMembers()
    {
        var original = new ServerConfig { Host = "db.internal", Port = 1234, UseTls = false };

        string text = DotEnvSerializer.Serialize(original);
        ServerConfig restored = DotEnvSerializer.Deserialize<ServerConfig>(text);

        Assert.AreEqual(original.Host, restored.Host);
        Assert.AreEqual(original.Port, restored.Port);
        Assert.AreEqual(original.UseTls, restored.UseTls);
    }

    /// <summary>
    /// Verifies that the Web defaults emit and match <c>SCREAMING_SNAKE_CASE</c> keys across a round-trip.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenWebDefaults_ShouldUseSnakeCaseUpperKeys()
    {
        var options = new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web);
        var original = new CallbackConfig { Name = "value" };

        string text = DotEnvSerializer.Serialize(original, options);
        Assert.IsTrue(text.StartsWith("NAME=value", StringComparison.Ordinal));

        CallbackConfig restored = DotEnvSerializer.Deserialize<CallbackConfig>(text, options);
        Assert.AreEqual("value", restored.Name);
    }

    /// <summary>
    /// Verifies that the asynchronous stream overloads round-trip a POCO.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsyncDeserializeAsync_WhenPoco_ShouldRoundTrip()
    {
        var original = new ServerConfig { Host = "h", Port = 9, UseTls = true };

        using var stream = new MemoryStream();
        await DotEnvSerializer.SerializeAsync(stream, original);
        stream.Position = 0;
        ServerConfig restored = await DotEnvSerializer.DeserializeAsync<ServerConfig>(stream);

        Assert.AreEqual(original.Host, restored.Host);
        Assert.AreEqual(original.Port, restored.Port);
        Assert.AreEqual(original.UseTls, restored.UseTls);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnly" />, <see cref="TimeOnly" />, 128-bit, half-precision and
    /// <see cref="System.Numerics.BigInteger" /> members, and nullable forms, read back as the values written.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenMembersAreTheExtendedScalars_ShouldRoundTripThem()
    {
        var original = new ExtendedScalarRecord
        {
            Day = new DateOnly(2026, 1, 2),
            Time = new TimeOnly(13, 45, 30).Add(TimeSpan.FromTicks(1234567)),
            Large = Int128.MinValue,
            Unsigned = UInt128.MaxValue,
            Small = (Half)0.1,
            Big = -(System.Numerics.BigInteger.Pow(10, 40) + 1),
            MaybeLarge = null,
            MaybeTime = new TimeOnly(23, 59, 59),
        };

        string text = DotEnvSerializer.Serialize(original);
        ExtendedScalarRecord restored = DotEnvSerializer.Deserialize<ExtendedScalarRecord>(text);

        Assert.AreEqual(original.Day, restored.Day);
        Assert.AreEqual(original.Time, restored.Time);
        Assert.AreEqual(original.Large, restored.Large);
        Assert.AreEqual(original.Unsigned, restored.Unsigned);
        Assert.AreEqual(original.Small, restored.Small);
        Assert.AreEqual(original.Big, restored.Big);
        Assert.IsNull(restored.MaybeLarge);
        Assert.AreEqual(original.MaybeTime, restored.MaybeTime);
    }

    /// <summary>
    /// Verifies that a POCO whose member hides a base member with <see langword="new" /> reads back the text it is
    /// serialized to, the hiding member keeping its value.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAMemberHidesABaseMemberWithNew_ShouldReadBackTheOutput()
    {
        var original = new PortNumberHidingConfig { Port = 5 };
        ((PortTextBase)original).Port = "x";

        string text = DotEnvSerializer.Serialize(original);
        PortNumberHidingConfig restored = DotEnvSerializer.Deserialize<PortNumberHidingConfig>(text);

        Assert.AreEqual(5, restored.Port);
    }
}
