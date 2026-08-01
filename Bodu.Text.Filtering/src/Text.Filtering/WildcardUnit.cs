// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WildcardUnit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Represents one element of a compiled wildcard pattern: a specific character, a single-character wildcard, a
/// character class, or a variable-length <c>*</c> segment.
/// </summary>
internal readonly struct WildcardUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WildcardUnit" /> struct.
    /// </summary>
    /// <param name="type">The kind of match this unit performs.</param>
    /// <param name="ch">
    /// The character to match when <paramref name="type" /> is <see cref="WildcardUnitType.Char" />.
    /// </param>
    /// <param name="classPairs">
    /// The character-class membership data when <paramref name="type" /> is <see cref="WildcardUnitType.Class" />:
    /// consecutive pairs of characters, each pair an inclusive low/high range bound (single members are stored with
    /// both bounds equal).
    /// </param>
    /// <param name="negated">Whether a character class matches characters outside the set instead of inside it.</param>
    private WildcardUnit(WildcardUnitType type, char ch, string? classPairs, bool negated)
    {
        Type = type;
        Ch = ch;
        ClassPairs = classPairs;
        Negated = negated;
    }

    /// <summary>
    /// Gets the kind of match this unit performs.
    /// </summary>
    public WildcardUnitType Type { get; }

    /// <summary>
    /// Gets the character matched when <see cref="Type" /> is <see cref="WildcardUnitType.Char" />.
    /// </summary>
    public char Ch { get; }

    /// <summary>
    /// Gets the character-class range pairs when <see cref="Type" /> is <see cref="WildcardUnitType.Class" />;
    /// otherwise <see langword="null" />.
    /// </summary>
    public string? ClassPairs { get; }

    /// <summary>
    /// Gets a value indicating whether the character class is negated (<c>[!...]</c>).
    /// </summary>
    public bool Negated { get; }

    /// <summary>
    /// Gets the unit representing the variable-length <c>*</c> metacharacter.
    /// </summary>
    public static WildcardUnit Star =>
        new(WildcardUnitType.Star, '\0', null, false);

    /// <summary>
    /// Gets the unit representing the single-character <c>?</c> metacharacter.
    /// </summary>
    public static WildcardUnit AnyOne =>
        new(WildcardUnitType.AnyOne, '\0', null, false);

    /// <summary>
    /// Creates a unit that matches one specific character.
    /// </summary>
    /// <param name="ch">The character to match.</param>
    /// <returns>A <see cref="WildcardUnitType.Char" /> unit for <paramref name="ch" />.</returns>
    public static WildcardUnit ForChar(char ch) =>
        new(WildcardUnitType.Char, ch, null, false);

    /// <summary>
    /// Creates a unit that matches one character against a character class.
    /// </summary>
    /// <param name="classPairs">The membership data as consecutive inclusive low/high range-bound pairs.</param>
    /// <param name="negated">Whether the class matches characters outside the set.</param>
    /// <returns>A <see cref="WildcardUnitType.Class" /> unit.</returns>
    public static WildcardUnit ForClass(string classPairs, bool negated) =>
        new(WildcardUnitType.Class, '\0', classPairs, negated);
}
