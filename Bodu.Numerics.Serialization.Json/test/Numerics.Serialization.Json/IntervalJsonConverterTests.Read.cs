// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalJsonConverterTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;

namespace Bodu.Numerics.Serialization.Json;

public partial class IntervalJsonConverterTests
{
    /// <summary>The Strict object form of a bounded interval whose lower endpoint is NaN.</summary>
    private const string NaNLowerEndpointJson =
        "{\"lower\":\"NaN\",\"upper\":1,\"lowerInclusive\":true,\"upperInclusive\":true}";

    /// <summary>
    /// Verifies that reading the object form under the Strict and Lenient policies rejects a NaN endpoint, on a bounded
    /// or a half-bounded interval, with a <see cref="JsonException" /> rather than letting the interval's
    /// <see cref="ArgumentException" /> escape.
    /// </summary>
    /// <param name="policy">The object policy under test.</param>
    /// <param name="json">The JSON payload whose endpoint is NaN.</param>
    [TestMethod]
    [DataRow(NumericsJsonPolicy.Strict, "{\"lower\":\"NaN\",\"upper\":1,\"lowerInclusive\":true,\"upperInclusive\":true}", DisplayName = "Strict, bounded, lower NaN")]
    [DataRow(NumericsJsonPolicy.Strict, "{\"lower\":0,\"upper\":\"NaN\",\"lowerInclusive\":true,\"upperInclusive\":false}", DisplayName = "Strict, bounded, upper NaN")]
    [DataRow(NumericsJsonPolicy.Strict, "{\"lower\":\"NaN\",\"upperUnbounded\":true,\"lowerInclusive\":true}", DisplayName = "Strict, upper unbounded, lower NaN")]
    [DataRow(NumericsJsonPolicy.Strict, "{\"lowerUnbounded\":true,\"upper\":\"NaN\",\"upperInclusive\":false}", DisplayName = "Strict, lower unbounded, upper NaN")]
    [DataRow(NumericsJsonPolicy.Lenient, "{\"min\":\"NaN\",\"max\":1}", DisplayName = "Lenient, bounded, lower NaN")]
    [DataRow(NumericsJsonPolicy.Lenient, "{\"lower\":0,\"upper\":\"nan\",\"upperInclusive\":false}", DisplayName = "Lenient, bounded, upper NaN")]
    [DataRow(NumericsJsonPolicy.Lenient, "{\"lower\":\"NaN\",\"upperUnbounded\":true}", DisplayName = "Lenient, upper unbounded, lower NaN")]
    [DataRow(NumericsJsonPolicy.Lenient, "{\"lowerUnbounded\":true,\"max\":\"NaN\"}", DisplayName = "Lenient, lower unbounded, upper NaN")]
    public void Read_WhenObjectEndpointIsNaN_ShouldThrowJsonException(NumericsJsonPolicy policy, string json)
    {
        JsonSerializerOptions options = new JsonSerializerOptions().AddNumericsJsonConverters(policy);

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<double>>(json, options);
        });
    }

    /// <summary>
    /// Verifies that reading the object form rejects a NaN endpoint of an <see cref="Interval{T}" /> over
    /// <see cref="float" /> with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenObjectEndpointIsNaN_ForSingle_ShouldThrowJsonException()
    {
        JsonSerializerOptions options = Options();

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<float>>(NaNLowerEndpointJson, options);
        });
    }

    /// <summary>
    /// Verifies that reading the object form rejects a NaN endpoint of an <see cref="Interval{T}" /> over
    /// <see cref="Half" /> with a <see cref="JsonException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenObjectEndpointIsNaN_ForHalf_ShouldThrowJsonException()
    {
        JsonSerializerOptions options = Options();

        _ = Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Interval<Half>>(NaNLowerEndpointJson, options);
        });
    }
}
