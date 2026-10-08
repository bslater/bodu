// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CorpusEscapesTests.TryDecode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Test.Corpus;

public sealed partial class CorpusEscapesTests
{
    /// <summary>
    /// Supplies escaped fields and the bytes each stands for.
    /// </summary>
    /// <returns>One row per escape, plus plain text and a mixture.</returns>
    public static IEnumerable<object[]> ValidEscapes()
    {
        yield return [new ValidKat<string, byte[]>("empty field", string.Empty, [])];
        yield return [new ValidKat<string, byte[]>("plain text", "a=1", [(byte)'a', (byte)'=', (byte)'1'])];
        yield return [new ValidKat<string, byte[]>("line feed", @"\n", [0x0A])];
        yield return [new ValidKat<string, byte[]>("carriage return", @"\r", [0x0D])];
        yield return [new ValidKat<string, byte[]>("tab", @"\t", [0x09])];
        yield return [new ValidKat<string, byte[]>("NUL", @"\0", [0x00])];
        yield return [new ValidKat<string, byte[]>("backslash", @"\\", [(byte)'\\'])];
        yield return [new ValidKat<string, byte[]>("byte upper case", @"\xFF", [0xFF])];
        yield return [new ValidKat<string, byte[]>("byte lower case", @"\x0a", [0x0A])];
        yield return [new ValidKat<string, byte[]>("two-byte scalar", @"\u{E9}", [0xC3, 0xA9])];
        yield return [new ValidKat<string, byte[]>("BOM scalar", @"\u{FEFF}", [0xEF, 0xBB, 0xBF])];
        yield return [new ValidKat<string, byte[]>("supplementary scalar", @"\u{1F600}", [0xF0, 0x9F, 0x98, 0x80])];
        yield return [new ValidKat<string, byte[]>("highest scalar", @"\u{10FFFF}", [0xF4, 0x8F, 0xBF, 0xBF])];
        yield return [new ValidKat<string, byte[]>("mixed", @"k:\x00v\\n\n", [(byte)'k', (byte)':', 0x00, (byte)'v', (byte)'\\', (byte)'n', 0x0A])];
    }

    /// <summary>
    /// Supplies malformed escaped fields.
    /// </summary>
    /// <returns>One row per kind of malformed field.</returns>
    public static IEnumerable<object[]> InvalidEscapes()
    {
        yield return [new BinaryKat<string, bool>("trailing backslash", @"abc\", false)];
        yield return [new BinaryKat<string, bool>("unknown escape", @"\q", false)];
        yield return [new BinaryKat<string, bool>("byte with one digit", @"\xA", false)];
        yield return [new BinaryKat<string, bool>("byte with a non-hex digit", @"\xG1", false)];
        yield return [new BinaryKat<string, bool>("scalar without braces", @"\u00E9", false)];
        yield return [new BinaryKat<string, bool>("scalar without a closing brace", @"\u{E9", false)];
        yield return [new BinaryKat<string, bool>("scalar with no digits", @"\u{}", false)];
        yield return [new BinaryKat<string, bool>("scalar with seven digits", @"\u{0000041}", false)];
        yield return [new BinaryKat<string, bool>("scalar with a non-hex digit", @"\u{4G}", false)];
        yield return [new BinaryKat<string, bool>("surrogate scalar", @"\u{D800}", false)];
        yield return [new BinaryKat<string, bool>("scalar above the Unicode range", @"\u{110000}", false)];
        yield return [new BinaryKat<string, bool>("raw tab", "a\tb", false)];
        yield return [new BinaryKat<string, bool>("raw line feed", "a\nb", false)];
        yield return [new BinaryKat<string, bool>("raw non-ASCII character", "caf\u00E9", false)];
        yield return [new BinaryKat<string, bool>("raw delete character", "a\u007Fb", false)];
    }

    /// <summary>
    /// Verifies that a well-formed escaped field decodes to the bytes it stands for.
    /// </summary>
    /// <param name="kat">The field and its bytes.</param>
    [TestMethod]
    [DynamicData(nameof(ValidEscapes), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryDecode_WhenFieldIsWellFormed_ShouldReturnItsBytes(ValidKat<string, byte[]> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        bool decoded = CorpusEscapes.TryDecode(kat.Input, out byte[] bytes, out string? error);

        Assert.IsTrue(decoded, error);
        CollectionAssert.AreEqual(kat.Expected, bytes);
    }

    /// <summary>
    /// Verifies that a malformed escaped field fails to decode, with an error and no bytes.
    /// </summary>
    /// <param name="kat">The malformed field.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidEscapes), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryDecode_WhenFieldIsMalformed_ShouldReturnFalseWithAnError(BinaryKat<string, bool> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        bool decoded = CorpusEscapes.TryDecode(kat.Input, out byte[] bytes, out string? error);

        Assert.AreEqual(kat.Expected, decoded);
        Assert.IsFalse(string.IsNullOrEmpty(error));
        Assert.AreEqual(0, bytes.Length);
    }

    /// <summary>
    /// Verifies that decoding a <see langword="null" /> field throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenFieldIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CorpusEscapes.TryDecode(null!, out _, out _);
        });

        Assert.AreEqual("field", ex.ParamName);
    }
}
