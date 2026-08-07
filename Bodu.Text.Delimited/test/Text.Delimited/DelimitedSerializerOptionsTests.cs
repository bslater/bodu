// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Delimited;

/// <summary>
/// Verifies the option surface of <see cref="DelimitedSerializerOptions" />: its defaults, that every setting round
/// trips, and that the shared <see cref="DelimitedSerializerOptions.Default" /> instance is frozen.
/// </summary>
[TestClass]
public sealed class DelimitedSerializerOptionsTests
{
    /// <summary>
    /// Verifies that a new instance carries the general-purpose defaults. <see cref="DelimitedSerializerOptions.Delimiter" />
    /// and <see cref="DelimitedSerializerOptions.Quote" /> default to <c>'\0'</c>, the documented sentinel meaning
    /// "use the format default", rather than to the characters themselves.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultConstructed_ShouldCarryGeneralDefaults()
    {
        var options = new DelimitedSerializerOptions();

        Assert.AreEqual('\0', options.Delimiter);
        Assert.AreEqual('\0', options.Quote);
        Assert.IsFalse(options.NoHeader);
        Assert.IsFalse(options.PropertyNameCaseInsensitive);
        Assert.IsFalse(options.IncludeFields);
        Assert.IsNull(options.PropertyNamingPolicy);
        Assert.IsFalse(options.IsReadOnly);
    }

    /// <summary>
    /// Verifies that the unset delimiter sentinel resolves to a comma on the write path, so a default-constructed
    /// options object produces RFC 4180 output rather than NUL-separated fields.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDelimiterUnset_ShouldResolveSentinelToComma()
    {
        string csv = DelimitedSerializer.Serialize(new[] { new Row { Name = "alpha", Value = 1 } }, new DelimitedSerializerOptions());

        Assert.Contains("Name,Value", csv);
        Assert.Contains("alpha,1", csv);
    }

    /// <summary>
    /// Verifies that the <see cref="DelimitedSerializerDefaults.Web" /> preset applies snake_case naming and
    /// case-insensitive property matching.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWebDefaults_ShouldApplySnakeCaseAndCaseInsensitiveMatching()
    {
        var options = new DelimitedSerializerOptions(DelimitedSerializerDefaults.Web);

        Assert.AreSame(NamingPolicy.SnakeCaseLower, options.PropertyNamingPolicy);
        Assert.IsTrue(options.PropertyNameCaseInsensitive);
    }

    /// <summary>
    /// Verifies that the <see cref="DelimitedSerializerDefaults.General" /> preset leaves naming untouched.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGeneralDefaults_ShouldLeaveNamingUnset()
    {
        var options = new DelimitedSerializerOptions(DelimitedSerializerDefaults.General);

        Assert.IsNull(options.PropertyNamingPolicy);
        Assert.IsFalse(options.PropertyNameCaseInsensitive);
    }

    /// <summary>
    /// A minimal record used by the write-path assertions.
    /// </summary>
    private sealed class Row
    {
        /// <summary>Gets or sets the name column.</summary>
        /// <value>The name.</value>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the value column.</summary>
        /// <value>The value.</value>
        public int Value { get; set; }
    }

    /// <summary>
    /// Verifies that every mutable setting round trips through its property.
    /// </summary>
    [TestMethod]
    public void Properties_WhenSetOnMutableInstance_ShouldRoundTrip()
    {
        var options = new DelimitedSerializerOptions
        {
            Delimiter = '\t',
            Quote = '\'',
            NoHeader = true,
            PropertyNameCaseInsensitive = true,
            IncludeFields = true,
            PropertyNamingPolicy = NamingPolicy.CamelCase,
        };

        Assert.AreEqual('\t', options.Delimiter);
        Assert.AreEqual('\'', options.Quote);
        Assert.IsTrue(options.NoHeader);
        Assert.IsTrue(options.PropertyNameCaseInsensitive);
        Assert.IsTrue(options.IncludeFields);
        Assert.AreSame(NamingPolicy.CamelCase, options.PropertyNamingPolicy);
    }

    /// <summary>
    /// Verifies that the shared default instance is read-only, so callers cannot mutate the options every
    /// serializer call falls back to.
    /// </summary>
    [TestMethod]
    public void Default_ShouldBeReadOnly()
    {
        Assert.IsTrue(DelimitedSerializerOptions.Default.IsReadOnly);
    }

    /// <summary>
    /// Verifies that every setter on the read-only default instance throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReadOnlyMutations))]
    public void Setters_WhenInstanceIsReadOnly_ShouldThrowExactly(string testName, Action<DelimitedSerializerOptions> mutate)
    {
        Assert.IsNotNull(testName);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            mutate(DelimitedSerializerOptions.Default);
        });
    }

    /// <summary>
    /// Gets the per-setter mutations applied against the read-only default instance.
    /// </summary>
    /// <value>The mutation cases.</value>
    public static IEnumerable<object[]> ReadOnlyMutations =>
    [
        ["Delimiter", (Action<DelimitedSerializerOptions>)(o => o.Delimiter = ';')],
        ["Quote", (Action<DelimitedSerializerOptions>)(o => o.Quote = '\'')],
        ["NoHeader", (Action<DelimitedSerializerOptions>)(o => o.NoHeader = true)],
        ["PropertyNameCaseInsensitive", (Action<DelimitedSerializerOptions>)(o => o.PropertyNameCaseInsensitive = true)],
        ["IncludeFields", (Action<DelimitedSerializerOptions>)(o => o.IncludeFields = true)],
        ["PropertyNamingPolicy", (Action<DelimitedSerializerOptions>)(o => o.PropertyNamingPolicy = NamingPolicy.CamelCase)],
    ];
}
