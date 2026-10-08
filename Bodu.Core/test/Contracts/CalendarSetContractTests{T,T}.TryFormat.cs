// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.TryFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using System.Text.Unicode;

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that writing every sample set into a span of characters, in every format and the default, writes the
    /// text <c>ToString</c> returns, both into a span of exactly that length and into a longer one.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationFits_ShouldWriteTheTextToStringReturns()
    {
        foreach (TSet set in SampleSets())
        {
            foreach (string format in FormatsAndDefault)
            {
                string expected = set.ToString(format, null);
                char[] exact = new char[expected.Length];
                char[] longer = new char[expected.Length + 8];

                Assert.IsTrue(set.TryFormat(exact, out int written, format, null), $"{expected}, '{format}'");
                Assert.AreEqual(expected.Length, written, $"{expected}, '{format}'");
                Assert.AreEqual(expected, new string(exact), $"'{format}'");

                Assert.IsTrue(set.TryFormat(longer, out written, format, null), $"{expected}, '{format}', longer");
                Assert.AreEqual(expected, new string(longer, 0, written), $"'{format}', longer");
            }
        }
    }

    /// <summary>
    /// Verifies that writing a set into a span of characters one character too short returns
    /// <see langword="false" />, reports nothing written, and leaves the span unchanged.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationIsOneCharacterShort_ShouldReturnFalseAndLeaveItUnchanged()
    {
        foreach (TSet set in SampleSets())
        {
            foreach (string format in FormatsAndDefault)
            {
                string expected = set.ToString(format, null);
                if (expected.Length == 0)
                    continue;

                char[] destination = Enumerable.Repeat('￿', expected.Length - 1).ToArray();

                Assert.IsFalse(set.TryFormat(destination, out int written, format, null), $"{expected}, '{format}'");
                Assert.AreEqual(0, written, $"{expected}, '{format}'");
                Assert.IsTrue(destination.All(c => c == '￿'), $"{expected}, '{format}' wrote into the destination.");
            }
        }
    }

    /// <summary>
    /// Verifies that writing a set whose text is not empty into an empty span of characters returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationIsEmpty_ShouldReturnFalseUnlessTheTextIsEmpty()
    {
        foreach (TSet set in SampleSets())
        {
            bool expected = set.ToString().Length == 0;

            Assert.AreEqual(expected, set.TryFormat(Span<char>.Empty, out int written, default, null), set.ToString());
            Assert.AreEqual(0, written, set.ToString());
        }
    }

    /// <summary>
    /// Verifies that writing every sample set into a span of UTF-8 bytes, in every format and the default, writes the
    /// ASCII bytes of the text <c>ToString</c> returns.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenUtf8DestinationFits_ShouldWriteTheTextToStringReturns()
    {
        foreach (TSet set in SampleSets())
        {
            foreach (string format in FormatsAndDefault)
            {
                byte[] expected = Encoding.ASCII.GetBytes(set.ToString(format, null));
                byte[] exact = new byte[expected.Length];

                Assert.IsTrue(set.TryFormat(exact, out int written, format, null), $"'{format}'");
                Assert.AreEqual(expected.Length, written, $"'{format}'");
                CollectionAssert.AreEqual(expected, exact, $"'{format}'");
            }
        }
    }

    /// <summary>
    /// Verifies that writing a set into a span of UTF-8 bytes one byte too short returns <see langword="false" />,
    /// reports nothing written, and leaves the span unchanged.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenUtf8DestinationIsOneByteShort_ShouldReturnFalseAndLeaveItUnchanged()
    {
        foreach (TSet set in SampleSets())
        {
            foreach (string format in FormatsAndDefault)
            {
                string expected = set.ToString(format, null);
                if (expected.Length == 0)
                    continue;

                byte[] destination = Enumerable.Repeat((byte)0xFF, expected.Length - 1).ToArray();

                Assert.IsFalse(set.TryFormat(destination, out int written, format, null), $"{expected}, '{format}'");
                Assert.AreEqual(0, written, $"{expected}, '{format}'");
                Assert.IsTrue(destination.All(b => b == 0xFF), $"{expected}, '{format}' wrote into the destination.");
            }
        }
    }

    /// <summary>
    /// Verifies that writing a set in a format no calendar value set accepts throws the
    /// <see cref="FormatException" /> <c>ToString</c> throws, with the same message, for characters and for UTF-8.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenFormatIsUnsupported_ShouldThrowTheFormatExceptionToStringThrows()
    {
        foreach (string format in UnsupportedFormats)
        {
            var expected = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = All.ToString(format, null);
            }, format);

            var chars = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = All.TryFormat(new char[300], out _, format, null);
            }, format);

            var bytes = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = All.TryFormat(new byte[300], out _, format, null);
            }, format);

            Assert.AreEqual(expected.Message, chars.Message, format);
            Assert.AreEqual(expected.Message, bytes.Message, format);
        }
    }

    /// <summary>
    /// Verifies that a set in an interpolated string, which goes through <c>TryFormat</c>, writes the text
    /// <c>ToString</c> returns, with and without a format, into a string and into UTF-8.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenSetIsInterpolated_ShouldWriteTheTextToStringReturns()
    {
        byte[] utf8 = new byte[300];

        foreach (TSet set in SampleSets())
        {
            Assert.AreEqual(set.ToString(), $"{set}");
            Assert.AreEqual(set.ToString("B", null), $"{set:B}");

            Assert.IsTrue(Utf8.TryWrite(utf8, $"[{set:B}]", out int written));
            Assert.AreEqual($"[{set.ToString("B", null)}]", Encoding.ASCII.GetString(utf8, 0, written));
        }
    }

    /// <summary>
    /// Verifies that writing a set into spans of characters and of UTF-8 bytes, in every format and the default,
    /// allocates nothing on the managed heap.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenWritingEveryFormat_ShouldAllocateNothing()
    {
        TSet set = SampleSets().Last();
        string[] formats = [.. FormatsAndDefault];
        char[] chars = new char[CalendarValueSet.MaxTextLength];
        byte[] bytes = new byte[CalendarValueSet.MaxTextLength];
        WriteEveryFormat(set, formats, chars, bytes);

        long before = GC.GetAllocatedBytesForCurrentThread();
        WriteEveryFormat(set, formats, chars, bytes);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.AreEqual(0L, allocated, $"TryFormat allocated {allocated} bytes.");
    }

    /// <summary>
    /// Writes a set in each of the specified formats into a span of characters and into a span of UTF-8 bytes.
    /// </summary>
    /// <param name="set">The set to write.</param>
    /// <param name="formats">The formats.</param>
    /// <param name="chars">The character span to write into.</param>
    /// <param name="bytes">The byte span to write into.</param>
    private static void WriteEveryFormat(TSet set, string[] formats, Span<char> chars, Span<byte> bytes)
    {
        for (int i = 0; i < formats.Length; i++)
        {
            _ = set.TryFormat(chars, out _, formats[i], null);
            _ = set.TryFormat(bytes, out _, formats[i], null);
        }
    }
}
