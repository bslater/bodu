// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundEntryBuilderTests.Sinks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.IO.Compound.Builders;

/// <summary>
/// Verifies the alternate authoring sinks and text conveniences of the builder model.
/// </summary>
/// <remarks>
/// The <see cref="IBufferWriter{T}" /> serialization sink and the text-based <c>SetContent</c> overload exist so a
/// caller who already owns a buffer or a string does not have to materialize a byte array first. Each is asserted
/// against the primary path it shortcuts, so a divergence shows up as a mismatch rather than as a subtly different
/// container.
/// </remarks>
public partial class CompoundEntryBuilderTests
{
    /// <summary>
    /// Verifies that serializing to an <see cref="IBufferWriter{T}" /> produces the same bytes as
    /// <see cref="CompoundStorageBuilder.ToArray(CompoundBuildOptions)" />.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenSinkIsBufferWriter_ShouldMatchToArray()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Data", new byte[] { 1, 2, 3 });

        var writer = new ArrayBufferWriter<byte>();
        root.WriteTo(writer);

        CollectionAssert.AreEqual(root.ToArray(), writer.WrittenSpan.ToArray());
    }

    /// <summary>
    /// Verifies that the buffer-writer sink rejects a <see langword="null" /> destination before serializing.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenBufferWriterIsNull_ShouldThrowArgumentNullException()
    {
        var root = CompoundStorageBuilder.CreateRoot();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            root.WriteTo((IBufferWriter<byte>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamBuilder.SetContent(string, Encoding)" /> encodes as UTF-8 by default
    /// and honors an explicit encoding, so a caller can author a text stream in the encoding the consuming format
    /// expects.
    /// </summary>
    [TestMethod]
    public void SetContent_WhenTextSupplied_ShouldEncodeWithTheRequestedEncoding()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Text", ReadOnlyMemory<byte>.Empty);

        stream.SetContent("café");
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("café"), stream.Content.ToArray());

        stream.SetContent("café", Encoding.Unicode);
        CollectionAssert.AreEqual(Encoding.Unicode.GetBytes("café"), stream.Content.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStreamBuilder.SetContent(string, Encoding)" /> rejects
    /// <see langword="null" /> text rather than staging an empty payload.
    /// </summary>
    /// <remarks>
    /// The <see langword="null" /> is cast deliberately: an untyped <c>null</c> binds to the
    /// <see cref="ReadOnlySpan{T}" /> overload instead, which clears the payload rather than throwing. That is
    /// ordinary overload resolution rather than a defect, but it means the guard is only reachable when the argument
    /// is typed as a string.
    /// </remarks>
    [TestMethod]
    public void SetContent_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        var root = CompoundStorageBuilder.CreateRoot();
        CompoundStreamBuilder stream = root.AddStream("Text", ReadOnlyMemory<byte>.Empty);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            stream.SetContent((string)null!);
        });
    }

    /// <summary>
    /// Verifies that an empty entry name is rejected. An empty name has no valid directory encoding, so it is
    /// refused even though names carrying control characters - such as the summary-information streams - are not.
    /// </summary>
    [TestMethod]
    public void AddStream_WhenNameIsEmpty_ShouldThrowExactly()
    {
        var root = CompoundStorageBuilder.CreateRoot();

        _ = Assert.ThrowsExactly<CompoundFileSerializationException>(() =>
        {
            _ = root.AddStream(string.Empty, new byte[] { 1 });
        });
    }
}
