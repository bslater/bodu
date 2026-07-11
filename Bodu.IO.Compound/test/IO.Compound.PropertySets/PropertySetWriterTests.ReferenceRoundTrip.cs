// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PropertySetWriterTests.ReferenceRoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound.PropertySets;

public partial class PropertySetWriterTests
{
    /// <summary>
    /// Verifies that every OLE property-set stream in the reference corpus survives a parse, re-emit, and re-parse
    /// cycle with identical sections, names, and property values.
    /// </summary>
    /// <param name="kat">The reference-fixture row under test.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(CompoundReferenceManifest.ValidFixtures),
        typeof(CompoundReferenceManifest),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Write_WhenReferencePropertySetReEmitted_ShouldRoundTripValues(CompoundReferenceFixtureKat kat)
    {
        using var file = CompoundFile.Open(CompoundFixtures.OpenReference(kat.RelativePath));

        foreach (byte[] payload in CollectPropertySetStreams(file.RootStorage))
        {
            OlePropertySet original;
            try
            {
                original = OlePropertySet.Parse(payload);
            }
            catch (CompoundFileFormatException)
            {
                // A property-named stream the reader itself rejects is outside the writer's symmetry contract.
                continue;
            }

            var reparsed = OlePropertySet.Parse(original.ToArray());
            AssertSetsEqual(original, reparsed);
        }
    }

    /// <summary>
    /// Recursively collects the payloads of all property-set streams (names starting with <c>\u0005</c>).
    /// </summary>
    /// <param name="storage">The storage to walk.</param>
    /// <returns>The property-set stream payloads.</returns>
    private static IEnumerable<byte[]> CollectPropertySetStreams(CompoundStorage storage)
    {
        foreach (CompoundEntryInfo info in storage.EnumerateStreams())
        {
            if (!info.Name.StartsWith('\u0005'))
                continue;

            using CompoundStream stream = storage.OpenStream(info.Name);
            yield return stream.ReadAllBytes();
        }

        foreach (CompoundStorage child in storage.EnumerateStorages())
        {
            foreach (byte[] payload in CollectPropertySetStreams(child))
                yield return payload;
        }
    }

    /// <summary>
    /// Asserts that two parsed property sets carry identical sections, names, and property values.
    /// </summary>
    /// <param name="expected">The originally parsed set.</param>
    /// <param name="actual">The set parsed from the re-emitted bytes.</param>
    private static void AssertSetsEqual(OlePropertySet expected, OlePropertySet actual)
    {
        Assert.AreEqual(expected.FormatId, actual.FormatId);
        Assert.AreEqual(expected.Sections.Count, actual.Sections.Count);

        for (int i = 0; i < expected.Sections.Count; i++)
        {
            OlePropertySection expectedSection = expected.Sections[i];
            OlePropertySection actualSection = actual.Sections[i];

            Assert.AreEqual(expectedSection.FormatId, actualSection.FormatId);
            Assert.AreEqual(expectedSection.CodePage, actualSection.CodePage);
            CollectionAssert.AreEquivalent(
                expectedSection.PropertyNames.ToList(),
                actualSection.PropertyNames.ToList());

            // The writer always materializes the code-page property (PID 1), so compare values over the
            // originally present identifiers excluding the code page and dictionary bookkeeping.
            foreach (KeyValuePair<int, OlePropertyValue> property in expectedSection.Properties)
            {
                if (property.Key is 0 or 1)
                    continue;

                Assert.IsTrue(
                    actualSection.TryGetValue(property.Key, out OlePropertyValue? actualValue),
                    $"Property {property.Key} missing after re-emit.");
                Assert.AreEqual(property.Value.Type, actualValue.Type, $"Property {property.Key} type drifted.");
                Assert.AreEqual(property.Value.IsVector, actualValue.IsVector, $"Property {property.Key} vector flag drifted.");
                AssertValuesEqual(property.Value.Value, actualValue.Value, $"Property {property.Key}");
            }
        }
    }

    /// <summary>
    /// Asserts CLR value equality, recursing into vectors and comparing blobs by content.
    /// </summary>
    /// <param name="expected">The expected value.</param>
    /// <param name="actual">The actual value.</param>
    /// <param name="context">The assertion context used in failure messages.</param>
    private static void AssertValuesEqual(object? expected, object? actual, string context)
    {
        switch (expected)
        {
            case null:
                Assert.IsNull(actual, $"{context}: expected null.");
                break;

            case byte[] expectedBytes:
                CollectionAssert.AreEqual(expectedBytes, (byte[])actual!, $"{context}: blob bytes drifted.");
                break;

            case object?[] expectedItems:
                var actualItems = (object?[])actual!;
                Assert.AreEqual(expectedItems.Length, actualItems.Length, $"{context}: element count drifted.");
                for (int i = 0; i < expectedItems.Length; i++)
                    AssertValuesEqual(expectedItems[i], actualItems[i], $"{context}[{i}]");
                break;

            default:
                Assert.AreEqual(expected, actual, $"{context}: value drifted.");
                break;
        }
    }
}
