// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocMemberKindHint.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.CodeStyle.XmlDocumentation;

/// <summary>
/// Identifies the broad kind of member that owns a documentation comment.
/// </summary>
/// <remarks>
/// <para>
/// The hint is purely informational; the formatter does not currently change layout based on the member kind. It
/// is reserved for future rules (for example, requiring <c>&lt;returns&gt;</c> on methods with non-void return).
/// </para>
/// </remarks>
public enum XmlDocMemberKindHint
{
    /// <summary>
    /// The member kind is unknown or not relevant.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The documentation comment is attached to a type declaration.
    /// </summary>
    Type = 1,

    /// <summary>
    /// The documentation comment is attached to a method declaration.
    /// </summary>
    Method = 2,

    /// <summary>
    /// The documentation comment is attached to a property or indexer declaration.
    /// </summary>
    Property = 3,

    /// <summary>
    /// The documentation comment is attached to a field declaration.
    /// </summary>
    Field = 4,

    /// <summary>
    /// The documentation comment is attached to an event declaration.
    /// </summary>
    Event = 5,

    /// <summary>
    /// The documentation comment is attached to a constructor declaration.
    /// </summary>
    Constructor = 6,
}
