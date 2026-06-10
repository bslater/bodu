// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationEngineTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Infrastructure;

namespace Bodu.Text.Serialization;

/// <summary>
/// Verifies the end-to-end serialize/deserialize behavior of the engine over the in-memory token format.
/// </summary>
[TestClass]
public partial class SerializationEngineTests
{
    /// <summary>
    /// Verifies that a string value round-trips through the engine unchanged.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void RoundTrip_WhenValueIsString_ShouldPreserveValue() =>
        Assert.AreEqual("hello", FakeFormat.RoundTrip("hello"));

    /// <summary>
    /// Verifies that the scalar value types round-trip through the engine unchanged.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsScalar_ShouldPreserveValue()
    {
        Assert.AreEqual(42, FakeFormat.RoundTrip(42));
        Assert.AreEqual(true, FakeFormat.RoundTrip(true));
        Assert.AreEqual(3.5, FakeFormat.RoundTrip(3.5));
        Assert.AreEqual(byte.MaxValue, FakeFormat.RoundTrip(byte.MaxValue));
        Assert.AreEqual(long.MinValue, FakeFormat.RoundTrip(long.MinValue));
        Assert.AreEqual('x', FakeFormat.RoundTrip('x'));
    }

    /// <summary>
    /// Verifies that an enumeration value round-trips by name.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsEnum_ShouldPreserveValue() =>
        Assert.AreEqual(DayOfWeek.Wednesday, FakeFormat.RoundTrip(DayOfWeek.Wednesday));

    /// <summary>
    /// Verifies that a <see cref="Guid" /> value round-trips through its string form.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsGuid_ShouldPreserveValue()
    {
        var value = Guid.NewGuid();
        Assert.AreEqual(value, FakeFormat.RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a <see cref="DateTimeOffset" /> value round-trips unchanged.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsDateTimeOffset_ShouldPreserveValue()
    {
        var value = new DateTimeOffset(2026, 6, 10, 7, 32, 0, TimeSpan.FromHours(10));
        Assert.AreEqual(value, FakeFormat.RoundTrip(value));
    }

    /// <summary>
    /// Verifies that a nullable value type round-trips both its present and absent states.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsNullable_ShouldPreserveValue()
    {
        Assert.AreEqual(7, FakeFormat.RoundTrip<int?>(7));
        Assert.IsNull(FakeFormat.RoundTrip<int?>(null));
    }

    /// <summary>
    /// Verifies that a mutable object with a parameterless constructor round-trips through its setters.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void RoundTrip_WhenValueIsMutableObject_ShouldPreserveMembers()
    {
        var person = new MutablePerson { Name = "Tom", Age = 45, Active = true };

        MutablePerson result = FakeFormat.RoundTrip(person);

        Assert.AreEqual("Tom", result.Name);
        Assert.AreEqual(45, result.Age);
        Assert.IsTrue(result.Active);
    }

    /// <summary>
    /// Verifies that a positional record is reconstructed through its primary constructor.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsPositionalRecord_ShouldBindConstructor()
    {
        var movie = new MovieRecord("Metropolis", 1927);

        MovieRecord result = FakeFormat.RoundTrip(movie);

        Assert.AreEqual(movie, result);
    }

    /// <summary>
    /// Verifies that a required init-only member is bound when present.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenRequiredMemberPresent_ShouldBindValue()
    {
        var model = new RequiredModel { Id = 99, Label = "alpha" };

        RequiredModel result = FakeFormat.RoundTrip(model);

        Assert.AreEqual(99, result.Id);
        Assert.AreEqual("alpha", result.Label);
    }

    /// <summary>
    /// Verifies that a list, an array, and a string-keyed dictionary round-trip within a nested graph.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenValueIsNestedGraph_ShouldPreserveCollections()
    {
        var catalog = new Catalog
        {
            Owner = new MutablePerson { Name = "Ada", Age = 36, Active = true },
            Tags = ["x", "y", "z"],
            Ports = [8000, 8001],
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal) { ["env"] = "prod" },
        };

        Catalog result = FakeFormat.RoundTrip(catalog);

        Assert.AreEqual("Ada", result.Owner.Name);
        CollectionAssert.AreEqual(catalog.Tags, result.Tags);
        CollectionAssert.AreEqual(catalog.Ports, result.Ports);
        Assert.AreEqual("prod", result.Metadata["env"]);
    }

    /// <summary>
    /// Verifies that an empty array round-trips to an empty array rather than null.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenArrayIsEmpty_ShouldRemainEmpty()
    {
        int[] result = FakeFormat.RoundTrip(Array.Empty<int>());

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Length);
    }
}
