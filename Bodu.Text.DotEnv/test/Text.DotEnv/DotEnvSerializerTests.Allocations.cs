// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializerTests.Allocations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Contains the allocation tests for <see cref="DotEnvSerializer" />, which pin the bytes a call allocates, counted with
/// <see cref="GC.GetAllocatedBytesForCurrentThread" /> after warm-up calls, so that they do not depend on timing.
/// </summary>
public partial class DotEnvSerializerTests
{
    /// <summary>
    /// A POCO of ten text members, which an allocation test serializes beside a dictionary of the same entries.
    /// </summary>
    public sealed class TenMemberConfig
    {
        /// <summary>Gets or sets the value of key <c>A</c>.</summary>
        /// <value>The value.</value>
        public string A { get; set; } = "a";

        /// <summary>Gets or sets the value of key <c>B</c>.</summary>
        /// <value>The value.</value>
        public string B { get; set; } = "b";

        /// <summary>Gets or sets the value of key <c>C</c>.</summary>
        /// <value>The value.</value>
        public string C { get; set; } = "c";

        /// <summary>Gets or sets the value of key <c>D</c>.</summary>
        /// <value>The value.</value>
        public string D { get; set; } = "d";

        /// <summary>Gets or sets the value of key <c>E</c>.</summary>
        /// <value>The value.</value>
        public string E { get; set; } = "e";

        /// <summary>Gets or sets the value of key <c>F</c>.</summary>
        /// <value>The value.</value>
        public string F { get; set; } = "f";

        /// <summary>Gets or sets the value of key <c>G</c>.</summary>
        /// <value>The value.</value>
        public string G { get; set; } = "g";

        /// <summary>Gets or sets the value of key <c>H</c>.</summary>
        /// <value>The value.</value>
        public string H { get; set; } = "h";

        /// <summary>Gets or sets the value of key <c>I</c>.</summary>
        /// <value>The value.</value>
        public string I { get; set; } = "i";

        /// <summary>Gets or sets the value of key <c>J</c>.</summary>
        /// <value>The value.</value>
        public string J { get; set; } = "j";

        /// <summary>
        /// Creates a dictionary of the same keys and values as the members.
        /// </summary>
        /// <returns>The dictionary.</returns>
        public Dictionary<string, string> ToDictionary() => new()
        {
            ["A"] = A, ["B"] = B, ["C"] = C, ["D"] = D, ["E"] = E, ["F"] = F, ["G"] = G, ["H"] = H, ["I"] = I, ["J"] = J,
        };
    }

    /// <summary>
    /// Verifies that serializing a POCO of ten text members allocates no more than serializing a dictionary of the same
    /// entries, within 1 KiB, rather than reflecting over the POCO's members on every call.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenThePocoHasTenMembers_ShouldAllocateNoMoreThanForADictionaryOfTheSameEntries()
    {
        var poco = new TenMemberConfig();
        Dictionary<string, string> dictionary = poco.ToDictionary();

        long pocoBytes = MeasureAllocations(() => DotEnvSerializer.Serialize(poco));
        long dictionaryBytes = MeasureAllocations(() => DotEnvSerializer.Serialize(dictionary));

        Assert.IsLessThanOrEqualTo(dictionaryBytes + 1024, pocoBytes, $"POCO: {pocoBytes} B; dictionary: {dictionaryBytes} B; .NET {Environment.Version}.");
    }

    /// <summary>
    /// Calls an action three times to warm up and once measured, and returns the bytes the measured call allocated on
    /// the current thread.
    /// </summary>
    /// <param name="action">The action to measure.</param>
    /// <returns>The bytes allocated by the measured call.</returns>
    /// <remarks>
    /// Reflection generates the code of a property's getter or setter on its second call, so the warm-up calls each
    /// accessor more than once and the measured call sees only the steady state.
    /// </remarks>
    private static long MeasureAllocations(Action action)
    {
        for (int i = 0; i < 3; i++)
            action();

        long before = GC.GetAllocatedBytesForCurrentThread();
        action();

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
