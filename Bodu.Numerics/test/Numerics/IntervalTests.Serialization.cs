// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTests.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Numerics.Serialization;

namespace Bodu.Numerics;

public partial class IntervalTests
{
    /// <summary>
    /// Verifies that the parameterless <see cref="IntervalJsonConverter{T}" /> constructor selects the canonical
    /// Strict object shape.
    /// </summary>
    [TestMethod]
    public void IntervalJsonConverter_WhenConstructedWithoutPolicy_ShouldUseStrictObjectShape()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new IntervalJsonConverter<int>());

        Assert.AreEqual(
            "{\"lower\":1,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":false}",
            JsonSerializer.Serialize(Interval<int>.ClosedOpen(1, 5), options));
    }

    /// <summary>
    /// Verifies that deserializing the canonical object form rejects malformed payloads — a duplicate property, a
    /// missing required property, or a non-numeric endpoint — with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    [DataRow("{\"lower\":1,\"upper\":2,\"upper\":3,\"lowerInclusive\":true,\"upperInclusive\":true}", DisplayName = "Duplicate upper")]
    [DataRow("{\"lower\":1,\"upper\":5,\"lowerInclusive\":true,\"lowerInclusive\":false,\"upperInclusive\":true}", DisplayName = "Duplicate lowerInclusive")]
    [DataRow("{\"lower\":1,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":true,\"upperInclusive\":false}", DisplayName = "Duplicate upperInclusive")]
    [DataRow("{\"empty\":true,\"empty\":true}", DisplayName = "Duplicate empty marker")]
    [DataRow("{\"lower\":1,\"lowerInclusive\":true,\"upperInclusive\":true}", DisplayName = "Missing upper")]
    [DataRow("{\"lower\":1,\"upper\":5,\"lowerInclusive\":true}", DisplayName = "Missing upperInclusive under Strict")]
    [DataRow("{\"lower\":true,\"upper\":5,\"lowerInclusive\":true,\"upperInclusive\":true}", DisplayName = "Lower is not a number")]
    public void IntervalJsonConverter_WhenObjectPayloadIsMalformed_ShouldThrowJsonException(string json)
    {
        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<int>>(json);
        });
    }
}
